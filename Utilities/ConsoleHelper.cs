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

    [SupportedOSPlatform("windows")]
    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();

    [SupportedOSPlatform("windows")]
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    private const int STD_OUTPUT_HANDLE = -11;

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

    /// <summary>
    /// Ensures a console window exists for logging output.
    /// Allocates a new console if one doesn't exist (e.g., when launched from Windows Explorer).
    /// </summary>
    public static void EnsureConsole()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var handle = GetConsoleWindow();
        if (handle == IntPtr.Zero)
        {
            // No console exists, allocate one
            if (AllocConsole())
            {
                // Reopen stdout/stderr to the new console
                var stdOut = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
                var stdErr = new StreamWriter(Console.OpenStandardError()) { AutoFlush = true };
                Console.SetOut(stdOut);
                Console.SetError(stdErr);
            }
        }
        else
        {
            // Console exists, make sure it's visible
            ShowWindow(handle, SW_SHOW);
        }
    }
}
