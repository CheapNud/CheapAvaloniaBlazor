using System.Text.RegularExpressions;
using CheapAvaloniaBlazor.Services;

namespace CheapAvaloniaBlazor.Utilities;

/// <summary>
/// Fetches the app's own root document once at startup and confirms that every script and
/// stylesheet it references actually comes back with content.
///
/// A Blazor desktop app whose assets do not resolve does not crash and does not show an error.
/// It serves its HTML, the WebView renders a blank white window with no controls, and nothing
/// anywhere says why. Worse, a broken static web asset endpoint answers <c>200</c> with a
/// zero-length body rather than 404, so even a network trace looks healthy. This check turns
/// that into one named error line per missing asset.
///
/// It runs alongside the WebView rather than ahead of it, so the window may already be blank by
/// the time the lines appear. The point is that they appear at all, naming files, instead of the
/// failure being invisible everywhere. Each request is bounded by the 5s client timeout and the
/// host's cancellation token, so a wedged endpoint cannot leave this running.
/// </summary>
internal static partial class StartupAssetCheck
{
    [GeneratedRegex("""(?:src|href)\s*=\s*["'](?<url>[^"']+)["']""", RegexOptions.IgnoreCase)]
    private static partial Regex AssetReference();

    /// <summary>
    /// Runs the check. Never throws: a diagnostic must not be able to take down the app it is
    /// diagnosing. Failures are reported through <paramref name="logger"/>.
    /// </summary>
    public static async Task RunAsync(string baseUrl, DiagnosticLogger logger, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = HttpClientFactory.CreateForServerCheck();

            var document = await client.GetStringAsync(baseUrl, cancellationToken).ConfigureAwait(false);
            var assetPaths = ExtractAssetPaths(document);

            if (assetPaths.Count == 0)
            {
                logger.LogVerbose("Startup asset check: the root document references no framework or _content assets");
                return;
            }

            foreach (var assetPath in assetPaths)
            {
                await CheckOneAsync(client, baseUrl, assetPath, logger, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            logger.LogVerbose("Startup asset check could not run: {Message}", ex.Message);
        }
    }

    /// <summary>
    /// Pulls the app-relative script and stylesheet URLs out of the root document, keeping only
    /// the ones this host is responsible for serving. Absolute URLs (a CDN, Google Fonts) are
    /// somebody else's problem and an offline machine failing to load them is not a white screen.
    /// </summary>
    internal static IReadOnlyList<string> ExtractAssetPaths(string document)
    {
        var paths = new List<string>();

        foreach (Match match in AssetReference().Matches(document))
        {
            var url = match.Groups["url"].Value.Trim();
            if (url.Length == 0 || url.Contains("://") || url.StartsWith("//", StringComparison.Ordinal))
            {
                continue;
            }

            var normalized = "/" + url.TrimStart('/');
            var isOurs = normalized.StartsWith(Constants.Paths.FrameworkRequestPrefix, StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith(Constants.Paths.ContentRequestPrefix, StringComparison.OrdinalIgnoreCase);

            if (isOurs && !paths.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            {
                paths.Add(normalized);
            }
        }

        return paths;
    }

    private static async Task CheckOneAsync(
        HttpClient client,
        string baseUrl,
        string assetPath,
        DiagnosticLogger logger,
        CancellationToken cancellationToken)
    {
        using var response = await client
            .GetAsync(baseUrl.TrimEnd('/') + assetPath, cancellationToken)
            .ConfigureAwait(false);

        var body = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);

        if (response.IsSuccessStatusCode && body.Length > 0)
        {
            logger.LogVerbose("Startup asset check: {Path} OK ({Length} bytes)", assetPath, body.Length);
            return;
        }

        logger.LogError(
            "Startup asset check: {Path} returned {StatusCode} with {Length} bytes. The window will " +
            "render blank or unstyled and no other error will be reported. This file must exist under " +
            "wwwroot in a deployed app. Deploy with 'dotnet publish' rather than by copying build " +
            "output, which leaves _content assets behind in the build machine's NuGet cache.",
            assetPath, (int)response.StatusCode, body.Length);
    }
}
