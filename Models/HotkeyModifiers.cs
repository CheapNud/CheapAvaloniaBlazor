namespace CheapAvaloniaBlazor.Models;

/// <summary>
/// Modifier keys for global hotkey registration.
/// Values match Win32 MOD_* constants directly.
/// </summary>
[Flags]
public enum HotkeyModifiers
{
    None = 0,
    Alt = 0x0001,
    Ctrl = 0x0002,
    Shift = 0x0004,
    Win = 0x0008
}
