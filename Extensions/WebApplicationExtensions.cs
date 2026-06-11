using CheapAvaloniaBlazor.Configuration;
using CheapAvaloniaBlazor.Hosting;
using CheapAvaloniaBlazor.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace CheapAvaloniaBlazor.Extensions;

/// <summary>
/// Extension methods for WebApplication to configure CheapAvaloniaBlazor
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Configure the web application for CheapAvaloniaBlazor
    /// </summary>
    public static WebApplication UseCheapBlazorDesktop(this WebApplication app)
    {
        var options = app.Services.GetService<CheapAvaloniaBlazorOptions>()
            ?? new CheapAvaloniaBlazorOptions();

        return app.UseCheapBlazorDesktop(options);
    }

    /// <summary>
    /// Configure the web application for CheapAvaloniaBlazor with options
    /// </summary>
    public static WebApplication UseCheapBlazorDesktop(
        this WebApplication app,
        CheapAvaloniaBlazorOptions options)
    {
        // Configure the HTTP request pipeline
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler(Constants.Endpoints.ErrorPage);
            if (options.UseHttps)
            {
                app.UseHsts();
            }
        }

        if (options.UseHttps)
        {
            app.UseHttpsRedirection();
        }

        // Configure static files with embedded resources
        ConfigureStaticFiles(app, options);

        app.UseRouting();

        // Required by MapRazorComponents
        app.UseAntiforgery();

        // Map static assets including _framework/blazor.web.js.
        // Required in .NET 9+ where framework JS is served via endpoint routing, not UseStaticFiles.
        app.MapStaticAssets();

        // Modern Blazor Web App pattern: MapRazorComponents<App>().AddInteractiveServerRenderMode()
        // Centralized in BlazorComponentMapper to avoid reflection duplication
        var appType = Utilities.BlazorComponentMapper.DiscoverAppType();

        if (appType != null)
        {
            Utilities.BlazorComponentMapper.TryMapRazorComponents(
                app, appType, typeof(WebApplicationExtensions).Assembly);
        }

        return app;
    }

    public static void MapCheapBlazorTestEndpoints(this WebApplication app)
    {
        // Test endpoint to verify embedded resources
        app.MapGet(Constants.Endpoints.TestEndpoint, async context =>
        {
            var assembly = typeof(WebApplicationExtensions).Assembly;
            var resources = assembly.GetManifestResourceNames();

            var html = $@"
<!DOCTYPE html>
<html>
<head><title>CheapAvaloniaBlazor Resource Test</title></head>
<body>
    <h1>Embedded Resources Test</h1>
    <h2>Found {resources.Length} resources:</h2>
    <ul>
        {string.Join("", resources.Select(r => $"<li>{r}</li>"))}
    </ul>
    
    <h2>JS File Test:</h2>
    <script src='{Constants.Endpoints.JavaScriptBridgeEndpoint}'></script>
    <script>
        setTimeout(() => {{
            if (typeof window.{Constants.JavaScript.CheapBlazorObject} !== 'undefined') {{
                document.body.innerHTML += '<p style=""color: green;"">✅ JS Bridge loaded successfully!</p>';
                document.body.innerHTML += '<p>Test result: ' + window.{Constants.JavaScript.CheapBlazorObject}.test() + '</p>';
            }} else {{
                document.body.innerHTML += '<p style=""color: red;"">❌ JS Bridge failed to load</p>';
            }}
        }}, 100);
    </script>
</body>
</html>";

            context.Response.ContentType = "text/html";
            await context.Response.WriteAsync(html);
        });
    }


    /// <summary>
    /// Configure static files for the consuming project's wwwroot
    /// </summary>
    // The JS bridge needs no special serving here: it ships as a static web asset and serves
    // at _content/CheapAvaloniaBlazor/cheap-blazor-interop.js through MapStaticAssets, exactly
    // like MudBlazor's _content files. The embedded-resource providers, manual MapGet fallback
    // and runtime extractor that used to live here existed only because the package's build
    // props clobbered the SDK-generated static web assets import (see Build/CheapAvaloniaBlazor.props).
    private static void ConfigureStaticFiles(WebApplication app, CheapAvaloniaBlazorOptions options)
    {
        var isDevelopment = app.Environment.IsDevelopment();

        // Serve wwwroot files from consuming project.
        // In Development, force revalidation for library-owned JS files so WebView2 picks up
        // changes across builds. ETags are sent by default — no-cache just ensures the browser
        // always checks (conditional GET → 304). Third-party JS caches normally in all environments.
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                if (isDevelopment && Constants.Http.NoCacheJsFiles.Any(f =>
                    ctx.File.Name.Equals(f, StringComparison.OrdinalIgnoreCase)))
                {
                    ctx.Context.Response.Headers.CacheControl = "no-cache";
                }
            }
        });

        // Custom static file options if provided
        if (options.CustomStaticFileOptions != null)
        {
            app.UseStaticFiles(options.CustomStaticFileOptions);
        }
    }

    /// <summary>
    /// Map Blazor endpoints with custom configuration
    /// </summary>
    public static void MapCheapBlazorEndpoints(
        this WebApplication app,
        Action<BlazorEndpointOptions>? configure = null)
    {
        var endpointOptions = new BlazorEndpointOptions();
        configure?.Invoke(endpointOptions);

        // Map custom endpoints if specified
        foreach (var endpoint in endpointOptions.CustomEndpoints)
        {
            app.Map(endpoint.Pattern, endpoint.Handler);
        }

        // Map health check if enabled
        if (endpointOptions.EnableHealthCheck)
        {
            app.MapGet(endpointOptions.HealthCheckPath, () => Results.Ok(new { status = "healthy" }));
        }

        // Map version endpoint if enabled
        if (endpointOptions.EnableVersionEndpoint)
        {
            app.MapGet(endpointOptions.VersionPath, () => Results.Ok(new
            {
                version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? Constants.Reflection.UnknownVersion,
                framework = Constants.Framework.Name
            }));
        }
    }

    // Note: RunAsDesktopAsync method removed - use Avalonia-based approach with BlazorHostWindow instead
    // Note: PrefixedEmbeddedFileProvider / EmbeddedResourceFileInfo removed - the JS bridge is a
    // static web asset now, no embedded-resource serving needed
}
