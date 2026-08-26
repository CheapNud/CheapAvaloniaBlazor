using CheapAvaloniaBlazor.Utilities;

namespace CheapAvaloniaBlazor.Tests;

/// <summary>
/// Pins which references the startup check follows.
///
/// It exists to name the cause of a blank white window, so it has to pick up exactly the assets
/// this host is responsible for serving and stay silent about the rest. Following an external URL
/// would turn "the machine is offline" into a scary error about the app being broken; skipping a
/// local one would leave the original silent failure in place.
/// </summary>
public class StartupAssetCheckTests
{
    private const string SampleDocument = """
        <!DOCTYPE html>
        <html>
        <head>
            <link href="https://fonts.googleapis.com/css?family=Roboto" rel="stylesheet" />
            <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
            <base href="/" />
        </head>
        <body>
            <script src="_framework/blazor.web.js"></script>
            <script src="_content/MudBlazor/MudBlazor.min.js"></script>
            <script src='_content/CheapAvaloniaBlazor/cheap-blazor-interop.js'></script>
            <img src="images/logo.png" />
        </body>
        </html>
        """;

    [Fact]
    public void Follows_every_local_framework_and_content_asset()
    {
        var paths = StartupAssetCheck.ExtractAssetPaths(SampleDocument);

        Assert.Equal(
            [
                "/_content/MudBlazor/MudBlazor.min.css",
                "/_framework/blazor.web.js",
                "/_content/MudBlazor/MudBlazor.min.js",
                "/_content/CheapAvaloniaBlazor/cheap-blazor-interop.js",
            ],
            paths);
    }

    [Fact]
    public void Ignores_external_urls_and_unrelated_local_files()
    {
        var paths = StartupAssetCheck.ExtractAssetPaths(SampleDocument);

        // A CDN font failing on an offline machine is not a blank window, and app images are not
        // this library's business.
        Assert.DoesNotContain(paths, path => path.Contains("fonts.googleapis.com"));
        Assert.DoesNotContain(paths, path => path.Contains("logo.png"));
        Assert.DoesNotContain(paths, path => path == "/");
    }

    [Fact]
    public void Ignores_protocol_relative_urls()
    {
        var paths = StartupAssetCheck.ExtractAssetPaths(
            """<script src="//cdn.example.com/_content/thing.js"></script>""");

        Assert.Empty(paths);
    }

    [Fact]
    public void Reports_each_asset_once_even_when_referenced_twice()
    {
        var paths = StartupAssetCheck.ExtractAssetPaths("""
            <script src="_framework/blazor.web.js"></script>
            <script src="/_framework/blazor.web.js"></script>
            """);

        Assert.Equal(["/_framework/blazor.web.js"], paths);
    }
}
