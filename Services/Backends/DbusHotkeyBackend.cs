using System.Runtime.Versioning;
using Avalonia.Input;
using CheapAvaloniaBlazor.Models;
using CheapAvaloniaBlazor.Utilities;
using Microsoft.Extensions.Logging;
using Tmds.DBus.Protocol;

namespace CheapAvaloniaBlazor.Services.Backends;

/// <summary>
/// D-Bus GlobalShortcuts portal backend for Wayland-native global hotkeys.
/// Requires xdg-desktop-portal with GlobalShortcuts support (KDE 5.27+, GNOME 48+, Hyprland).
/// Uses the org.freedesktop.portal.GlobalShortcuts interface.
/// </summary>
[SupportedOSPlatform("linux")]
internal sealed class DbusHotkeyBackend : IHotkeyBackend
{
    private readonly ILogger _logger;
    private readonly object _lock = new();

    private DBusConnection? _connection;
    private string? _sessionHandle;
    private IDisposable? _activatedSubscription;

    // hotkeyId → portal shortcut id ("hotkey_N")
    private readonly Dictionary<int, string> _shortcutIdMap = [];
    // Reverse: portal shortcut id → hotkeyId
    private readonly Dictionary<string, int> _reverseMap = [];
    // hotkeyId → (modifiers, key) for rebind
    private readonly Dictionary<int, (HotkeyModifiers modifiers, Key key)> _hotkeyDetails = [];

    private volatile bool _disposed;

    private const string PortalBus = "org.freedesktop.portal.Desktop";
    private const string PortalPath = "/org/freedesktop/portal/desktop";
    private const string ShortcutsInterface = "org.freedesktop.portal.GlobalShortcuts";
    private const string RequestInterface = "org.freedesktop.portal.Request";
    private const string SessionInterface = "org.freedesktop.portal.Session";

    public bool IsSupported { get; }

    public event Action<int>? HotkeyPressed;

