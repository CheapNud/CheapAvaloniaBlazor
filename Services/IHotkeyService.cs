using Avalonia.Input;
using CheapAvaloniaBlazor.Models;

namespace CheapAvaloniaBlazor.Services;

/// <summary>
/// Service for registering system-wide global hotkeys.
/// Hotkeys fire even when the application window is not focused.
/// Supported on Windows (Win32 RegisterHotKey) and Linux (D-Bus GlobalShortcuts portal / X11 XGrabKey).
/// </summary>
public interface IHotkeyService
{
    /// <summary>
    /// Whether global hotkeys are supported on the current platform/session.
    /// True on Windows and Linux (with D-Bus portal or X11), false on macOS.
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// Registers a global hotkey with the specified modifier keys and key.
    /// </summary>
    /// <param name="modifiers">Modifier keys (Ctrl, Alt, Shift, Win)</param>
    /// <param name="key">The key to register</param>
    /// <param name="callback">Action invoked when the hotkey is pressed</param>
    /// <returns>A unique hotkey ID for later unregistration</returns>
    /// <exception cref="PlatformNotSupportedException">Thrown on unsupported platforms</exception>
    /// <exception cref="InvalidOperationException">Thrown when registration fails (e.g. hotkey already in use)</exception>
    int RegisterHotkey(HotkeyModifiers modifiers, Key key, Action callback);

    /// <summary>
    /// Unregisters a previously registered hotkey.
    /// </summary>
    /// <param name="hotkeyId">The ID returned by <see cref="RegisterHotkey"/></param>
    /// <returns>True if the hotkey was found and unregistered, false if the ID was not found</returns>
    bool UnregisterHotkey(int hotkeyId);

    /// <summary>
    /// Unregisters all currently registered hotkeys.
    /// </summary>
    void UnregisterAll();

    /// <summary>
    /// Fired when any registered hotkey is pressed, with the hotkey ID.
    /// </summary>
    event Action<int>? HotkeyPressed;
}
