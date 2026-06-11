namespace CheapAvaloniaBlazor.Utilities;

/// <summary>
/// Extracts blazor.web.js from the NuGet cache to wwwroot/_framework/ for serving.
///
/// WHY THIS IS NEEDED:
/// In .NET 10, blazor.web.js ships in the Microsoft.AspNetCore.App.Internal.Assets NuGet package.
/// The MSBuild targets in that package register it as a static web asset, but ONLY when:
///   1. The project uses Microsoft.NET.Sdk.Web (UsingMicrosoftNETSdkWeb == true), AND
///   2. OutputType == "Exe" (console subsystem)
///
/// Desktop apps use OutputType=WinExe (windowed subsystem), so the targets are skipped entirely
/// — even with Sdk.Web. This means blazor.web.js never enters the static web assets manifest,
/// and neither UseStaticWebAssets() nor MapStaticAssets() can serve it.
///
/// This extractor copies the file from the NuGet cache at runtime so UseStaticFiles() can serve it.
/// </summary>
public static class BlazorFrameworkExtractor
{
    private static bool _extracted;
    private static string? _extractedPath;

    /// <summary>
    /// Extracts blazor.web.js to wwwroot/_framework/ if not already present.
    /// </summary>
    /// <param name="wwwrootPath">The physical wwwroot directory path.</param>
    /// <param name="logger">Diagnostic logger for progress reporting.</param>
    /// <returns>The path to the extracted file, or null if extraction failed.</returns>
    public static string? ExtractBlazorFrameworkJs(string wwwrootPath, Services.DiagnosticLogger logger)
    {
        var targetDir = Path.Combine(wwwrootPath, Constants.BlazorFramework.FrameworkDirectory);
        var targetPath = Path.Combine(targetDir, Constants.BlazorFramework.BlazorWebJsFileName);

        // Already extracted this session
        if (_extracted && _extractedPath != null && File.Exists(_extractedPath))
        {
            logger.LogVerbose("blazor.web.js already extracted to: {Path}", _extractedPath);
            return _extractedPath;
        }

        try
        {
            var sourcePath = FindBlazorWebJs(logger);

            // Present on disk (MSBuild target or previous run) and still matching the cache
            // source — use it. A bare exists-check is not enough: after a .NET update the old
            // copy would be served forever, so compare against the resolved source first.
            if (File.Exists(targetPath))
            {
                if (sourcePath == null || !IsTargetStale(sourcePath, targetPath))
                {
                    _extractedPath = targetPath;
                    _extracted = true;
                    logger.LogVerbose("blazor.web.js already exists at: {Path}", targetPath);
                    return targetPath;
                }

                logger.LogInformation("blazor.web.js at {Target} is stale compared to {Source} - refreshing",
                    targetPath, sourcePath);
            }

            if (sourcePath == null)
            {
                logger.LogError("Could not find blazor.web.js in NuGet cache. " +
                    "Ensure Microsoft.AspNetCore.App.Internal.Assets is restored (dotnet restore).");
                return null;
            }

            Directory.CreateDirectory(targetDir);
            File.Copy(sourcePath, targetPath, overwrite: true);

            _extractedPath = targetPath;
            _extracted = true;

            logger.LogInformation("Extracted blazor.web.js to: {Path} (source: {Source})",
                targetPath, sourcePath);

            return targetPath;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to extract blazor.web.js");
            return null;
        }
    }

    /// <summary>
    /// True when the extracted copy no longer matches the NuGet cache source.
    /// File.Copy preserves the source timestamp, so an up-to-date copy has the same
    /// length and an equal-or-newer write time; a freshly restored .NET update has a
    /// newer write time (and usually a different length) and wins.
    /// </summary>
    internal static bool IsTargetStale(string sourcePath, string targetPath)
    {
        var sourceInfo = new FileInfo(sourcePath);
        var targetInfo = new FileInfo(targetPath);

        if (!targetInfo.Exists)
            return true;

        if (sourceInfo.Length != targetInfo.Length)
            return true;

        return sourceInfo.LastWriteTimeUtc > targetInfo.LastWriteTimeUtc;
    }