    public DbusHotkeyBackend(ILogger logger)
    {
        _logger = logger;

        if (!OperatingSystem.IsLinux())
        {
            IsSupported = false;
            return;
        }

        try
        {
            // Run on a free thread-pool thread so we don't deadlock if the constructor
            // is invoked under a captured SynchronizationContext (e.g. Avalonia's UI dispatcher).
            IsSupported = Task.Run(InitializeSessionAsync).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "D-Bus GlobalShortcuts portal initialization failed");
            IsSupported = false;
        }
    }

    public bool Register(int hotkeyId, HotkeyModifiers modifiers, Key key)
    {
        if (_disposed || !IsSupported) return false;

        var trigger = KeyMapper.ToPortalTrigger(modifiers, key);
        if (trigger is null) return false;

        lock (_lock)
        {
            var shortcutId = $"hotkey_{hotkeyId}";
            _shortcutIdMap[hotkeyId] = shortcutId;
            _reverseMap[shortcutId] = hotkeyId;
            _hotkeyDetails[hotkeyId] = (modifiers, key);
        }

        try
        {
            Task.Run(RebindAllShortcutsAsync).GetAwaiter().GetResult();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "D-Bus: Failed to bind shortcut for hotkey ID={Id}", hotkeyId);

            // Roll back — defensive check in case a concurrent Unregister already removed it
            lock (_lock)
            {
                if (_shortcutIdMap.TryGetValue(hotkeyId, out var shortcutId))
                {
                    _shortcutIdMap.Remove(hotkeyId);
                    _reverseMap.Remove(shortcutId);
                    _hotkeyDetails.Remove(hotkeyId);
                }
            }

            return false;
        }
    }

    public bool Unregister(int hotkeyId)
    {
        if (_disposed || !IsSupported) return false;

        lock (_lock)
        {
            if (!_shortcutIdMap.TryGetValue(hotkeyId, out var shortcutId))
                return false;

            _shortcutIdMap.Remove(hotkeyId);
            _reverseMap.Remove(shortcutId);
            _hotkeyDetails.Remove(hotkeyId);
        }

        try
        {
            Task.Run(RebindAllShortcutsAsync).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "D-Bus: Failed to rebind after unregister of hotkey ID={Id}", hotkeyId);
        }

        return true;
    }

    public void UnregisterAll()
    {
        if (_disposed || !IsSupported) return;

        lock (_lock)
        {
            _shortcutIdMap.Clear();
            _reverseMap.Clear();
            _hotkeyDetails.Clear();
        }

        try
        {
            Task.Run(RebindAllShortcutsAsync).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "D-Bus: Failed to rebind after unregister all");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _activatedSubscription?.Dispose();

        if (_connection is not null && _sessionHandle is not null)
        {
            try
            {
                Task.Run(CloseSessionAsync).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "D-Bus: Failed to close session on dispose");
            }
        }

        _connection?.Dispose();
        _connection = null;
    }

    private async Task<bool> InitializeSessionAsync()
    {
        var sessionAddress = DBusAddress.Session;
        if (sessionAddress is null)
        {
            _logger.LogDebug("D-Bus: No session bus address available");
            return false;
        }

        _connection = new DBusConnection(sessionAddress);
        await _connection.ConnectAsync().ConfigureAwait(false);

        _logger.LogDebug("D-Bus: Connected to session bus");

        _sessionHandle = await CreateSessionAsync().ConfigureAwait(false);
        if (_sessionHandle is null)
        {
            _logger.LogDebug("D-Bus: CreateSession failed — GlobalShortcuts portal not available");
            _connection.Dispose();
            _connection = null;
            return false;
        }

        await SubscribeActivatedAsync().ConfigureAwait(false);

        _logger.LogDebug("D-Bus: GlobalShortcuts session created: {Session}", _sessionHandle);
        return true;
    }

    // Response signal data extracted by the reader delegate
    private sealed record CreateSessionResponse(uint ResponseCode, string? SessionHandle);

    private async Task<string?> CreateSessionAsync()
    {
        var senderName = _connection!.UniqueName!.Replace(".", "_").Replace(":", "");
        var sessionToken = $"cheapblazor_hotkey_{Environment.ProcessId}_{Environment.TickCount64}";
        var requestToken = $"req_{Environment.ProcessId}_create";
        var requestPath = $"/org/freedesktop/portal/desktop/request/{senderName}/{requestToken}";

        var responseSource = new TaskCompletionSource<CreateSessionResponse>();

        // Reader delegate: Message → CreateSessionResponse (extracts data from the D-Bus message)
        static CreateSessionResponse ReadResponse(Message message, object? state)
        {
            var reader = message.GetBodyReader();
            var responseCode = reader.ReadUInt32();
            string? sessionPath = null;

            if (responseCode == 0)
            {
                var results = reader.ReadDictionaryOfStringToVariantValue();
                if (results.TryGetValue("session_handle", out var handleVariant))
                    sessionPath = handleVariant.GetString();
            }

            return new CreateSessionResponse(responseCode, sessionPath);
        }

        using var signalSubscription = await _connection.AddMatchAsync(
            new MatchRule
            {
                Type = MessageType.Signal,
                Interface = RequestInterface,
                Member = "Response",
                Path = requestPath
            },
            (MessageValueReader<CreateSessionResponse>)ReadResponse,
            (Notification<CreateSessionResponse> notification) =>
            {
                if (notification.Exception is not null)
                    responseSource.TrySetException(notification.Exception);
                else if (notification.HasValue)
                    responseSource.TrySetResult(notification.Value);
            },
            emitOnCapturedContext: false,
            ObserverFlags.None).ConfigureAwait(false);

        try
        {
            await _connection.CallMethodAsync(CreateSessionMessage(requestToken, sessionToken)).ConfigureAwait(false);

            try
            {
                var response = await responseSource.Task.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                return response.ResponseCode == 0 ? response.SessionHandle : null;
            }
            catch (TimeoutException)
            {
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "D-Bus: CreateSession threw");
            return null;
        }
    }

    private MessageBuffer CreateSessionMessage(string requestToken, string sessionToken)
    {
        var writer = _connection!.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: PortalBus,
            path: PortalPath,
            @interface: ShortcutsInterface,
            member: "CreateSession",
            signature: "a{sv}",
            flags: MessageFlags.None);

        writer.WriteDictionary(new Dictionary<string, VariantValue>
        {
            ["handle_token"] = VariantValue.String(requestToken),
            ["session_handle_token"] = VariantValue.String(sessionToken)
        });

        return writer.CreateMessage();
    }

    // Activated signal data: (session_handle, shortcut_id, timestamp, options)
    private sealed record ActivatedSignal(string SessionHandle, string ShortcutId);

    private async Task SubscribeActivatedAsync()
    {
        static ActivatedSignal ReadActivated(Message message, object? state)
        {
            var reader = message.GetBodyReader();
            var sessionHandle = reader.ReadString();
            var shortcutId = reader.ReadString();
            return new ActivatedSignal(sessionHandle, shortcutId);
        }

        _activatedSubscription = await _connection!.AddMatchAsync(
            new MatchRule
            {
                Type = MessageType.Signal,
                Interface = ShortcutsInterface,
                Member = "Activated",
                Path = PortalPath
            },
            (MessageValueReader<ActivatedSignal>)ReadActivated,
            (Notification<ActivatedSignal> notification) =>
            {
                if (_disposed || notification.Exception is not null || !notification.HasValue) return;

                try
                {
                    int hotkeyId;
                    lock (_lock)
                    {
                        if (!_reverseMap.TryGetValue(notification.Value.ShortcutId, out hotkeyId))
                            return;
                    }

                    HotkeyPressed?.Invoke(hotkeyId);
                }
                catch (Exception activatedEx)
                {
                    _logger.LogError(activatedEx, "D-Bus: Error handling Activated signal");
                }
            },
            emitOnCapturedContext: false,
            ObserverFlags.None).ConfigureAwait(false);
    }

    private async Task RebindAllShortcutsAsync()
    {
        if (_connection is null || _sessionHandle is null) return;

        List<(string id, string description, string trigger)> shortcuts;

        lock (_lock)
        {
            shortcuts = _shortcutIdMap.Select(kvp =>
            {
                var (hotkeyId, shortcutId) = kvp;
                var (modifiers, key) = _hotkeyDetails[hotkeyId];
                var trigger = KeyMapper.ToPortalTrigger(modifiers, key) ?? "";
                return (shortcutId, $"Global hotkey {modifiers}+{key}", trigger);
            }).ToList();
        }

        var senderName = _connection.UniqueName!.Replace(".", "_").Replace(":", "");
        var requestToken = $"req_{Environment.ProcessId}_bind_{Environment.TickCount64}";
        var requestPath = $"/org/freedesktop/portal/desktop/request/{senderName}/{requestToken}";

        var responseSource = new TaskCompletionSource<uint>();

        static uint ReadBindResponse(Message message, object? state)
        {
            var reader = message.GetBodyReader();
            return reader.ReadUInt32();
        }

        using var signalSubscription = await _connection.AddMatchAsync(
            new MatchRule
            {
                Type = MessageType.Signal,
                Interface = RequestInterface,
                Member = "Response",
                Path = requestPath
            },
            (MessageValueReader<uint>)ReadBindResponse,
            (Notification<uint> notification) =>
            {
                if (notification.Exception is not null)
                    responseSource.TrySetException(notification.Exception);
                else if (notification.HasValue)
                    responseSource.TrySetResult(notification.Value);
            },
            emitOnCapturedContext: false,
            ObserverFlags.None).ConfigureAwait(false);

        await _connection.CallMethodAsync(CreateBindMessage(requestToken, shortcuts)).ConfigureAwait(false);

        // Wait for portal response (user may need to confirm in a dialog)
        try
        {
            var responseCode = await responseSource.Task.WaitAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false);
            if (responseCode != 0)
                _logger.LogWarning("D-Bus: BindShortcuts returned response code {Code}", responseCode);
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("D-Bus: BindShortcuts timed out after 30 seconds");
        }
    }

    private MessageBuffer CreateBindMessage(string requestToken, List<(string id, string description, string trigger)> shortcuts)
    {
        var writer = _connection!.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: PortalBus,
            path: PortalPath,
            @interface: ShortcutsInterface,
            member: "BindShortcuts",
            signature: "oa(sa{sv})a{sv}",
            flags: MessageFlags.None);

        // session_handle (object path)
        writer.WriteObjectPath(_sessionHandle!);

        // shortcuts array: a(sa{sv})
        // Each element is a struct: (shortcut_id: string, options: a{sv})
        var arrayStart = writer.WriteArrayStart(DBusType.Struct);
        foreach (var (shortcutId, description, trigger) in shortcuts)
        {
            writer.WriteStructureStart();
            writer.WriteString(shortcutId);
            writer.WriteDictionary(new Dictionary<string, VariantValue>
            {
                ["description"] = VariantValue.String(description),
                ["preferred_trigger"] = VariantValue.String(trigger)
            });
        }
        writer.WriteArrayEnd(arrayStart);

        // options dict
        writer.WriteDictionary(new Dictionary<string, VariantValue>
        {
            ["handle_token"] = VariantValue.String(requestToken)
        });

        return writer.CreateMessage();
    }

    private async Task CloseSessionAsync()
    {
        if (_connection is null || _sessionHandle is null) return;

        var writer = _connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: PortalBus,
            path: _sessionHandle,
            @interface: SessionInterface,
            member: "Close",
            signature: null,
            flags: MessageFlags.None);

        var closeMsg = writer.CreateMessage();
        await _connection.CallMethodAsync(closeMsg).ConfigureAwait(false);
    }
}
