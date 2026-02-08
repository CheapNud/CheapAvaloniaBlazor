using System.Text;
using Avalonia.Input;
using CheapAvaloniaBlazor.Models;

namespace CheapAvaloniaBlazor.Utilities;

/// <summary>
/// Maps Avalonia <see cref="Key"/> values to platform-specific key codes.
/// Returns null for unsupported keys.
/// </summary>
public static class KeyMapper
{
    /// <summary>
    /// Maps an Avalonia key to a Win32 virtual key code.
    /// </summary>
    public static int? ToVirtualKey(Key key) => key switch
    {
        // Letters A-Z → 0x41-0x5A
        Key.A => 0x41,
        Key.B => 0x42,
        Key.C => 0x43,
        Key.D => 0x44,
        Key.E => 0x45,
        Key.F => 0x46,
        Key.G => 0x47,
        Key.H => 0x48,
        Key.I => 0x49,
        Key.J => 0x4A,
        Key.K => 0x4B,
        Key.L => 0x4C,
        Key.M => 0x4D,
        Key.N => 0x4E,
        Key.O => 0x4F,
        Key.P => 0x50,
        Key.Q => 0x51,
        Key.R => 0x52,
        Key.S => 0x53,
        Key.T => 0x54,
        Key.U => 0x55,
        Key.V => 0x56,
        Key.W => 0x57,
        Key.X => 0x58,
        Key.Y => 0x59,
        Key.Z => 0x5A,

        // Digits 0-9 → 0x30-0x39
        Key.D0 => 0x30,
        Key.D1 => 0x31,
        Key.D2 => 0x32,
        Key.D3 => 0x33,
        Key.D4 => 0x34,
        Key.D5 => 0x35,
        Key.D6 => 0x36,
        Key.D7 => 0x37,
        Key.D8 => 0x38,
        Key.D9 => 0x39,

        // Function keys F1-F24 → 0x70-0x87
        Key.F1 => 0x70,
        Key.F2 => 0x71,
        Key.F3 => 0x72,
        Key.F4 => 0x73,
        Key.F5 => 0x74,
        Key.F6 => 0x75,
        Key.F7 => 0x76,
        Key.F8 => 0x77,
        Key.F9 => 0x78,
        Key.F10 => 0x79,
        Key.F11 => 0x7A,
        Key.F12 => 0x7B,
        Key.F13 => 0x7C,
        Key.F14 => 0x7D,
        Key.F15 => 0x7E,
        Key.F16 => 0x7F,
        Key.F17 => 0x80,
        Key.F18 => 0x81,
        Key.F19 => 0x82,
        Key.F20 => 0x83,
        Key.F21 => 0x84,
        Key.F22 => 0x85,
        Key.F23 => 0x86,
        Key.F24 => 0x87,

        // NumPad 0-9 → 0x60-0x69
        Key.NumPad0 => 0x60,
        Key.NumPad1 => 0x61,
        Key.NumPad2 => 0x62,
        Key.NumPad3 => 0x63,
        Key.NumPad4 => 0x64,
        Key.NumPad5 => 0x65,
        Key.NumPad6 => 0x66,
        Key.NumPad7 => 0x67,
        Key.NumPad8 => 0x68,
        Key.NumPad9 => 0x69,

        // Special keys
        Key.Space => 0x20,
        Key.Return => 0x0D,
        Key.Escape => 0x1B,
        Key.Tab => 0x09,
        Key.Back => 0x08,
        Key.Delete => 0x2E,
        Key.Insert => 0x2D,
        Key.Home => 0x24,
        Key.End => 0x23,
        Key.PageUp => 0x21,
        Key.PageDown => 0x22,

        // Arrow keys
        Key.Left => 0x25,
        Key.Up => 0x26,
        Key.Right => 0x27,
        Key.Down => 0x28,

        // Misc
        Key.PrintScreen => 0x2C,
        Key.Pause => 0x13,
        Key.CapsLock => 0x14,
        Key.NumLock => 0x90,
        Key.Scroll => 0x91,

        _ => null
    };

