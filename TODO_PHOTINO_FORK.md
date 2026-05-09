# Photino Fork Plan — CheapPhotino

## Why

Photino.NET v4.0.16 doesn't expose cookie access, JS execution, or several other native WebView APIs that all three platform backends already support. Upstream development is slow (~1 major release per quarter, maintainer responds sporadically). CheapAvaloniaBlazor already wraps Photino — forking removes the bottleneck.

## Scope

Fork both repos under CheapNud:
- `tryphotino/photino.Native` → `CheapNud/CheapPhotino.Native`
- `tryphotino/photino.NET` → `CheapNud/CheapPhotino.NET`

Both are Apache-2.0. Rename NuGet packages to `CheapPhotino.Native` / `CheapPhotino.NET`.

## Phase 1: Cookie Manager (unblocks CF bypass)

### Native layer (`CheapPhotino.Native`)

**Photino.h** — Add to API surface:
```cpp
typedef void (*CppGetCookiesCallback)(int count, const char** names, const char** values, const char** domains, const char** paths);
void Photino_GetCookies(Photino* instance, const char* url, CppGetCookiesCallback callback);
void Photino_DeleteCookies(Photino* instance, const char* url, const char* name);
```

**Photino.Windows.cpp** — WebView2:
```cpp
// Already has ICoreWebView2 internally
// Call: webview->get_CookieManager() → GetCookies(url, handler)
// Handler converts ICoreWebView2CookieList to callback arrays
```

**Photino.Linux.cpp** — WebKitGTK:
```cpp
// webkit_web_context_get_cookie_manager(context)
// webkit_cookie_manager_get_cookies(manager, domain, NULL, callback, user_data)
// Finish: webkit_cookie_manager_get_cookies_finish() → GList<SoupCookie>
```

**Photino.Mac.mm** — WKWebView:
```objc
// [webView.configuration.websiteDataStore.httpCookieStore getAllCookies:^(NSArray<NSHTTPCookie*>* cookies) { ... }]
```

**Exports.cpp** — Export:
```cpp
AUTO_API Photino_GetCookies(Photino* instance, const char* url, CppGetCookiesCallback callback);
AUTO_API Photino_DeleteCookies(Photino* instance, const char* url, const char* name);
```

### .NET wrapper (`CheapPhotino.NET`)

**PhotinoWindow.NET.cs**:
```csharp
[DllImport(DllName)] static extern void Photino_GetCookies(IntPtr instance, string url, CppGetCookiesCallback callback);

public Task<List<CheapPhotinoCookie>> GetCookiesAsync(string uri) { ... }
public Task DeleteCookiesAsync(string uri, string? name = null) { ... }
```

**CheapPhotinoCookie.cs** (new):
```csharp
public record CheapPhotinoCookie(string Name, string Value, string Domain, string Path, bool HttpOnly, bool Secure, DateTime? Expires);
```

### CheapAvaloniaBlazor

- Update package reference: `Photino.NET` → `CheapPhotino.NET`
- `CookieService.GetCookiesAsync()` delegates to `PhotinoWindow.GetCookiesAsync()`
- Remove FlareSolverr as primary CF path for Desktop (keep as Web fallback)

## Phase 2: ExecuteScript (unblocks arbitrary JS execution)

### Native layer
```cpp
// Windows: ICoreWebView2::ExecuteScript(script, handler)
// Linux: webkit_web_view_evaluate_javascript() (WebKitGTK 2.40+)
// macOS: [WKWebView evaluateJavaScript:completionHandler:]
void Photino_ExecuteScript(Photino* instance, const char* script, CppScriptResultCallback callback);
```

### .NET wrapper
```csharp
public Task<string> ExecuteScriptAsync(string script) { ... }
```

### CheapAvaloniaBlazor
- `PhotinoMessageHandler.ExecuteScriptAsync()` delegates to native instead of `SendWebMessage` + eval workaround
- Remove the `DangerousScriptPatterns` security filter (native execution is sandboxed by the WebView itself)

## Phase 3: Future additions (as needed)
- `NavigateAsync(url)` with completion callback
- `GetCurrentUrl()` property
- Download interception
- Custom user data folder path
- Print support

## Build Matrix

| Platform | Compiler | WebView SDK |
|----------|----------|-------------|
| Windows x64 | MSVC | WebView2 (bundled via NuGet) |
| Linux x64 | GCC | libwebkit2gtk-4.1-dev |
| macOS arm64+x64 | Clang | WKWebView (system) |

GitHub Actions CI builds all three, publishes NuGet packages.

## Estimated Effort

- Phase 1 (cookies): ~50-100 lines per platform C++ + .NET wrapper. 1-2 days.
- Phase 2 (ExecuteScript): ~30-50 lines per platform. Half day.
- CI/Build setup: 1 day (GitHub Actions for 3 platforms + NuGet publish).

## Upstream Sync

Cherry-pick upstream `tryphotino/photino.Native` fixes as needed. Upstream averages ~4 commits/quarter — trivial to track. Add `upstream` remote and periodically `git log upstream/master..HEAD` to see divergence.
