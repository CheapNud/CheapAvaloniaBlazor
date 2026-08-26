using System.Reflection;

namespace CheapAvaloniaBlazor.Utilities;

/// <summary>
/// Reads the JavaScript this library keeps inside its own assembly.
///
/// These copies are the layer that cannot go missing. Both files normally arrive as static web
/// assets under wwwroot, which is fine on a developer machine and on anything deployed with
/// <c>dotnet publish</c>, but the chain depends on files and manifests that an end-user machine
/// may simply not have. When it breaks the failure is silent from the user's side: a blank white
/// window with no controls and no error.
/// </summary>
public static class EmbeddedAssets
{
    private static readonly Assembly OwnAssembly = typeof(EmbeddedAssets).Assembly;

    // Resolved once. Opening a stream just to test for null is wasteful when the manifest
    // already lists every name.
    private static readonly HashSet<string> ResourceNames =
        new(OwnAssembly.GetManifestResourceNames(), StringComparer.Ordinal);

    /// <summary>Whether the named manifest resource was embedded in this assembly.</summary>
    public static bool Has(string resourceName) => ResourceNames.Contains(resourceName);

    /// <summary>
    /// Opens an embedded asset, or returns null when it was not embedded. The caller owns
    /// the stream.
    /// </summary>
    public static Stream? Open(string resourceName)
        => Has(resourceName) ? OwnAssembly.GetManifestResourceStream(resourceName) : null;
}
