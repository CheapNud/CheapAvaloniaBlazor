using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace CheapAvaloniaBlazor.Utilities;

/// <summary>
/// Helper class for controlling the console window visibility on Windows.
/// </summary>
internal static class ConsoleHelper
{
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

    /// <summary>
    /// Suppresses console window for desktop apps. Call early in startup.
    /// Detaches from any inherited console and redirects stdout/stderr to null.
    /// </summary>
    public static void SuppressConsole()
    {
        if (!OperatingSystem.IsWindows())
            return;

        // Detach from any inherited/parent console
        FreeConsole();

        // Redirect stdout/stderr to prevent Console.Write from allocating a new console
        try
        {
            Console.SetOut(TextWriter.Null);
            Console.SetError(TextWriter.Null);
        }
        catch
        {
            // Ignore if redirection fails
        }
    }

    /// <summary>
    /// Shows the console window. Used for error recovery during startup failures.
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