    /// <summary>
    /// Searches the NuGet package cache for blazor.web.js from the internal assets package.
    /// </summary>
    private static string? FindBlazorWebJs(Services.DiagnosticLogger logger)
    {
        var nugetRoot = GetNuGetPackageRoot();
        if (nugetRoot == null || !Directory.Exists(nugetRoot))
        {
            logger.LogVerbose("NuGet package root not found");
            return null;
        }

        logger.LogVerbose("Searching NuGet cache at: {Root}", nugetRoot);

        var packageDir = Path.Combine(nugetRoot, Constants.BlazorFramework.InternalAssetsPackageName);
        if (!Directory.Exists(packageDir))
        {
            logger.LogVerbose("Package directory not found: {Dir}", packageDir);
            return null;
        }

        // Prefer the cache version matching the runtime the app is actually on — the cache is
        // machine-global, so "highest version" can hand a net10 app a preview-12 script from an
        // unrelated project. Within the matching major take the highest; only fall back to other
        // majors when the runtime's own assets aren't restored.
        var runtimeMajor = Environment.Version.Major;
        var versionDirs = OrderVersionDirectories(Directory.GetDirectories(packageDir), runtimeMajor);

        foreach (var versionDir in versionDirs)
        {
            var candidate = Path.Combine(versionDir,
                Constants.BlazorFramework.FrameworkDirectory,
                Constants.BlazorFramework.BlazorWebJsFileName);

            if (File.Exists(candidate))
            {
                var candidateVersion = ParseVersion(Path.GetFileName(versionDir));
                if (candidateVersion?.Major != runtimeMajor)
                {
                    logger.LogVerbose("No blazor.web.js for runtime major {RuntimeMajor} in cache - falling back to {Version}",
                        runtimeMajor, Path.GetFileName(versionDir));
                }

                logger.LogVerbose("Found blazor.web.js at: {Path}", candidate);
                return candidate;
            }
        }

        logger.LogVerbose("blazor.web.js not found in any version of {Package}", packageDir);
        return null;
    }

    /// <summary>
    /// Orders version directories so the preferred (runtime) major comes first, highest version
    /// within it; non-matching majors follow, highest first, as a fallback. Non-version
    /// directory names are dropped. Sorts by parsed Version so 10.0.2 > 9.0.10 — string
    /// sort gets this wrong.
    /// </summary>
    internal static IReadOnlyList<string> OrderVersionDirectories(IEnumerable<string> directories, int preferredMajor)
    {
        return directories
            .Select(directory => new { Path = directory, Version = ParseVersion(System.IO.Path.GetFileName(directory)) })
            .Where(entry => entry.Version is not null)
            .OrderByDescending(entry => entry.Version!.Major == preferredMajor)
            .ThenByDescending(entry => entry.Version)
            .Select(entry => entry.Path)
            .ToList();
    }

    /// <summary>
    /// Parses a version string, stripping NuGet pre-release suffixes (e.g. "10.0.2-preview.1" → 10.0.2).
    /// Returns null for non-version directory names.
    /// </summary>
    internal static Version? ParseVersion(string versionString)
    {
        // Strip pre-release suffix: "10.0.2-preview.1" → "10.0.2"
        var dashIndex = versionString.IndexOf('-');
        var cleanVersion = dashIndex >= 0 ? versionString[..dashIndex] : versionString;
        return Version.TryParse(cleanVersion, out var parsed) ? parsed : null;
    }

    /// <summary>
    /// Resolves the NuGet global packages folder.
    /// Checks NUGET_PACKAGES env var first, then falls back to the default location.
    /// </summary>
    private static string? GetNuGetPackageRoot()
    {
        // Environment variable takes priority
        var envPath = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        if (!string.IsNullOrEmpty(envPath) && Directory.Exists(envPath))
            return envPath;

        // Default location: ~/.nuget/packages
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrEmpty(home))
            return null;

        return Path.Combine(home, ".nuget", "packages");
    }
}
