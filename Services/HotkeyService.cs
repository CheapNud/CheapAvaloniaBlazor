using System.Collections.Concurrent;
using Avalonia.Input;
using CheapAvaloniaBlazor.Models;
using CheapAvaloniaBlazor.Services.Backends;
using Microsoft.Extensions.Logging;

namespace CheapAvaloniaBlazor.Services;

/// <summary>
/// Cross-platform orchestrator for global hotkeys.
/// Selects the appropriate platform backend (Windows, D-Bus, X11) and relays events/callbacks.
/// </summary>
public sealed class HotkeyService : IHotkeyService, IDisposable
{
    private readonly ILogger<HotkeyService> _logger;
    private readonly IHotkeyBackend _backend;
    private readonly ConcurrentDictionary<int, HotkeyRegistration> _registrations = new();

    private volatile bool _disposed;
    private int _nextId;

    public bool IsSupported => _backend.IsSupported;

    public event Action<int>? HotkeyPressed;

    public HotkeyService(ILogger<HotkeyService> logger)
    {
        _logger = logger;
        _backend = CreateBackend(logger);
        _backend.HotkeyPressed += OnBackendHotkeyPressed;

        _logger.LogDebug("HotkeyService initialized with backend {Backend} (supported={Supported})",
            _backend.GetType().Name, _backend.IsSupported);
    }

    public int RegisterHotkey(HotkeyModifiers modifiers, Key key, Action callback)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_backend.IsSupported)
            throw new PlatformNotSupportedException("Global hotkeys are not supported on this platform.");

        var hotkeyId = Interlocked.Increment(ref _nextId);
        var registration = new HotkeyRegistration(hotkeyId, modifiers, key, callback);

        var registered = _backend.Register(hotkeyId, modifiers, key);
        if (!registered)
        {
            throw new InvalidOperationException(
                $"Failed to register hotkey {modifiers}+{key}. " +
                "The key combination may already be registered by another application.");
        }

        _registrations[hotkeyId] = registration;
        _logger.LogInformation("Registered global hotkey: {Modifiers}+{Key} (ID={Id})", modifiers, key, hotkeyId);
        return hotkeyId;
    }

    public bool UnregisterHotkey(int hotkeyId)
    {
        if (_disposed) return false;
        if (!_registrations.TryRemove(hotkeyId, out var registration)) return false;

        _backend.Unregister(hotkeyId);

        _logger.LogInformation("Unregistered global hotkey: {Modifiers}+{Key} (ID={Id})", registration.Modifiers, registration.Key, hotkeyId);
        return true;
    }

    public void UnregisterAll()
    {
        if (_disposed) return;

        var ids = _registrations.Keys.ToArray();
        foreach (var hotkeyId in ids)
            UnregisterHotkey(hotkeyId);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _backend.HotkeyPressed -= OnBackendHotkeyPressed;
        UnregisterAll();
        _backend.Dispose();
    }

    private void OnBackendHotkeyPressed(int hotkeyId)
    {
        if (_registrations.TryGetValue(hotkeyId, out var registration))
        {
            try
            {
                registration.Callback();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hotkey callback threw for ID={Id}", hotkeyId);
            }
        }

        try
        {
            HotkeyPressed?.Invoke(hotkeyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HotkeyPressed event handler threw for ID={Id}", hotkeyId);
        }
    }

    private static IHotkeyBackend CreateBackend(ILogger logger)
    {
        if (OperatingSystem.IsWindows())
            return new WindowsHotkeyBackend(logger);

        if (OperatingSystem.IsLinux())
        {
            var dbusBackend = new DbusHotkeyBackend(logger);
            if (dbusBackend.IsSupported)
                return dbusBackend;

            dbusBackend.Dispose();
            logger.LogDebug("D-Bus GlobalShortcuts portal not available, trying X11 fallback");

            var x11Backend = new X11HotkeyBackend(logger);
            if (x11Backend.IsSupported)
                return x11Backend;

            x11Backend.Dispose();
            logger.LogDebug("X11 not available either, global hotkeys disabled");
        }

        return new NullHotkeyBackend();
    }
}
