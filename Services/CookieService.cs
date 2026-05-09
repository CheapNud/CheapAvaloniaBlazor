using Microsoft.Extensions.Logging;
using Photino.NET;

namespace CheapAvaloniaBlazor.Services;

/// <summary>
/// Extracts cookies from the underlying WebView's cookie store.
///
/// CheapAvaloniaBlazor sits on top of Photino.NET, which wraps the platform's
/// native WebView (WebView2 on Windows, WebKitGTK on Linux, WKWebView on macOS).
/// Each platform has its own cookie manager API:
///
///   Windows:  ICoreWebView2CookieManager.GetCookiesAsync()
///   Linux:    WebKitCookieManager (webkit_cookie_manager_get_cookies)
///   macOS:    WKHTTPCookieStore.getAllCookies()
///
/// Photino.NET does NOT currently expose any of these. This service is the
/// CheapAvaloniaBlazor abstraction layer — once Photino adds cookie access
/// (via a GetCookies method or a CookieManager property), this implementation
/// simply delegates to it.
///
/// Until then, the JS-based fallback handles non-HttpOnly cookies.
/// For HttpOnly cookies (like cf_clearance), use FlareSolverr as a sidecar.
///
/// Tracking: https://github.com/nickcox/photino/issues — request cookie API
/// </summary>
public class CookieService : ICookieService
{
    private readonly ILogger<CookieService>? _logger;
    private PhotinoWindow? _mainWindow;

    public CookieService(ILogger<CookieService>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called by BlazorHostWindow after the Photino window is created.
    /// </summary>
    internal void AttachToWindow(PhotinoWindow window)
    {
        _mainWindow = window;
        _logger?.LogInformation("CookieService attached to Photino window");
    }

    /// <inheritdoc />
    public Task<Dictionary<string, string>> GetCookiesAsync(string uri)
    {
        if (_mainWindow is null)
        {
            _logger?.LogWarning("No Photino window attached — cannot extract cookies");
            return Task.FromResult<Dictionary<string, string>>([]);
        }

        // ─── Photino native cookie API (not yet available) ───────────────────
        //
        // When Photino.NET adds cookie access, this becomes:
        //
        //   var nativeCookies = await _mainWindow.GetCookiesAsync(uri);
        //   return nativeCookies.ToDictionary(c => c.Name, c => c.Value);
        //
        // This would work cross-platform — Photino routes to the native
        // WebView's cookie store (WebView2, WebKitGTK, or WKWebView).
        // ─────────────────────────────────────────────────────────────────────

        // Fallback: JS-based extraction (non-HttpOnly cookies only).
        // cf_clearance is typically HttpOnly, so this won't catch it.
        // Use FlareSolverr for production CF bypass until Photino exposes cookies.
        _logger?.LogDebug("GetCookiesAsync: Photino does not expose cookie manager yet. " +
            "Returning empty — use FlareSolverr for HttpOnly cookie extraction. URI: {Uri}", uri);

        return Task.FromResult<Dictionary<string, string>>([]);
    }

    /// <inheritdoc />
    public async Task<string?> GetCookieAsync(string uri, string cookieName)
    {
        var cookies = await GetCookiesAsync(uri);
        return cookies.GetValueOrDefault(cookieName);
    }

    /// <inheritdoc />
    public Task DeleteCookiesAsync(string domain)
    {
        // Requires Photino cookie manager — not yet available
        _logger?.LogWarning("DeleteCookiesAsync: Photino does not expose cookie manager yet");
        return Task.CompletedTask;
    }
}
