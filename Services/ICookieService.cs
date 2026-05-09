namespace CheapAvaloniaBlazor.Services;

/// <summary>
/// Service for extracting cookies from the WebView2 cookie store.
/// Enables reading cookies set by external sites (e.g., Cloudflare cf_clearance)
/// that are not accessible via document.cookie due to same-origin policy.
///
/// Uses WebView2's CoreWebView2.CookieManager.GetCookiesAsync() under the hood.
/// Only available on Windows (WebView2). Other platforms return empty results.
/// </summary>
public interface ICookieService
{
    /// <summary>
    /// Gets all cookies for the specified URI from the WebView2 cookie store.
    /// </summary>
    /// <param name="uri">The URI to get cookies for (e.g., "https://asuracomic.net").</param>
    /// <returns>Dictionary of cookie name → value pairs.</returns>
    Task<Dictionary<string, string>> GetCookiesAsync(string uri);

    /// <summary>
    /// Gets a specific cookie by name for the specified URI.
    /// </summary>
    /// <param name="uri">The URI to get the cookie for.</param>
    /// <param name="cookieName">The cookie name (e.g., "cf_clearance").</param>
    /// <returns>The cookie value, or null if not found.</returns>
    Task<string?> GetCookieAsync(string uri, string cookieName);

    /// <summary>
    /// Deletes all cookies for the specified domain from the WebView2 cookie store.
    /// </summary>
    /// <param name="domain">The domain to clear cookies for.</param>
    Task DeleteCookiesAsync(string domain);
}
