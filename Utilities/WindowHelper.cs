using System.Runtime.InteropServices;

namespace CheapAvaloniaBlazor.Utilities;

/// <summary>
/// Platform-specific window helper methods
/// </summary>
public static class WindowHelper
{
    // Windows ShowWindow constants
    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;
    private const int SW_RESTORE = 9;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    /// <summary>
    /// Hide a window completely (not just minimize to taskbar)
    /// </summary>
    /// <param name="windowHandle">Native window handle</param>
    /// <returns>True if successful</returns>
    public static bool HideWindow(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
            return false;

        if (OperatingSystem.IsWindows())
        {
            return ShowWindow(windowHandle, SW_HIDE);
        }

        // Other platforms: not implemented yet
        return false;
    }

    /// <summary>
    /// Show a previously hidden window and bring it to foreground
    /// </summary>
    /// <param name="windowHandle">Native window handle</param>
    /// <returns>True if successful</returns>
    public static bool ShowAndActivateWindow(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
            return false;

        if (OperatingSystem.IsWindows())
        {
            ShowWindow(windowHandle, SW_RESTORE);
            ShowWindow(windowHandle, SW_SHOW);
            SetForegroundWindow(windowHandle);
            return true;
        }

        // Other platforms: not implemented yet
        return false;
    }
}
