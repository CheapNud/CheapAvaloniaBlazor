using CheapAvaloniaBlazor.Configuration;
using CheapAvaloniaBlazor.Hosting;
using CheapAvaloniaBlazor.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using System.Diagnostics;
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
    /// Configure static files including embedded resources
    /// </summary>
    // Add this temporarily to your WebApplicationExtensions.cs ConfigureStaticFiles method
    private static void ConfigureStaticFiles(WebApplication app, CheapAvaloniaBlazorOptions options)
    {
        var loggerFactory = app.Services.GetRequiredService<Services.IDiagnosticLoggerFactory>();
        var logger = loggerFactory.CreateLogger(nameof(WebApplicationExtensions));

        // Serve wwwroot files from consuming project.
        // Force revalidation for library-owned JS files so WebView2 picks up changes across builds.
        // Scoped to specific files — third-party JS (MudBlazor, etc.) uses normal browser caching.
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                if (Constants.Http.NoCacheJsFiles.Any(f =>
                    ctx.File.Name.Equals(f, StringComparison.OrdinalIgnoreCase)))
                {
                    ctx.Context.Response.Headers.CacheControl = "no-cache";
                }
            }
        });

        // Get the assembly containing embedded resources
        var assembly = typeof(WebApplicationExtensions).Assembly;

        // DEBUG: Show what's actually embedded (remove after testing)
        if (app.Environment.IsDevelopment())
        {
            DebugEmbeddedResources(assembly, logger);
        }

        // SOLUTION 1: Standard embedded file provider (most common case)
        try
        {
            var embeddedProvider = new EmbeddedFileProvider(assembly, Constants.Paths.EmbeddedResourceNamespace);

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = embeddedProvider,
                RequestPath = Constants.Endpoints.ContentPath,
                OnPrepareResponse = ctx =>
                {
                    if (Constants.Http.NoCacheJsFiles.Any(f =>
                        ctx.File.Name.Equals(f, StringComparison.OrdinalIgnoreCase)))
                    {
                        ctx.Context.Response.Headers.CacheControl = "no-cache";
                    }
                }
            });

            logger.LogVerbose("✅ Standard EmbeddedFileProvider configured successfully");
        }
        catch (Exception ex)
        {
            logger.LogVerbose($"❌ Standard EmbeddedFileProvider failed: {ex.Message}");

            // SOLUTION 2: Fallback - try without namespace prefix
            try
            {
                var fallbackProvider = new EmbeddedFileProvider(assembly);

                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PrefixedEmbeddedFileProvider(fallbackProvider, Constants.Paths.EmbeddedResourceNamespace),
                    RequestPath = Constants.Endpoints.ContentPath
                });

                logger.LogVerbose("✅ Fallback EmbeddedFileProvider configured successfully");
            }
            catch (Exception fallbackEx)
            {
                logger.LogVerbose($"❌ Fallback EmbeddedFileProvider also failed: {fallbackEx.Message}");

                // SOLUTION 3: Manual resource serving as last resort
                ConfigureManualResourceServing(app, assembly, logger);
            }
        }

        // Custom static file options if provided
        if (options.CustomStaticFileOptions != null)
        {
            app.UseStaticFiles(options.CustomStaticFileOptions);
        }
    }

    private static void DebugEmbeddedResources(Assembly assembly, Services.DiagnosticLogger logger)
    {
        logger.LogVerbose("🔍 === EMBEDDED RESOURCES DEBUG ===");
        logger.LogVerbose($"Assembly: {assembly.FullName}");

        var resources = assembly.GetManifestResourceNames();
        logger.LogVerbose($"Found {resources.Length} embedded resources:");

        foreach (var resource in resources.OrderBy(r => r))
        {
            logger.LogVerbose($"  📄 {resource}");
        }

        // Look for our specific JS file
        var jsFiles = resources.Where(r => r.Contains(Constants.Resources.JavaScriptBridgeResourcePattern)).ToArray();
        if (jsFiles.Any())
        {
            logger.LogVerbose("🎯 Found JS files:");
            foreach (var js in jsFiles)
            {
                logger.LogVerbose($"  ⚡ {js}");
            }
        }
        else
        {
            logger.LogVerbose($"❌ No {Constants.Resources.JavaScriptBridgeFileName} files found in embedded resources!");
        }

        logger.LogVerbose("🔍 === END DEBUG ===");
    }

    private static void ConfigureManualResourceServing(WebApplication app, Assembly assembly, Services.DiagnosticLogger logger)
    {
        logger.LogVerbose("🔧 Configuring manual resource serving...");

        // Manual endpoint for serving the JS file
        app.MapGet(Constants.Endpoints.JavaScriptBridgeEndpoint, async context =>
        {
            try
            {
                var resourceName = assembly.GetManifestResourceNames()
                    .FirstOrDefault(r => r.Contains(Constants.Resources.JavaScriptBridgeResourcePattern));

                if (resourceName == null)
                {
                    context.Response.StatusCode = Constants.Http.StatusCodeNotFound;
                    await context.Response.WriteAsync("JS file not found in embedded resources");
                    return;
                }

                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    context.Response.StatusCode = Constants.Http.StatusCodeNotFound;
                    await context.Response.WriteAsync("Could not load JS stream");
                    return;
                }

                context.Response.ContentType = Constants.Http.ContentTypeJavaScript;
                await stream.CopyToAsync(context.Response.Body);

                logger.LogVerbose($"✅ Manually served JS file: {resourceName}");
            }
            catch (Exception ex)
            {
                logger.LogVerbose($"❌ Manual serving failed: {ex.Message}");
                context.Response.StatusCode = Constants.Http.StatusCodeInternalServerError;
                await context.Response.WriteAsync($"Error serving JS file: {ex.Message}");
            }
        });
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
}

