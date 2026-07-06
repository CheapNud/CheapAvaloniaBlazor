using Photino.NET;

namespace CheapAvaloniaBlazor.Utilities;

/// <summary>
/// Helpers for <see cref="PhotinoWindow"/> platform quirks.
/// </summary>
internal static class PhotinoWindowExtensions
{
    /// <summary>
    /// <see cref="PhotinoWindow.WindowHandle"/> throws <see cref="PlatformNotSupportedException"/>
    /// on anything other than Windows. Returns <see cref="IntPtr.Zero"/> there instead, so callers
    /// can share one code path and let the handle-consuming Windows backends no-op.
    /// </summary>
    public static IntPtr GetWindowHandleOrZero(this PhotinoWindow window)
        => OperatingSystem.IsWindows() ? window.WindowHandle : IntPtr.Zero;
}
