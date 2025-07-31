using CheapAvaloniaBlazor.Configuration;
using CheapAvaloniaBlazor.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
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
            app.UseExceptionHandler("/Error");
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

        // Map Blazor SignalR hub
        app.MapBlazorHub(hubOptions =>
        {
            // Fix: HttpConnectionDispatcherOptions does not have MaximumReceiveMessageSize property.
            // Instead, use ApplicationMaxBufferSize or TransportMaxBufferSize if applicable.
            if (options.MaximumReceiveMessageSize.HasValue)
            {
                hubOptions.ApplicationMaxBufferSize = options.MaximumReceiveMessageSize.Value;
            }
        });

        // Map fallback to host page
        app.MapFallbackToPage("/_Host");

        return app;
    }

    /// <summary>
    /// Configure static files including embedded resources
    /// </summary>
    private static void ConfigureStaticFiles(WebApplication app, CheapAvaloniaBlazorOptions options)
    {
        // Serve wwwroot files
        app.UseStaticFiles();

        // Serve embedded resources from the library
        var assembly = typeof(WebApplicationExtensions).Assembly;
        var embeddedProvider = new EmbeddedFileProvider(assembly, "CheapAvaloniaBlazor.Resources.wwwroot");

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = embeddedProvider,
            RequestPath = "/_content/CheapAvaloniaBlazor"
        });

        // If custom static file options provided
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
                version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown",
                framework = "CheapAvaloniaBlazor"
            }));
        }
    }

    /// <summary>
    /// Run the application as a desktop app
    /// </summary>
    public static async Task RunAsDesktopAsync(
        this WebApplication app,
        CancellationToken cancellationToken = default)
    {
        // Start the web application in background
        var webAppTask = app.RunAsync(cancellationToken);

        // Create and run the desktop host
        var desktopHost = app.Services.GetRequiredService<BlazorDesktopHost>();
        await desktopHost.RunAsync(cancellationToken);

        // Stop the web app when desktop closes
        await app.StopAsync(cancellationToken);
    }
}

/// <summary>
/// Options for Blazor endpoint configuration
/// </summary>
public class BlazorEndpointOptions
{
    /// <summary>
    /// Enable health check endpoint
    /// </summary>
    public bool EnableHealthCheck { get; set; } = true;

    /// <summary>
    /// Health check endpoint path
    /// </summary>
    public string HealthCheckPath { get; set; } = "/health";

    /// <summary>
    /// Enable version endpoint
    /// </summary>
    public bool EnableVersionEndpoint { get; set; } = true;

    /// <summary>
    /// Version endpoint path
    /// </summary>
    public string VersionPath { get; set; } = "/version";

    /// <summary>
    /// Custom endpoints to map
    /// </summary>
    public List<CustomEndpoint> CustomEndpoints { get; } = new();

    /// <summary>
    /// Add a custom endpoint
    /// </summary>
    public void AddEndpoint(string pattern, RequestDelegate handler)
    {
        CustomEndpoints.Add(new CustomEndpoint { Pattern = pattern, Handler = handler });
    }
}

/// <summary>
/// Represents a custom endpoint
/// </summary>
public class CustomEndpoint
{
    public required string Pattern { get; init; }
    public required RequestDelegate Handler { get; init; }
}