public class PrefixedEmbeddedFileProvider : IFileProvider
{
    private readonly IFileProvider _provider;
    private readonly string _prefix;

    public PrefixedEmbeddedFileProvider(IFileProvider provider, string prefix)
    {
        _provider = provider;
        _prefix = prefix.TrimEnd('/') + "/";
    }

    public IFileInfo GetFileInfo(string subpath)
    {
        var prefixedPath = _prefix + subpath.TrimStart('/');

        // Try to find the file with the prefixed path
        var resources = ((EmbeddedFileProvider)_provider).GetFileInfo(prefixedPath);
        if (resources.Exists)
            return resources;

        // Fallback: try to find any resource that ends with the filename
        if (_provider is EmbeddedFileProvider embeddedProvider)
        {
            var assembly = embeddedProvider.GetType()
                .GetField(Constants.Reflection.AssemblyFieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                .GetValue(embeddedProvider) as Assembly;

            if (assembly != null)
            {
                var fileName = Path.GetFileName(subpath);
                var matchingResource = assembly.GetManifestResourceNames()
                    .FirstOrDefault(r => r.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));

                if (matchingResource != null)
                {
                    var stream = assembly.GetManifestResourceStream(matchingResource);
                    if (stream != null)
                    {
                        return new EmbeddedResourceFileInfo(matchingResource, stream, DateTimeOffset.UtcNow);
                    }
                }
            }
        }

        return new NotFoundFileInfo(subpath);
    }

    public IDirectoryContents GetDirectoryContents(string subpath) => _provider.GetDirectoryContents(subpath);
    public IChangeToken Watch(string filter) => _provider.Watch(filter);
}

// Helper file info class
public class EmbeddedResourceFileInfo : IFileInfo
{
    private readonly Stream _stream;

    public EmbeddedResourceFileInfo(string name, Stream stream, DateTimeOffset lastModified)
    {
        Name = Path.GetFileName(name);
        _stream = stream;
        LastModified = lastModified;
        Length = stream.Length;
    }

    public bool Exists => true;
    public bool IsDirectory => false;
    public DateTimeOffset LastModified { get; }
    public long Length { get; }
    public string Name { get; }
    public string PhysicalPath => null;

    public Stream CreateReadStream() => _stream;
}
