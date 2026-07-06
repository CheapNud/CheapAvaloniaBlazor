using System.Runtime.InteropServices;

namespace CheapAvaloniaBlazor;

public static class PlatformHelper
{
    private static readonly Version MinimumGlibcVersion = new(2, 38);

    /// <summary>
    /// Collects fatal environment problems that would prevent the Photino window from
    /// opening on Linux. Returns an empty list on a healthy system, and always on non-Linux
    /// platforms. Each entry is a human-readable description including an install hint.
    /// </summary>
    public static IReadOnlyList<string> GetLinuxStartupIssues()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return [];

        var issues = new List<string>();

        var display = Environment.GetEnvironmentVariable("DISPLAY");
        var waylandDisplay = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");
        if (string.IsNullOrEmpty(display) && string.IsNullOrEmpty(waylandDisplay))
        {
            issues.Add("No display server detected (neither DISPLAY nor WAYLAND_DISPLAY is set). " +
                "Run inside a desktop session, or use Xvfb for headless environments.");
        }

        // Probe via the dynamic loader rather than hardcoded paths so distro layout doesn't matter.
        // The versioned .so.N names are what runtime packages actually install; the bare .so
        // symlinks only ship with -dev packages.
        if (!CanLoadAnyNativeLibrary("libgtk-3.so.0", "libgtk-3.so"))
        {
            issues.Add("GTK 3 not found (libgtk-3.so.0). Install it with your package manager, " +
                "e.g. 'sudo apt install libgtk-3-0'.");
        }

        if (!CanLoadAnyNativeLibrary("libwebkit2gtk-4.1.so.0", "libwebkit2gtk-4.0.so.37"))
        {
            issues.Add("WebKitGTK not found (libwebkit2gtk-4.1.so.0). Install it with your package manager, " +
                "e.g. 'sudo apt install libwebkit2gtk-4.1-0'.");
        }

        var glibcVersion = GetGlibcVersion();
        if (glibcVersion is not null && glibcVersion < MinimumGlibcVersion)
        {
            issues.Add($"GLIBC {glibcVersion} is older than {MinimumGlibcVersion} required by Photino.Native. " +
                "Upgrade to a distribution release that ships a newer glibc (e.g. Ubuntu 24.04+, Debian 13+).");
        }

