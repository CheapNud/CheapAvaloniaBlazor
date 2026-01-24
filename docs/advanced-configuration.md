# Advanced Configuration Reference

This document provides comprehensive technical reference for advanced configuration options in CheapAvaloniaBlazor. For getting started, see the [README](../README.md).

## Table of Contents

1. [Overview](#overview)
2. [HostBuilder Fluent API](#hostbuilder-fluent-api)
3. [Window Configuration](#window-configuration)
4. [Server Configuration](#server-configuration)
5. [Render Modes](#render-modes)
6. [SignalR Configuration](#signalr-configuration)
7. [Hot Reload](#hot-reload)
8. [Custom Pipeline & Endpoints](#custom-pipeline--endpoints)
9. [HTTP Clients](#http-clients)
10. [Web View Configuration](#web-view-configuration)
11. [Injection & Advanced Features](#injection--advanced-features)
12. [Complete Configuration Examples](#complete-configuration-examples)
13. [Configuration Options Reference](#configuration-options-reference)

## Overview

CheapAvaloniaBlazor provides a fluent API through `HostBuilder` for configuring desktop Blazor applications. The builder pattern allows for elegant, chainable configuration that combines:

- **Window management** (size, position, chrome, icons)
- **Server configuration** (ports, paths, HTTPS)
- **Component hosting** (render modes, root components)
- **Network features** (SignalR, HTTP clients)
- **Development tools** (diagnostics, dev tools, hot reload)

### Getting Started

```csharp
using CheapAvaloniaBlazor.Hosting;

var builder = new HostBuilder()
    .WithTitle("My Desktop App")
    .WithSize(1200, 800)
    .AddMudBlazor()
    .UsePort(5000);

var window = builder.Build();
window.Show();
```

## HostBuilder Fluent API

The `HostBuilder` class provides a fluent interface for configuration. All methods return the builder itself, allowing for method chaining:

```csharp
var builder = new HostBuilder()
    .WithTitle("Application")
    .WithSize(1400, 900)
    .UsePort(5001)
    .EnableConsoleLogging();
```

### Key Properties

```csharp
// Access underlying services
var services = builder.Services;

// Access current options
var options = builder.Options;
```

### Building the Window

Two approaches:

```csharp
// Default: Creates BlazorHostWindow
var window = builder.Build();

// Custom window type (must implement IBlazorWindow)
var window = builder.Build<CustomBlazorWindow>();
```

## Window Configuration

### Title

```csharp
builder.WithTitle("Advanced Desktop App")
```

Default: `"Blazor Desktop App"`

### Size

Set window width and height in pixels:

```csharp
builder.WithSize(1400, 900)
```

Defaults: 1200x800

### Position

Control window placement:

```csharp
// Center on screen (default)
builder.CenterWindow(true)

// Custom position
builder.WithPosition(100, 50)  // Automatically disables centering
builder.CenterWindow(false)     // Disable centering
```

When both `CenterWindow(true)` and `WithPosition()` are set, position takes precedence after being configured last.

### Window Chrome

Control window frame and decorations:

```csharp
// Show/hide title bar and borders
builder.Chromeless(false)   // Show chrome (default)
builder.Chromeless(true)    // Hide chrome/borders
```

### Resizable

Allow window resizing:

```csharp
builder.Resizable(true)   // Allow resizing (default)
builder.Resizable(false)  // Fixed size window
```

### Window Icon

Set custom window icon:

```csharp
builder.WithIcon("path/to/icon.ico")
builder.WithIcon("Assets/app.png")
```

The icon file is loaded from the specified path. If the file doesn't exist, a warning is logged and the default icon is used.

### Zoom Level

Set default zoom/scale percentage:

```csharp
builder.ConfigureOptions(opts => opts.DefaultZoom = 125)  // 125% zoom
```

Default: 100

### Advanced Window Configuration

For fine-grained control over window properties:

```csharp
builder.ConfigureWindow(window =>
{
    window.MinHeight = 480;
    window.MinWidth = 640;
    window.ShowInTaskbar = true;
    // ... other Avalonia window properties
})
```

## Server Configuration

### Port

Configure the port for the internal Blazor server:

```csharp
builder.UsePort(5001)
builder.UsePort(3000)
```

Default: 5000

The server uses `http://localhost:{port}` by default. This is an internal server—the application doesn't need external access.

### HTTPS

Enable HTTPS for the Blazor server:

```csharp
builder.UseHttps()        // Enable HTTPS
builder.UseHttps(true)    // Enable HTTPS
builder.UseHttps(false)   // Disable HTTPS (default)
```

When HTTPS is enabled, a self-signed certificate is generated automatically.

### Content Root

Specify the root directory for application files:

```csharp
builder.UseContentRoot("./content")
builder.UseContentRoot("/absolute/path/to/content")
```

This affects where Blazor looks for components, razor files, and other application assets.

### Web Root

Specify the directory for static files (CSS, JavaScript, images):

```csharp
builder.UseWebRoot("./wwwroot")
builder.UseWebRoot("./public")
```

Static files in this directory are served as-is to the web view. Default: `wwwroot`

## Render Modes

### Understanding Render Modes

CheapAvaloniaBlazor supports two render modes, configured in your `_Host.cshtml`:

| Mode | Pre-rendering | Interactivity | Use Case |
|------|---|---|---|
| **Server** | No | Immediate | Interactive from page load |
| **ServerPrerendered** | Yes | After hydration | Fast first paint + smooth interactivity |

Note: The `WithRenderMode()` method is informational only. Actual render mode is set in `_Host.cshtml`:

```html
<component type="typeof(App)" render-mode="ServerPrerendered" />
<!-- or -->
<component type="typeof(App)" render-mode="Server" />
```

### Configuring Render Mode

```csharp
// Set the recommended mode for documentation/reference
builder.WithRenderMode("ServerPrerendered")
builder.WithRenderMode("Server")
```

Access via:
```csharp
var renderMode = builder.Options.RecommendedRenderMode;
```

## SignalR Configuration

SignalR handles real-time communication between Blazor components and the server.

### Message Size Limits

Configure maximum message size:

```csharp
builder.ConfigureOptions(options =>
{
    // 64 KB messages
    options.MaximumReceiveMessageSize = 64 * 1024;

    // 1 MB messages
    options.MaximumReceiveMessageSize = 1024 * 1024;
});
```

Default: 32 KB

Use larger limits for:
- File uploads
- Large data transfers
- Image/media operations

### Auto-Reconnect

Enable automatic reconnection when connection is lost:

```csharp
builder.ConfigureOptions(options =>
{
    options.EnableAutoReconnect = true;  // Default
    options.EnableAutoReconnect = false; // Disable
});
```

### Reconnect Intervals

Configure retry intervals in seconds:

```csharp
builder.ConfigureOptions(options =>
{
    // Retry immediately, then after 2s, 10s, 30s, then give up
    options.ReconnectIntervals = new[] { 0, 2, 10, 30 };

    // More aggressive retry
    options.ReconnectIntervals = new[] { 0, 1, 2, 5, 10 };

    // Only retry once
    options.ReconnectIntervals = new[] { 0, 5 };
});
```

Default: `{ 0, 2, 10, 30 }`

The array represents retry attempts. When the last interval is exhausted, reconnection stops and the connection is considered lost.

## Hot Reload

Enable fast refresh during development:

```csharp
builder.ConfigureOptions(options =>
{
    options.EnableHotReload = true;   // Default in development
    options.EnableHotReload = false;  // Disable
});
```

When enabled:
- Component changes are reflected without full page reload
- CSS changes apply immediately
- State is preserved during reload when possible

Hot reload is most effective with `ServerPrerendered` render mode.

## Custom Pipeline & Endpoints

### Configure ASP.NET Pipeline

Add custom middleware or configure request processing:

```csharp
builder.ConfigurePipeline(app =>
{
    // Add custom middleware
    app.UseMiddleware<CustomAuthMiddleware>();

    // Configure exception handling
    app.UseExceptionHandler("/error");

    // Add security headers
    app.Use(async (context, next) =>
    {
        context.Response.Headers.Add("X-Custom-Header", "value");
        await next();
    });
});
```

This is called after default Blazor middleware is configured, allowing you to:
- Wrap or extend default behavior
- Add authentication/authorization
- Implement custom error handling
- Add security headers

### Configure Custom Endpoints

Add custom HTTP endpoints:

```csharp
builder.ConfigureEndpoints(app =>
{
    // Health check endpoint
    app.MapGet("/api/health", () => new { status = "OK" });

    // Custom REST API
    app.MapPost("/api/data", async (HttpContext context) =>
    {
        var request = await context.Request.ReadAsAsync<DataRequest>();
        return new { result = ProcessData(request) };
    });

    // File serving
    app.MapGet("/api/download/{filename}", (string filename) =>
    {
        var path = Path.Combine("Downloads", filename);
        return Results.File(path);
    });

    // WebSocket endpoint
    app.MapGet("/ws/notifications", async (HttpContext context) =>
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            // Handle WebSocket connection
        }
    });
});
```

Endpoints are mapped after Blazor components but before the default 404 handler.

## HTTP Clients

### Named HTTP Clients

Configure HTTP clients for specific services:

```csharp
builder
    .AddHttpClient("GitHub", client =>
    {
        client.BaseAddress = new Uri("https://api.github.com");
        client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
    })
    .AddHttpClient("Database", client =>
    {
        client.BaseAddress = new Uri("https://db.example.com");
        client.Timeout = TimeSpan.FromSeconds(30);
    });
```

Use in components:

```csharp
@inject IHttpClientFactory HttpClientFactory

@code {
    protected override async Task OnInitializedAsync()
    {
        var client = HttpClientFactory.CreateClient("GitHub");
        var response = await client.GetAsync("/repos");
        // ...
    }
}
```

### Typed HTTP Clients

Create dedicated client classes:

```csharp
public class WeatherApiClient
{
    private readonly HttpClient _httpClient;

    public WeatherApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<WeatherData> GetWeatherAsync(string location)
    {
        return await _httpClient.GetFromJsonAsync<WeatherData>($"/weather/{location}");
    }
}

// Register in builder
builder.AddHttpClient<WeatherApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.weather.example.com");
})
```

Use in components:

```csharp
@inject WeatherApiClient WeatherClient

@code {
    protected override async Task OnInitializedAsync()
    {
        var weather = await WeatherClient.GetWeatherAsync("Seattle");
    }
}
```

### Default HTTP Client

```csharp
builder.AddHttpClient();
```

Then inject `IHttpClientFactory`:

```csharp
@inject IHttpClientFactory HttpClientFactory

@code {
    var client = HttpClientFactory.CreateClient();
    var response = await client.GetAsync("https://example.com");
}
```

## Web View Configuration

### Developer Tools

Enable browser developer tools for debugging JavaScript, inspecting the DOM, monitoring network requests, and viewing console output:

```csharp
// Fluent API
builder.EnableDevTools()          // Enable
builder.EnableDevTools(true)      // Enable
builder.EnableDevTools(false)     // Disable (default)

// Or via ConfigureOptions
builder.ConfigureOptions(options =>
{
    options.EnableDevTools = true;
});
```

**How to access DevTools:**
- Press **F12** to open/close DevTools
- Right-click in the application → Select "Inspect" (requires `EnableContextMenu = true`)

**What you can do with DevTools:**
- **Console tab**: View JavaScript errors, warnings, and `console.log` output
- **Network tab**: Monitor SignalR WebSocket connections and HTTP requests
- **Elements tab**: Inspect and modify the DOM in real-time
- **Sources tab**: Debug JavaScript with breakpoints

**Default:** Disabled (`false`)

### Context Menu

Enable/disable the browser's right-click context menu:

```csharp
// Fluent API
builder.EnableContextMenu()        // Enable
builder.EnableContextMenu(true)    // Enable (default)
builder.EnableContextMenu(false)   // Disable

// Or via ConfigureOptions
builder.ConfigureOptions(options =>
{
    options.EnableContextMenu = true;   // Default
    options.EnableContextMenu = false;  // Disable
});
```

**When enabled:**
- Right-click shows browser context menu (copy, paste, inspect, etc.)
- Required for "Inspect" option to access DevTools via right-click

**When disabled:**
- Right-click does nothing (cleaner native app feel)
- DevTools still accessible via F12 if `EnableDevTools = true`

**Default:** Enabled (`true`)

### Browser Permissions

Grant browser permissions automatically:

```csharp
builder.ConfigureOptions(options =>
{
    options.GrantBrowserPermissions = true;   // Grant all (default)
    options.GrantBrowserPermissions = false;  // Deny/prompt
});
```

Affects permissions like:
- Geolocation
- Camera/Microphone
- Notifications
- Clipboard access

### User Agent

Customize the user agent string:

```csharp
builder.ConfigureOptions(options =>
{
    options.UserAgent = "MyApp/1.0 (CheapAvaloniaBlazor)";
});
```

Default: Standard Chromium user agent

### GPU Acceleration

Enable GPU acceleration for rendering:

```csharp
builder.ConfigureOptions(options =>
{
    options.EnableGpuAcceleration = true;   // Default
    options.EnableGpuAcceleration = false;  // Disable
});
```

Disable for:
- Remote desktop/headless environments
- Virtual machines with limited GPU
- Troubleshooting rendering issues

### Browser Arguments

Pass custom arguments to the Chromium engine:

```csharp
builder.ConfigureOptions(options =>
{
    options.BrowserArgs = new[]
    {
        "--disable-background-timer-throttling",
        "--disable-renderer-backgrounding",
        "--disable-backgrounding-occluded-windows",
        "--disable-breakpad",
        "--disable-default-apps",
        "--disable-hang-monitor",
        "--disable-popup-blocking",
        "--disable-prompt-on-repost",
        "--disable-sync"
    };
});
```

Common arguments:
- `--disable-gpu`: Disable GPU acceleration
- `--single-process`: Run in single process (for testing)
- `--no-first-run`: Skip first-run initialization
- `--no-default-browser-check`: Skip browser check

## Injection & Advanced Features

### Custom CSS

Inject CSS into the web view:

```csharp
builder.ConfigureOptions(options =>
{
    options.CustomCss = @"
        body {
            font-family: 'Segoe UI', sans-serif;
            background-color: #f5f5f5;
        }
        .dark-mode {
            background-color: #1e1e1e;
            color: #fff;
        }
    ";
});
```

Custom CSS is injected into the document head and applies globally.

### Custom JavaScript

Inject JavaScript into the web view:

```csharp
builder.ConfigureOptions(options =>
{
    options.CustomJavaScript = @"
        window.myApp = {
            version: '1.0.0',
            initialize: function() {
                console.log('App initialized');
            }
        };
    ";
});
```

JavaScript is executed when the page loads, before Blazor initializes.

### Custom Static File Options

Configure advanced static file serving:

```csharp
builder.ConfigureOptions(options =>
{
    options.CustomStaticFileOptions = new StaticFileOptions
    {
        ContentTypeProvider = new FileExtensionContentTypeProvider
        {
            Mappings =
            {
                [".wasm"] = "application/wasm",
                [".webmanifest"] = "application/manifest+json",
                [".custom"] = "application/custom"
            }
        },
        DefaultContentType = "application/octet-stream",
        OnPrepareResponse = ctx =>
        {
            ctx.Context.Response.Headers.Add("Cache-Control", "public,max-age=3600");
        }
    };
});
```

## Splash Screen

Configure the splash screen shown during startup:

### Enable/Disable

```csharp
builder.WithSplashScreen()           // Enable with defaults
builder.WithSplashScreen(false)      // Disable
builder.WithSplashScreen(true)       // Enable with defaults
```

### Title and Message

```csharp
builder.WithSplashScreen("My Application", "Initializing modules...")
```

### Advanced Configuration

```csharp
builder.ConfigureSplashScreen(splash =>
{
    splash.Title = "Advanced App";
    splash.LoadingMessage = "Loading...";
    splash.Width = 500;
    splash.Height = 300;
    splash.BackgroundColor = "#2D2D30";
    splash.ForegroundColor = "#00D9FF";
    splash.TitleFontSize = 28;
    splash.MessageFontSize = 16;
    splash.ShowLoadingIndicator = true;
});
```

### Custom Splash Screen Content

Replace the default splash with custom content:

```csharp
builder.WithCustomSplashScreen(() =>
{
    return new Border
    {
        Background = new SolidColorBrush(Colors.Navy),
        Child = new StackPanel
        {
            Children =
            {
                new Image { Source = new Bitmap("logo.png") },
                new TextBlock { Text = "Loading..." }
            }
        }
    };
});
```

## Logging & Diagnostics

### Console Logging

Enable console window for logging output:

```csharp
// Fluent API
builder.EnableConsoleLogging()        // Enable
builder.EnableConsoleLogging(true)    // Enable
builder.EnableConsoleLogging(false)   // Disable (default)

// Or via ConfigureOptions
builder.ConfigureOptions(options =>
{
    options.EnableConsoleLogging = true;
});
```

**Behavior:**
- **When enabled (`true`):**
  - Console window is shown for logging output
  - If launched from Windows Explorer (no parent console), a new console window is automatically allocated
  - Photino WebView logging is set to verbose (level 2)
  - Useful for debugging startup issues and monitoring runtime behavior

- **When disabled (`false`):**
  - Console window is hidden for a native desktop app feel
  - Standard output/error are redirected to null
  - Photino WebView logging is set to critical only (level 0)
  - This is the default for production applications

**Platform notes:**
- On Windows, uses `AllocConsole()` to create a console if none exists
- On Linux/macOS, console behavior follows standard terminal behavior

**Default:** Disabled (`false`)

### Comprehensive Diagnostics

Enable detailed diagnostics for troubleshooting:

```csharp
builder.EnableDiagnostics()   // Enables console logging + diagnostics
builder.WithDiagnostics()     // Alias for EnableDiagnostics()
```

Diagnostics output includes:
- Service registration summary
- Component initialization
- Pipeline configuration
- Runtime initialization details

## Startup Configuration

### Port Scanning

CheapAvaloniaBlazor automatically finds an available port if the configured port is in use:

```csharp
builder.UsePort(5000);  // Will scan 5000-5099 if needed
```

### Startup Retries

Configure retry attempts when starting the server:

```csharp
builder.ConfigureOptions(options =>
{
    options.MaxStartupRetries = 3;  // Retry up to 3 times (default)
});
```

### Startup Timeout

Configure the timeout for server startup:

```csharp
builder.ConfigureOptions(options =>
{
    options.StartupTimeout = TimeSpan.FromSeconds(30);  // Default
    options.StartupTimeout = TimeSpan.FromSeconds(60);  // Longer timeout
});
```

## Service Registration

### Direct Service Registration

Add services to dependency injection:

```csharp
builder.Services.AddSingleton<IDataService, DataService>();
builder.Services.AddScoped<IRepository, Repository>();
builder.Services.AddTransient<IValidator, Validator>();
```

### Post-Build Configuration

Configure services after the service provider is built:

```csharp
builder.ConfigureServices(serviceProvider =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Services configured: {Count}",
        serviceProvider.GetServices<object>().Count());
});
```

## Complete Configuration Examples

### Example 1: Standard Business Application

```csharp
var builder = new HostBuilder()
    .WithTitle("Business Application")
    .WithSize(1400, 900)
    .CenterWindow(true)
    .Resizable(true)
    .WithIcon("Assets/icon.ico")
    .UsePort(5000)
    .UseHttps(false)
    .EnableConsoleLogging(true)
    .AddMudBlazor(config =>
    {
        config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
    })
    .AddHttpClient("CompanyApi", client =>
    {
        client.BaseAddress = new Uri("https://api.company.com");
        client.DefaultRequestHeaders.Add("Authorization", "Bearer token");
    })
    .ConfigureOptions(options =>
    {
        options.EnableDevTools = false;
        options.EnableContextMenu = false;
        options.GrantBrowserPermissions = true;
        options.MaximumReceiveMessageSize = 64 * 1024;
    })
    .WithSplashScreen("Business App", "Initializing...")
    .WithRenderMode("ServerPrerendered");

// Add custom services
builder.Services.AddSingleton<IDataService, DataService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IReportService, ReportService>();

var window = builder.Build();
window.Show();
```

### Example 2: Developer Tool

```csharp
var builder = new HostBuilder()
    .WithTitle("Debug Inspector")
    .WithSize(1600, 1000)
    .WithPosition(50, 50)
    .Resizable(true)
    .Chromeless(false)
    .UsePort(5001)
    .EnableConsoleLogging(true)
    .EnableDiagnostics()
    .EnableDevTools(true)
    .AddMudBlazor()
    .ConfigureOptions(options =>
    {
        options.EnableContextMenu = true;
        options.EnableHotReload = true;
        options.CustomCss = @"
            body {
                font-family: 'Cascadia Code', monospace;
                background-color: #1e1e1e;
                color: #d4d4d4;
            }
        ";
    })
    .ConfigureEndpoints(app =>
    {
        app.MapGet("/api/debug-info", () => new { dotnetVersion = Environment.Version });
        app.MapGet("/api/logs", async (ILogger<Program> logger) =>
            new { status = "logging enabled" });
    })
    .WithSplashScreen("Inspector", "Loading debug tools...", false);

var window = builder.Build();
window.Show();
```

### Example 3: Media Application with Custom JS

```csharp
var builder = new HostBuilder()
    .WithTitle("Media Player")
    .WithSize(1024, 768)
    .CenterWindow(true)
    .UsePort(5002)
    .AddMudBlazor()
    .ConfigureOptions(options =>
    {
        options.EnableGpuAcceleration = true;
        options.EnableDevTools = false;
        options.CustomCss = @"
            .video-player {
                background: #000;
                width: 100%;
                height: 100%;
            }
        ";
        options.CustomJavaScript = @"
            window.mediaApp = {
                play: function(src) {
                    var video = document.querySelector('video');
                    if (video) {
                        video.src = src;
                        video.play();
                    }
                }
            };
        ";
        options.BrowserArgs = new[]
        {
            "--disable-background-timer-throttling",
            "--disable-renderer-backgrounding"
        };
    })
    .ConfigurePipeline(app =>
    {
        app.Use(async (context, next) =>
        {
            context.Response.Headers.Add("Cross-Origin-Opener-Policy", "same-origin");
            await next();
        });
    })
    .ConfigureEndpoints(app =>
    {
        app.MapGet("/api/media/list", () => GetMediaList());
        app.MapPost("/api/media/play", (MediaRequest req) => PlayMedia(req));
    });

var window = builder.Build();
window.Show();
```

### Example 4: Full-Featured Desktop App

```csharp
var builder = new HostBuilder()
    .WithTitle("Full-Featured Desktop App")
    .WithSize(1920, 1080)
    .CenterWindow(true)
    .Resizable(true)
    .WithIcon("Assets/app-icon.ico")
    .UsePort(5003)
    .UseHttps(false)
    .UseContentRoot("./app-content")
    .UseWebRoot("./wwwroot")
    .EnableConsoleLogging(true)
    .WithRenderMode("ServerPrerendered")
    .AddMudBlazor(config =>
    {
        config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopCenter;
    })
    .AddHttpClient<ApiClient>(client =>
    {
        client.BaseAddress = new Uri("http://localhost:5000");
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddHttpClient("ExternalApi", client =>
    {
        client.BaseAddress = new Uri("https://api.external.com");
    })
    .ConfigureOptions(options =>
    {
        options.EnableDevTools = true;
        options.EnableContextMenu = true;
        options.GrantBrowserPermissions = true;
        options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB
        options.EnableAutoReconnect = true;
        options.ReconnectIntervals = new[] { 0, 2, 5, 10, 30 };
        options.EnableHotReload = true;
        options.DefaultZoom = 100;
        options.CustomCss = @"
            :root {
                --primary-color: #1976d2;
                --secondary-color: #dc004e;
            }
        ";
        options.CustomJavaScript = @"
            console.log('App initialized');
            window.app = {
                version: '1.0.0'
            };
        ";
    })
    .ConfigurePipeline(app =>
    {
        app.UseRouting();
        app.UseAuthorization();
    })
    .ConfigureEndpoints(app =>
    {
        app.MapGet("/api/status", () => new { online = true, timestamp = DateTime.UtcNow });
        app.MapPost("/api/config", (ConfigRequest req) => ProcessConfig(req));
    })
    .ConfigureSplashScreen(splash =>
    {
        splash.Title = "Full-Featured App";
        splash.LoadingMessage = "Initializing...";
        splash.BackgroundColor = "#2D2D30";
    });

// Register services
builder.Services
    .AddSingleton<IDataRepository, DataRepository>()
    .AddSingleton<ICache, MemoryCache>()
    .AddScoped<IAuthService, AuthService>()
    .AddScoped<ISettingsService, SettingsService>();

// Post-build configuration
builder.ConfigureServices(serviceProvider =>
{
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Application services initialized");
});

var window = builder.Build();
window.Show();
```

## Configuration Options Reference

Complete table of all `CheapAvaloniaBlazorOptions` properties:

### Server & Network Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Port` | int | 5000 | Port for internal Blazor server |
| `UseHttps` | bool | false | Enable HTTPS for server |
| `ContentRoot` | string | null | Root path for application files |
| `WebRoot` | string | null | Root path for static files (wwwroot) |
| `MaximumReceiveMessageSize` | long | null | Max SignalR message size in bytes |

### Window Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `DefaultWindowTitle` | string | "Blazor Desktop App" | Window title bar text |
| `DefaultWindowWidth` | int | 1200 | Initial window width in pixels |
| `DefaultWindowHeight` | int | 800 | Initial window height in pixels |
| `WindowLeft` | int? | null | Window left position (X coordinate) |
| `WindowTop` | int? | null | Window top position (Y coordinate) |
| `CenterWindow` | bool | true | Center window on screen at startup |
| `Resizable` | bool | true | Allow window resizing |
| `Chromeless` | bool | false | Hide window chrome/decorations |
| `IconPath` | string | null | Path to window icon file |
| `DefaultZoom` | int | 100 | Initial zoom level (percentage) |

### Web View Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `EnableDevTools` | bool | false | Enable browser developer tools |
| `EnableContextMenu` | bool | true | Enable right-click context menu |
| `GrantBrowserPermissions` | bool | true | Auto-grant browser permissions |
| `UserAgent` | string | null | Custom user agent string |
| `BrowserArgs` | string[] | null | Arguments for Chromium engine |
| `EnableGpuAcceleration` | bool | true | Enable GPU acceleration |

### Render & Component Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `RecommendedRenderMode` | string | "ServerPrerendered" | Recommended render mode (informational) |
| `RootComponentType` | Type | null | Custom root component type |
| `AdditionalAssemblies` | List<Assembly> | [] | Additional assemblies for component discovery |

### SignalR Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `EnableAutoReconnect` | bool | true | Auto-reconnect when connection lost |
| `ReconnectIntervals` | int[] | {0,2,10,30} | Retry intervals in seconds |

### Diagnostics & Logging

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `EnableConsoleLogging` | bool | false | Enable console output |
| `EnableDiagnostics` | bool | false | Enable comprehensive diagnostics |

### Development & Debug

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `EnableHotReload` | bool | true | Enable hot reload in development |

### Startup Configuration

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `MaxStartupRetries` | int | 3 | Retry attempts for server startup |
| `StartupTimeout` | TimeSpan | 30s | Timeout for server startup |

### CSS & JavaScript Injection

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `CustomCss` | string | null | Custom CSS to inject |
| `CustomJavaScript` | string | null | Custom JavaScript to inject |

### Configuration Delegates

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ConfigureServices` | Action<IServiceCollection> | null | Configure DI services |
| `ConfigurePipeline` | Action<WebApplication> | null | Configure ASP.NET pipeline |
| `ConfigureEndpoints` | Action<WebApplication> | null | Add custom endpoints |
| `CustomStaticFileOptions` | StaticFileOptions | null | Custom static file serving |

### Splash Screen

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `SplashScreen` | SplashScreenConfig | default | Splash screen configuration |

See `SplashScreenConfig` class for splash screen specific options.

## Advanced Patterns

### Conditional Configuration

```csharp
var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

var builder = new HostBuilder()
    .WithTitle("App");

if (isDevelopment)
{
    builder.EnableDiagnostics()
           .EnableDevTools()
           .EnableConsoleLogging();
}
else
{
    builder.EnableDevTools(false)
           .EnableConsoleLogging(false);
}
```

### Configuration from File

```csharp
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", true)
    .Build();

var builder = new HostBuilder()
    .UsePort(config.GetValue<int>("Server:Port", 5000))
    .WithTitle(config["App:Title"])
    .WithSize(
        config.GetValue<int>("Window:Width", 1200),
        config.GetValue<int>("Window:Height", 800)
    );
```

### Fluent Service Registration

```csharp
builder.Services
    .AddLogging()
    .AddSingleton<IDataService, DataService>()
    .AddScoped<IRepository, Repository>()
    .AddHttpClient<ApiClient>()
    .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(30));
```

### Dynamic Configuration

```csharp
builder.ConfigureOptions(options =>
{
    // Calculate port dynamically
    options.Port = FindAvailablePort(5000, 5100);

    // Get paths from environment
    options.ContentRoot = Environment.GetEnvironmentVariable("APP_CONTENT") ?? "./content";
    options.WebRoot = Environment.GetEnvironmentVariable("APP_WWWROOT") ?? "./wwwroot";

    // Configure based on runtime
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
        options.BrowserArgs = new[] { "--no-sandbox" };
    }
});
```

## Troubleshooting

### Port Already in Use

If the configured port is already in use:
1. CheapAvaloniaBlazor automatically scans for available ports within a range
2. Check the logs to see which port was actually used
3. Use `EnableConsoleLogging()` to see port information
4. Manually specify a different port with `UsePort()`

### High Message Sizes Cause Disconnection

If components disconnect with large data transfers:
```csharp
builder.ConfigureOptions(options =>
{
    options.MaximumReceiveMessageSize = 5 * 1024 * 1024; // 5MB
});
```

### Hot Reload Not Working

Ensure:
1. `EnableHotReload` is true (default in development)
2. Using `ServerPrerendered` render mode for best results
3. Components are in the correct location for discovery

### DevTools Not Opening

Check:
1. `EnableDevTools()` is called (or `options.EnableDevTools = true`)
2. Press F12 to open DevTools manually
3. WebView2 runtime is properly installed

## Best Practices

1. **Use Fluent API**: Chain methods for readability
2. **Extract Configuration**: Use config files for production settings
3. **Log Startup**: Enable diagnostics during development
4. **Test Message Sizes**: Verify `MaximumReceiveMessageSize` for your data
5. **Custom Middleware**: Place in `ConfigurePipeline` for proper ordering
6. **Service Lifecycle**: Use appropriate scopes (Singleton, Scoped, Transient)
7. **Error Handling**: Implement in `ConfigurePipeline` for global error handling
8. **Security**: Never log sensitive data, validate all inputs

## See Also

- [README - Getting Started](../README.md)
- [API Documentation](api.md)
- [Component Development Guide](components.md)
- [Deployment Guide](deployment.md)
