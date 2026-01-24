using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace CheapAvaloniaBlazor.Utilities;

/// <summary>
/// Helper class for controlling the console window visibility.
/// Uses Win32 API to show/hide the console window at runtime.
/// </summary>
internal static class ConsoleHelper
{
    // Win32 ShowWindow constants
    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;

    [SupportedOSPlatform("windows")]
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [SupportedOSPlatform("windows")]
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    /// <summary>
    /// Hides the console window on Windows. No-op on other platforms.
    /// </summary>
    /// <returns>True if console was hidden, false if not Windows or no console attached.</returns>
    public static bool HideConsoleWindow()
    {
        if (!OperatingSystem.IsWindows())
            return false;

        var handle = GetConsoleWindow();
        if (handle == IntPtr.Zero)
            return false; // No console window attached (e.g., WinExe app)

        return ShowWindow(handle, SW_HIDE);
    }

    /// <summary>
    /// Shows the console window on Windows. No-op on other platforms.
    /// Useful for debugging scenarios where console output is needed after initial hide.
    /// </summary>
    /// <returns>True if console was shown, false if not Windows or no console attached.</returns>
    public static bool ShowConsoleWindow()
    {
        if (!OperatingSystem.IsWindows())
            return false;

        var handle = GetConsoleWindow();
        if (handle == IntPtr.Zero)
            return false; // No console window attached

        return ShowWindow(handle, SW_SHOW);
    }
}
