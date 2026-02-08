using CheapAvaloniaBlazor.Models;

namespace CheapAvaloniaBlazor.Services.Backends;

/// <summary>
/// Internal backend interface for platform-specific global hotkey implementations.
/// Backends are created by <see cref="HotkeyService"/> based on the current platform.
/// </summary>
internal interface IHotkeyBackend : IDisposable
{
    /// <summary>
    /// Whether this backend is functional on the current platform/session.
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// Registers a hotkey with the platform. Returns true on success.
    /// </summary>
    bool Register(int hotkeyId, HotkeyModifiers modifiers, Avalonia.Input.Key key);

    /// <summary>
    /// Unregisters a previously registered hotkey. Returns true on success.
    /// </summary>
    bool Unregister(int hotkeyId);

    /// <summary>
    /// Unregisters all hotkeys managed by this backend.
    /// </summary>
    void UnregisterAll();

    /// <summary>
    /// Fired when a registered hotkey is pressed. Carries the hotkey ID.
    /// </summary>
    event Action<int>? HotkeyPressed;
}
