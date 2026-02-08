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

    private Connection? _connection;
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
            IsSupported = InitializeSession();
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
            RebindAllShortcuts();
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
            RebindAllShortcuts();
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
            RebindAllShortcuts();
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
                CloseSession();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "D-Bus: Failed to close session on dispose");
            }
        }

        _connection?.Dispose();
        _connection = null;
    }

    private bool InitializeSession()
    {
        var sessionAddress = Address.Session;
        if (sessionAddress is null)
        {
            _logger.LogDebug("D-Bus: No session bus address available");
            return false;
        }

        _connection = new Connection(sessionAddress);
        _connection.ConnectAsync().GetAwaiter().GetResult();

        _logger.LogDebug("D-Bus: Connected to session bus");

        _sessionHandle = CreateSession();
        if (_sessionHandle is null)
        {
            _logger.LogDebug("D-Bus: CreateSession failed — GlobalShortcuts portal not available");
            _connection.Dispose();
            _connection = null;
            return false;
        }

        SubscribeActivated();

        _logger.LogDebug("D-Bus: GlobalShortcuts session created: {Session}", _sessionHandle);
        return true;
    }

    // Response signal data extracted by the reader delegate
    private sealed record CreateSessionResponse(uint ResponseCode, string? SessionHandle);

    private string? CreateSession()
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

        var signalSubscription = _connection.AddMatchAsync(
            new MatchRule
            {
                Type = MessageType.Signal,
                Interface = RequestInterface,
                Member = "Response",
                Path = requestPath
            },
            (MessageValueReader<CreateSessionResponse>)ReadResponse,
            (Exception? ex, CreateSessionResponse resp, object? rs, object? hs) =>
            {
                if (ex is not null)
                    responseSource.TrySetException(ex);
                else
                    responseSource.TrySetResult(resp);
            },
            ObserverFlags.None).GetAwaiter().GetResult();

        try
        {
            _connection.CallMethodAsync(CreateSessionMessage(requestToken, sessionToken)).GetAwaiter().GetResult();

            if (!responseSource.Task.Wait(TimeSpan.FromSeconds(10)))
                return null;

            var response = responseSource.Task.Result;
            return response.ResponseCode == 0 ? response.SessionHandle : null;
        }
        catch
        {
            return null;
        }
        finally
        {
            signalSubscription.Dispose();
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

    private void SubscribeActivated()
    {
        static ActivatedSignal ReadActivated(Message message, object? state)
        {
            var reader = message.GetBodyReader();
            var sessionHandle = reader.ReadString();
            var shortcutId = reader.ReadString();
            return new ActivatedSignal(sessionHandle, shortcutId);
        }

        _activatedSubscription = _connection!.AddMatchAsync(
            new MatchRule
            {
                Type = MessageType.Signal,
                Interface = ShortcutsInterface,
                Member = "Activated",
                Path = PortalPath
            },
            (MessageValueReader<ActivatedSignal>)ReadActivated,
            (Exception? ex, ActivatedSignal signal, object? rs, object? hs) =>
            {
                if (ex is not null || _disposed) return;

                try
                {
                    int hotkeyId;
                    lock (_lock)
                    {
                        if (!_reverseMap.TryGetValue(signal.ShortcutId, out hotkeyId))
                            return;
                    }

                    HotkeyPressed?.Invoke(hotkeyId);
                }
                catch (Exception activatedEx)
                {
                    _logger.LogError(activatedEx, "D-Bus: Error handling Activated signal");
                }
            },
            ObserverFlags.None).GetAwaiter().GetResult();
    }

    private void RebindAllShortcuts()
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

        var signalSubscription = _connection.AddMatchAsync(
            new MatchRule
            {
                Type = MessageType.Signal,
                Interface = RequestInterface,
                Member = "Response",
                Path = requestPath
            },
            (MessageValueReader<uint>)ReadBindResponse,
            (Exception? ex, uint responseCode, object? rs, object? hs) =>
            {
                if (ex is not null)
                    responseSource.TrySetException(ex);
                else
                    responseSource.TrySetResult(responseCode);
            },
            ObserverFlags.None).GetAwaiter().GetResult();

        try
        {
            _connection.CallMethodAsync(CreateBindMessage(requestToken, shortcuts)).GetAwaiter().GetResult();

            // Wait for portal response (user may need to confirm in a dialog)
            responseSource.Task.Wait(TimeSpan.FromSeconds(30));

            if (responseSource.Task.IsCompletedSuccessfully && responseSource.Task.Result != 0)
                _logger.LogWarning("D-Bus: BindShortcuts returned response code {Code}", responseSource.Task.Result);
        }
        finally
        {
            signalSubscription.Dispose();
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

    private void CloseSession()
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
        _connection.CallMethodAsync(closeMsg).GetAwaiter().GetResult();
    }
}