        return issues;
    }

    private static bool CanLoadAnyNativeLibrary(params ReadOnlySpan<string> libraryNames)
    {
        foreach (var libraryName in libraryNames)
        {
            if (NativeLibrary.TryLoad(libraryName, out var libraryHandle))
            {
                NativeLibrary.Free(libraryHandle);
                return true;
            }
        }

        return false;
    }

    public static bool IsPhotinoNetSupported()
    {
        try
        {
            // Basic platform checks
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                !RuntimeInformation.IsOSPlatform(OSPlatform.Linux) &&
                !RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return false;
            }

            // On Linux, check for known issues
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return CheckLinuxPhotinoSupport();
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool CheckLinuxPhotinoSupport()
    {
        try
        {
            // Check GLIBC version - Photino requires GLIBC 2.38+
            var glibcVersion = GetGlibcVersion();
            if (glibcVersion != null && glibcVersion < new Version(2, 38))
            {
                System.Diagnostics.Debug.WriteLine($"GLIBC {glibcVersion} is too old for Photino (requires 2.38+)");
                return false;
            }

            // Check environment variables that might indicate compatibility issues
            var display = Environment.GetEnvironmentVariable("DISPLAY");
            var waylandDisplay = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");

            if (string.IsNullOrEmpty(display) && string.IsNullOrEmpty(waylandDisplay))
            {
                System.Diagnostics.Debug.WriteLine("No display environment detected");
                return false;
            }

            // Try to check if the native library exists
            var currentDir = AppContext.BaseDirectory;
            var possiblePaths = new[]
            {
                Path.Combine(currentDir, "runtimes", "linux-x64", "native", "Photino.Native.so"),
                Path.Combine(currentDir, "Photino.Native.so"),
                Path.Combine(currentDir, "runtimes", "linux-x64", "native", "libPhotino.Native.so"),
                Path.Combine(currentDir, "libPhotino.Native.so")
            };

            bool hasNativeLib = false;
            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    hasNativeLib = true;
                    System.Diagnostics.Debug.WriteLine($"Found Photino native lib at: {path}");
                    break;
                }
            }

            if (!hasNativeLib)
            {
                System.Diagnostics.Debug.WriteLine("Photino native library not found");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Linux Photino support check failed: {ex}");
            return false;
        }
    }

    private static Version? GetGlibcVersion()
    {
        try
        {
            // Try to get GLIBC version from ldd
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "ldd",
                Arguments = "--version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(psi);
            if (process != null)
            {
                using var reader = process.StandardOutput;
                var output = reader.ReadToEnd();
                process.WaitForExit();

                // Parse output like "ldd (Debian GLIBC 2.36-9+deb12u10) 2.36"
                var lines = output.Split('\n');
                if (lines.Length > 0)
                {
                    var versionLine = lines[0];
                    var match = System.Text.RegularExpressions.Regex.Match(versionLine, @"(\d+\.\d+)$");
                    if (match.Success && Version.TryParse(match.Groups[1].Value, out var version))
                    {
                        System.Diagnostics.Debug.WriteLine($"Detected GLIBC version: {version}");
                        return version;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to get GLIBC version: {ex}");
        }

        return null;
    }

    public static bool IsAvaloniaWebViewSupported()
    {
        try
        {
            // Check if WebView.Avalonia community package would work
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return CheckLinuxWebViewSupport();
            }
            return true; // Generally available on Windows/macOS
        }
        catch
        {
            return false;
        }
    }

    private static bool CheckLinuxWebViewSupport()
    {
        try
        {
            // Check for webkit2gtk libraries
            var webkitLibs = new[]
            {
                "/usr/lib/x86_64-linux-gnu/libwebkit2gtk-4.0.so",
                "/usr/lib/x86_64-linux-gnu/libwebkit2gtk-4.1.so",
                "/usr/lib/libwebkit2gtk-4.0.so",
                "/usr/lib/libwebkit2gtk-4.1.so"
            };

            foreach (var lib in webkitLibs)
            {
                if (File.Exists(lib))
                {
                    System.Diagnostics.Debug.WriteLine($"Found WebKit library: {lib}");
                    return true;
                }
            }

            System.Diagnostics.Debug.WriteLine("WebKit libraries not found");
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"WebView support check failed: {ex}");
            return false;
        }
    }

    public static string GetRecommendedWebViewStrategy()
    {
        if (IsPhotinoNetSupported())
        {
            return "Photino.NET";
        }

        if (IsAvaloniaWebViewSupported())
        {
            return "AvaloniaWebView";
        }

        return "EmbeddedBrowser";
    }

    public static string[] GetAllWebViewStrategies()
    {
        var strategies = new List<string>();

        if (IsPhotinoNetSupported())
        {
            strategies.Add("Photino.NET");
        }

        if (IsAvaloniaWebViewSupported())
        {
            strategies.Add("AvaloniaWebView");
        }

        strategies.Add("EmbeddedBrowser"); // Always available

        return strategies.ToArray();
    }

    public static string GetPhotinoUnsupportedReason()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var glibcVersion = GetGlibcVersion();
            if (glibcVersion != null && glibcVersion < new Version(2, 38))
            {
                return $"GLIBC {glibcVersion} too old (requires 2.38+)";
            }
            return "Linux compatibility issues detected";
        }

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
            !RuntimeInformation.IsOSPlatform(OSPlatform.Linux) &&
            !RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "Unsupported platform";
        }

        return "Unknown compatibility issue";
    }

    public static void LogPlatformInfo()
    {
        System.Diagnostics.Debug.WriteLine("=== Platform Information ===");
        System.Diagnostics.Debug.WriteLine($"OS: {RuntimeInformation.OSDescription}");
        System.Diagnostics.Debug.WriteLine($"Architecture: {RuntimeInformation.OSArchitecture}");
        System.Diagnostics.Debug.WriteLine($"Framework: {RuntimeInformation.FrameworkDescription}");
        System.Diagnostics.Debug.WriteLine($"Photino.NET Supported: {IsPhotinoNetSupported()}");
        System.Diagnostics.Debug.WriteLine($"Avalonia WebView Supported: {IsAvaloniaWebViewSupported()}");
        System.Diagnostics.Debug.WriteLine($"Recommended Strategy: {GetRecommendedWebViewStrategy()}");
        System.Diagnostics.Debug.WriteLine($"All Strategies: {string.Join(", ", GetAllWebViewStrategies())}");
        System.Diagnostics.Debug.WriteLine("===========================");
    }
}