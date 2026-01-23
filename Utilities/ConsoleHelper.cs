using System.Runtime.InteropServices;

namespace CheapAvaloniaBlazor.Utilities;

/// <summary>
/// Helper class for controlling the console window visibility
/// </summary>
internal static class ConsoleHelper
{
    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    /// <summary>
    /// Hides the console window on Windows. No-op on other platforms.
    /// </summary>
    public static void HideConsoleWindow()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var handle = GetConsoleWindow();
        if (handle != IntPtr.Zero)
        {
            ShowWindow(handle, SW_HIDE);
        }
    }

    /// <summary>
    /// Shows the console window on Windows. No-op on other platforms.
    /// </summary>
    public static void ShowConsoleWindow()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var handle = GetConsoleWindow();
        if (handle != IntPtr.Zero)
        {
            ShowWindow(handle, SW_SHOW);
        }
    }
}
