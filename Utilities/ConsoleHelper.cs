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

    [SupportedOSPlatform("windows")]
    [DllImport("kernel32.dll")]
    private static extern bool FreeConsole();

    [SupportedOSPlatform("windows")]
    [DllImport("kernel32.dll")]
    private static extern bool AttachConsole(int dwProcessId);

    /// <summary>
    /// Detaches from any console window. Call this early to prevent console allocation.
    /// </summary>
    /// <returns>True if successfully detached, false if not Windows.</returns>
    public static bool DetachConsole()
    {
        if (!OperatingSystem.IsWindows())
            return false;

        // FreeConsole detaches from any attached console, preventing child processes
        // or libraries (like ASP.NET Core) from inheriting or allocating a console
        return FreeConsole();
    }

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
    /// Ensures no console is visible by first detaching, then hiding any remaining console.
    /// This is the recommended method for production apps to suppress all console windows.
    /// </summary>
    /// <returns>True if any action was taken, false if not Windows.</returns>
    public static bool SuppressConsole()
    {
        if (!OperatingSystem.IsWindows())
            return false;

        // First detach from any inherited console
        FreeConsole();

        // Then hide any console window that might still exist
        var handle = GetConsoleWindow();
        if (handle != IntPtr.Zero)
        {
            ShowWindow(handle, SW_HIDE);
        }

        // Redirect stdout/stderr to prevent any Console.Write calls from allocating a console
        try
        {
            Console.SetOut(TextWriter.Null);
            Console.SetError(TextWriter.Null);
        }
        catch
        {
            // Ignore if redirection fails
        }

        return true;
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
