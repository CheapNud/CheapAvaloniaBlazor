using System.Xml.Linq;

namespace CheapAvaloniaBlazor.Tests;

/// <summary>
/// Guards the static web assets wiring of the NuGet package.
///
/// Background: NuGet only auto-imports build/&lt;PackageId&gt;.props from a package. Because this
/// repo packs the hand-written Build/CheapAvaloniaBlazor.props into that slot, the SDK-generated
/// props (which would import the static web assets registration) is silently dropped at pack
/// time — NU5118. For years that meant _content/CheapAvaloniaBlazor/* returned 404 from packaged
/// consumers even though the asset and its registration were inside every nupkg, which spawned
/// the JavaScriptBridgeExtractor and the embedded-resource serving fallbacks (since removed).
///
/// These tests fail if anyone edits the props/csproj in a way that reintroduces the 404.
/// </summary>
public class PackagingWiringTests
{
    private static readonly string RepoRoot = FindRepoRoot();
    private static readonly string PropsPath = Path.Combine(RepoRoot, "Build", "CheapAvaloniaBlazor.props");
    private static readonly string CsprojPath = Path.Combine(RepoRoot, "CheapAvaloniaBlazor.csproj");
    private static readonly string BridgeJsPath = Path.Combine(RepoRoot, "wwwroot", "cheap-blazor-interop.js");

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

    private static List<XElement> GetPropsImports()
    {
        var propsDocument = XDocument.Load(PropsPath);
        return propsDocument.Descendants()
            .Where(element => element.Name.LocalName == "Import")
            .ToList();
    }

    [Fact]
    public void Props_imports_static_web_assets_registration()
    {
        // The import that makes _content/CheapAvaloniaBlazor/* work from the packaged build slot.
        // Removing it resurrects the silent NU5118 clobber and every consumer 404s the JS bridge.
        var imports = GetPropsImports();

        var staticWebAssetsImport = imports.FirstOrDefault(import =>
            (import.Attribute("Project")?.Value ?? string.Empty).Contains("Microsoft.AspNetCore.StaticWebAssets.props"));

        Assert.NotNull(staticWebAssetsImport);
    }

    [Fact]
    public void Props_imports_static_web_asset_endpoints_registration()
    {
        var imports = GetPropsImports();

        var endpointsImport = imports.FirstOrDefault(import =>
            (import.Attribute("Project")?.Value ?? string.Empty).Contains("Microsoft.AspNetCore.StaticWebAssetEndpoints.props"));

        Assert.NotNull(endpointsImport);
    }

    [Fact]
    public void Static_web_assets_imports_are_guarded_with_exists_conditions()
    {
        // The imported files only exist inside the packaged build folder, not in the source tree.
        // Without an Exists() guard every consumer restore would fail on the missing file.
        var imports = GetPropsImports();

        var packagedImports = imports
            .Where(import => (import.Attribute("Project")?.Value ?? string.Empty).Contains("StaticWebAsset"))
            .ToList();

        Assert.NotEmpty(packagedImports);
        Assert.All(packagedImports, import =>
        {
            var importCondition = import.Attribute("Condition")?.Value;
            Assert.False(string.IsNullOrWhiteSpace(importCondition), "Packaged static web assets import must be guarded with an Exists() condition.");
            Assert.Contains("Exists(", importCondition);
        });
    }

    [Fact]
    public void Csproj_packs_the_hand_written_props_into_the_build_slot()
    {
        // Documents WHY the imports above are mandatory: the hand-written props occupies the
        // build/<PackageId>.props slot, which drops the SDK-generated wiring (NU5118). If this
        // Content item is ever removed, the SDK generates its own props and the imports become
        // redundant (but harmless) — at that point this test and the imports can both go.
        var csprojDocument = XDocument.Load(CsprojPath);

        var propsPackedIntoBuildSlot = csprojDocument.Descendants()
            .Where(element => element.Name.LocalName == "Content")
            .Any(element =>
                (element.Attribute("Include")?.Value ?? string.Empty).EndsWith("CheapAvaloniaBlazor.props") &&
                (element.Attribute("PackagePath")?.Value ?? string.Empty) == "build");

        Assert.True(propsPackedIntoBuildSlot);
    }

    [Fact]
    public void Bridge_js_exists_as_a_plain_static_web_asset()
    {
        // The SDK packs wwwroot/*.js into staticwebassets/ automatically; the file just has to be there.
        Assert.True(File.Exists(BridgeJsPath), $"JS bridge not found at {BridgeJsPath}");

        var bridgeScript = File.ReadAllText(BridgeJsPath);
        Assert.Contains("cheapBlazor", bridgeScript);
    }

    [Fact]
    public void Bridge_js_is_not_embedded_as_a_resource()
    {
        // The EmbeddedResource copy fed the runtime extractor workaround. Both are gone — the
        // bridge serves via the static web assets manifest. Reintroducing the embed would mean
        // shipping the file twice and would suggest the extractor universe is leaking back in.
        var csprojDocument = XDocument.Load(CsprojPath);

        var bridgeEmbeds = csprojDocument.Descendants()
            .Where(element => element.Name.LocalName == "EmbeddedResource")
            .Where(element => (element.Attribute("Include")?.Value ?? string.Empty).Contains("cheap-blazor-interop"))
            .ToList();

        Assert.Empty(bridgeEmbeds);
    }
}