    /// <summary>
    /// Maps an Avalonia key to an X11 keysym value for XGrabKey.
    /// </summary>
    public static long? ToX11KeySym(Key key) => key switch
    {
        // Letters A-Z → lowercase keysyms 0x61-0x7A
        Key.A => 0x61,
        Key.B => 0x62,
        Key.C => 0x63,
        Key.D => 0x64,
        Key.E => 0x65,
        Key.F => 0x66,
        Key.G => 0x67,
        Key.H => 0x68,
        Key.I => 0x69,
        Key.J => 0x6A,
        Key.K => 0x6B,
        Key.L => 0x6C,
        Key.M => 0x6D,
        Key.N => 0x6E,
        Key.O => 0x6F,
        Key.P => 0x70,
        Key.Q => 0x71,
        Key.R => 0x72,
        Key.S => 0x73,
        Key.T => 0x74,
        Key.U => 0x75,
        Key.V => 0x76,
        Key.W => 0x77,
        Key.X => 0x78,
        Key.Y => 0x79,
        Key.Z => 0x7A,

        // Digits 0-9 → 0x30-0x39
        Key.D0 => 0x30,
        Key.D1 => 0x31,
        Key.D2 => 0x32,
        Key.D3 => 0x33,
        Key.D4 => 0x34,
        Key.D5 => 0x35,
        Key.D6 => 0x36,
        Key.D7 => 0x37,
        Key.D8 => 0x38,
        Key.D9 => 0x39,

        // Function keys F1-F24 → 0xFFBE-0xFFD5
        Key.F1 => 0xFFBE,
        Key.F2 => 0xFFBF,
        Key.F3 => 0xFFC0,
        Key.F4 => 0xFFC1,
        Key.F5 => 0xFFC2,
        Key.F6 => 0xFFC3,
        Key.F7 => 0xFFC4,
        Key.F8 => 0xFFC5,
        Key.F9 => 0xFFC6,
        Key.F10 => 0xFFC7,
        Key.F11 => 0xFFC8,
        Key.F12 => 0xFFC9,
        Key.F13 => 0xFFCA,
        Key.F14 => 0xFFCB,
        Key.F15 => 0xFFCC,
        Key.F16 => 0xFFCD,
        Key.F17 => 0xFFCE,
        Key.F18 => 0xFFCF,
        Key.F19 => 0xFFD0,
        Key.F20 => 0xFFD1,
        Key.F21 => 0xFFD2,
        Key.F22 => 0xFFD3,
        Key.F23 => 0xFFD4,
        Key.F24 => 0xFFD5,

        // NumPad 0-9 → 0xFFB0-0xFFB9
        Key.NumPad0 => 0xFFB0,
        Key.NumPad1 => 0xFFB1,
        Key.NumPad2 => 0xFFB2,
        Key.NumPad3 => 0xFFB3,
        Key.NumPad4 => 0xFFB4,
        Key.NumPad5 => 0xFFB5,
        Key.NumPad6 => 0xFFB6,
        Key.NumPad7 => 0xFFB7,
        Key.NumPad8 => 0xFFB8,
        Key.NumPad9 => 0xFFB9,

        // Special keys
        Key.Space => 0x20,
        Key.Return => 0xFF0D,
        Key.Escape => 0xFF1B,
        Key.Tab => 0xFF09,
        Key.Back => 0xFF08,
        Key.Delete => 0xFFFF,
        Key.Insert => 0xFF63,
        Key.Home => 0xFF50,
        Key.End => 0xFF57,
        Key.PageUp => 0xFF55,
        Key.PageDown => 0xFF56,

        // Arrow keys
        Key.Left => 0xFF51,
        Key.Up => 0xFF52,
        Key.Right => 0xFF53,
        Key.Down => 0xFF54,

        // Misc
        Key.PrintScreen => 0xFF61,
        Key.Pause => 0xFF13,
        Key.CapsLock => 0xFFE5,
        Key.NumLock => 0xFF7F,
        Key.Scroll => 0xFF14,

        _ => null
    };

    /// <summary>
    /// Converts modifiers and key to a GTK accelerator string for the D-Bus GlobalShortcuts portal.
    /// Example: Ctrl+Shift+H → "&lt;Control&gt;&lt;Shift&gt;h"
    /// </summary>
    internal static string? ToPortalTrigger(HotkeyModifiers modifiers, Key key)
    {
        var keyName = ToGtkKeyName(key);
        if (keyName is null) return null;

        var sb = new StringBuilder();

        if (modifiers.HasFlag(HotkeyModifiers.Ctrl))
            sb.Append("<Control>");
        if (modifiers.HasFlag(HotkeyModifiers.Alt))
            sb.Append("<Alt>");
        if (modifiers.HasFlag(HotkeyModifiers.Shift))
            sb.Append("<Shift>");
        if (modifiers.HasFlag(HotkeyModifiers.Win))
            sb.Append("<Super>");

        sb.Append(keyName);
        return sb.ToString();
    }

    private static string? ToGtkKeyName(Key key) => key switch
    {
        >= Key.A and <= Key.Z => ((char)('a' + (key - Key.A))).ToString(),
        >= Key.D0 and <= Key.D9 => ((char)('0' + (key - Key.D0))).ToString(),
        >= Key.F1 and <= Key.F24 => $"F{1 + (key - Key.F1)}",
        >= Key.NumPad0 and <= Key.NumPad9 => $"KP_{key - Key.NumPad0}",
        Key.Space => "space",
        Key.Return => "Return",
        Key.Escape => "Escape",
        Key.Tab => "Tab",
        Key.Back => "BackSpace",
        Key.Delete => "Delete",
        Key.Insert => "Insert",
        Key.Home => "Home",
        Key.End => "End",
        Key.PageUp => "Page_Up",
        Key.PageDown => "Page_Down",
        Key.Left => "Left",
        Key.Up => "Up",
        Key.Right => "Right",
        Key.Down => "Down",
        Key.PrintScreen => "Print",
        Key.Pause => "Pause",
        _ => null
    };
}
