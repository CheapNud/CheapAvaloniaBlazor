using System.Xml.Linq;
using CheapAvaloniaBlazor.Utilities;

namespace CheapAvaloniaBlazor.Tests;

/// <summary>
/// Pins the blazor.web.js version-selection and staleness behavior.
///
/// The NuGet cache is machine-global, so Microsoft.AspNetCore.App.Internal.Assets can hold
/// versions from unrelated projects. "Highest version wins" hands a net10 app a preview-12
/// script; an unscoped MSBuild glob lets alphabetical order decide (where "9.x" beats "10.x").
/// Selection must follow the runtime the app is actually on, and an already-extracted copy
/// must be refreshed after a .NET update instead of being served forever.
/// </summary>
public class BlazorFrameworkVersionTests
{
    private static string VersionDir(string version) => Path.Combine("C:", "cache", "internal.assets", version);

    [Fact]
    public void Selection_prefers_runtime_major_over_higher_version()
    {
        var cacheDirs = new[] { VersionDir("10.0.2"), VersionDir("11.0.0-preview.1.26104.118") };

        var ordered = BlazorFrameworkExtractor.OrderVersionDirectories(cacheDirs, preferredMajor: 10);

        Assert.Equal(VersionDir("10.0.2"), ordered[0]);
    }

    [Fact]
    public void Selection_picks_highest_patch_within_the_preferred_major()
    {
        // String sort would put 10.0.2 after 10.0.10 — semantic comparison must win.
        var cacheDirs = new[] { VersionDir("10.0.2"), VersionDir("10.0.10"), VersionDir("9.0.11") };

        var ordered = BlazorFrameworkExtractor.OrderVersionDirectories(cacheDirs, preferredMajor: 10);

        Assert.Equal(VersionDir("10.0.10"), ordered[0]);
    }

    [Fact]
    public void Selection_falls_back_to_highest_version_when_runtime_major_is_not_cached()
    {
        var cacheDirs = new[] { VersionDir("9.0.11"), VersionDir("10.0.2") };

        var ordered = BlazorFrameworkExtractor.OrderVersionDirectories(cacheDirs, preferredMajor: 12);

        Assert.Equal(VersionDir("10.0.2"), ordered[0]);
    }

    [Fact]
    public void Selection_drops_directories_that_are_not_versions()
    {
        var cacheDirs = new[] { VersionDir("not-a-version"), VersionDir("10.0.2") };

        var ordered = BlazorFrameworkExtractor.OrderVersionDirectories(cacheDirs, preferredMajor: 10);

        Assert.Single(ordered);
        Assert.Equal(VersionDir("10.0.2"), ordered[0]);
    }

    [Fact]
    public void ParseVersion_strips_prerelease_suffix_and_rejects_junk()
    {
        Assert.Equal(new Version(11, 0, 0), BlazorFrameworkExtractor.ParseVersion("11.0.0-preview.1.26104.118"));
        Assert.Equal(new Version(10, 0, 2), BlazorFrameworkExtractor.ParseVersion("10.0.2"));
        Assert.Null(BlazorFrameworkExtractor.ParseVersion("not-a-version"));
    }

    [Fact]
    public void Stale_when_target_is_missing()
    {
        using var scratch = new ScratchDir();
        var sourceFile = scratch.WriteFile("source.js", "fresh content");

        Assert.True(BlazorFrameworkExtractor.IsTargetStale(sourceFile, Path.Combine(scratch.Root, "missing.js")));
    }

    [Fact]
    public void Stale_when_lengths_differ()
    {
        using var scratch = new ScratchDir();
        var sourceFile = scratch.WriteFile("source.js", "new framework script, longer than before");
        var targetFile = scratch.WriteFile("target.js", "old script");

        Assert.True(BlazorFrameworkExtractor.IsTargetStale(sourceFile, targetFile));
    }

    [Fact]
    public void Stale_when_source_is_newer_than_target_at_equal_length()
    {
        using var scratch = new ScratchDir();
        var sourceFile = scratch.WriteFile("source.js", "same-length-a");
        var targetFile = scratch.WriteFile("target.js", "same-length-b");
        File.SetLastWriteTimeUtc(targetFile, DateTime.UtcNow.AddDays(-30));

        Assert.True(BlazorFrameworkExtractor.IsTargetStale(sourceFile, targetFile));
    }

    [Fact]
    public void Not_stale_when_target_matches_length_and_is_as_recent_as_source()
    {
        // File.Copy preserves the source timestamp, so an up-to-date extraction compares equal.
        using var scratch = new ScratchDir();
        var sourceFile = scratch.WriteFile("source.js", "same-length-a");
        var targetFile = Path.Combine(scratch.Root, "target.js");
        File.Copy(sourceFile, targetFile);

        Assert.False(BlazorFrameworkExtractor.IsTargetStale(sourceFile, targetFile));
    }

    [Fact]
    public void Build_targets_glob_is_scoped_to_the_apps_framework_version()
    {
        // An unscoped ** glob copies every cached version and lets alphabetical order pick the
        // winner. The glob must be narrowed by the TFM-derived major.minor property.
        var targetsPath = Path.Combine(FindRepoRoot(), "Build", "CheapAvaloniaBlazor.targets");
        var targetsDocument = XDocument.Load(targetsPath);

        var frameworkFileIncludes = targetsDocument.Descendants()
            .Where(element => element.Name.LocalName == "BlazorFrameworkFiles")
            .Select(element => element.Attribute("Include")?.Value ?? string.Empty)
            .ToList();

        Assert.NotEmpty(frameworkFileIncludes);
        Assert.All(frameworkFileIncludes, include =>
        {
            Assert.DoesNotContain("**", include);
            Assert.Contains("$(_BlazorTfmMajorMinor)", include);
        });
    }

    [Fact]
    public void Build_targets_tfm_regex_tolerates_the_leading_v()
    {
        // $(TargetFrameworkVersion) is "v11.0" — an anchored ^\d+\.\d+ regex silently matches
        // nothing and the glob degrades to matching no versions at all.
        var targetsPath = Path.Combine(FindRepoRoot(), "Build", "CheapAvaloniaBlazor.targets");
        var targetsText = File.ReadAllText(targetsPath);

        Assert.DoesNotContain(@"'^\d+\.\d+'", targetsText);
        Assert.Contains(@"'\d+\.\d+'", targetsText);
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null && !File.Exists(Path.Combine(current.FullName, "CheapAvaloniaBlazor.csproj")))
        {
            current = current.Parent;
        }

        return current?.FullName
            ?? throw new InvalidOperationException("Could not locate the repo root (CheapAvaloniaBlazor.csproj) above the test output directory.");
    }

    /// <summary>Temp directory that cleans itself up after the test.</summary>
    private sealed class ScratchDir : IDisposable
    {
        public string Root { get; } = Directory.CreateTempSubdirectory("cab-tests-").FullName;

        public string WriteFile(string fileName, string fileContent)
        {
            var filePath = Path.Combine(Root, fileName);
            File.WriteAllText(filePath, fileContent);
            return filePath;
        }

        public void Dispose()
        {
            try { Directory.Delete(Root, recursive: true); } catch { /* best effort */ }
        }
    }
}
