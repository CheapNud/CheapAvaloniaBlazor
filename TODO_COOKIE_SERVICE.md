# CookieService — WebView Cookie Extraction

## Problem

CheapManga needs `cf_clearance` cookies from Cloudflare-protected external domains.
These cookies are HttpOnly — JS cannot read them. Same-origin policy also prevents
reading cookies from the Blazor localhost context.

## Architecture

```
CheapAvaloniaBlazor (ICookieService)
    └── CookieService delegates to Photino.NET
            └── Photino delegates to native WebView
                    ├── Windows:  WebView2 → ICoreWebView2CookieManager
                    ├── Linux:    WebKitGTK → WebKitCookieManager
                    └── macOS:    WKWebView → WKHTTPCookieStore
```

CheapAvaloniaBlazor does NOT take a direct dependency on any platform WebView SDK.
Cookie access goes through Photino.NET which already wraps the platform WebView.
Photino just needs to expose the cookie API it already has access to internally.

## Current state

- `ICookieService` interface: ready
- `CookieService` implementation: ready, waiting for Photino API
- Fallback: FlareSolverr sidecar (works now on both Web and Desktop)

## What Photino.NET needs

A method on `PhotinoWindow` like:

```csharp
// Option A: Simple dictionary return
public Task<IReadOnlyList<Cookie>> GetCookiesAsync(string uri);

// Option B: CookieManager property
public ICookieManager CookieManager { get; }
```

Under the hood this calls:
- **Windows**: `ICoreWebView2CookieManager.GetCookiesAsync(uri)`
- **Linux**: `webkit_cookie_manager_get_cookies()`
- **macOS**: `WKHTTPCookieStore.getAllCookies()`

All three platforms already have this capability in their native WebView APIs.
Photino just doesn't expose it yet.

## Files

- `Services/ICookieService.cs` — Abstraction (3 methods)
- `Services/CookieService.cs` — Implementation (delegates to Photino when available)
- `TODO_COOKIE_SERVICE.md` — This file

## When Photino adds cookie access

Update `CookieService.GetCookiesAsync()`:

```csharp
var nativeCookies = await _mainWindow.GetCookiesAsync(uri);
return nativeCookies.ToDictionary(c => c.Name, c => c.Value);
```

No other changes needed — ICookieService consumers are already wired.
