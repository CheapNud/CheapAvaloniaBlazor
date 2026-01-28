using CheapAvaloniaBlazor.Configuration;
using CheapAvaloniaBlazor.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Reflection;

namespace CheapAvaloniaBlazor.Services;

public class EmbeddedBlazorHostService : IBlazorHostService, IDisposable
{
    private WebApplication? _app;
    private readonly CheapAvaloniaBlazorOptions _options;
    private readonly ILogger<EmbeddedBlazorHostService> _logger;
    private readonly DiagnosticLogger _diagnosticLogger;
    private CancellationTokenSource? _hostCts;
    private Type? _appType;

    public bool IsRunning { get; private set; }
    public string BaseUrl => $"{(_options.UseHttps ? Constants.Defaults.HttpsScheme : Constants.Defaults.HttpScheme)}://{Constants.Defaults.LocalhostAddress}:{_options.Port}";

    public EmbeddedBlazorHostService(
        CheapAvaloniaBlazorOptions options,
        ILogger<EmbeddedBlazorHostService> logger)
    {
        _options = options;
        _logger = logger;
        _diagnosticLogger = new DiagnosticLogger(logger, options);
    }

    public async Task<string> StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            _logger.LogWarning("Blazor host is already running");
            return BaseUrl;
        }

        try
        {
            _hostCts = new CancellationTokenSource();

            // Find an available port if the configured port is in use
            var availablePort = FindAvailablePort(_options.Port);
            if (availablePort != _options.Port)
            {
                _logger.LogInformation("Port {ConfiguredPort} is in use, using port {AvailablePort} instead", _options.Port, availablePort);
                _options.Port = availablePort;
            }

            // ===================================================================================
            // WHY IS ENVIRONMENT HARDCODED TO DEVELOPMENT?
            // ===================================================================================
            //
            // TL;DR: CheapAvaloniaBlazor is ALWAYS a desktop app, never a web server.
            //        Development mode is required for UseStaticWebAssets() to serve framework JS files.
            //        Production mode concerns (security, error exposure) don't apply to localhost desktop apps.
            //
            // TECHNICAL EXPLANATION:
            // ----------------------
            // Blazor Web App requires these JavaScript files to function:
            //   - /_framework/blazor.web.js (Blazor Web App runtime)
            //   - /_content/MudBlazor/MudBlazor.min.js (UI components)
            //   - /_content/CheapAvaloniaBlazor/cheap-blazor-interop.js (desktop interop)
            //
            // These files live INSIDE NuGet packages, not in your wwwroot folder.
            // ASP.NET Core's UseStaticWebAssets() serves them - but ONLY in Development mode.
            // In Production mode, UseStaticWebAssets() is a no-op and expects files to exist
            // physically in wwwroot (which only happens after 'dotnet publish').
            //
            // For desktop apps:
            //   - We're running on localhost, not exposed to the internet
            //   - Production's security benefits (error hiding, HSTS) are irrelevant
            //   - Users never see ASP.NET error pages (they're in a WebView)
            //   - We need the NuGet static assets to work without publishing
            //
            // HISTORY OF OVER-ENGINEERING (v1.2.2 - v1.2.3):
            // ----------------------------------------------
            // We tried to be "proper" and let users configure Development vs Production:
            //   1. Added EnvironmentName property to options
            //   2. Added UseEnvironment(), UseDevelopmentEnvironment(), UseProductionEnvironment() methods
            //   3. Tried compile-time defaults with #if DEBUG in the library (failed: NuGet packages
            //      are compiled once in Release mode, so DEBUG is always false)
            //   4. Tried requiring consumers to add #if DEBUG in their own Program.cs
            //   5. Added validation exceptions with helpful error messages
            //
            // All of this was solving a problem that doesn't exist for desktop apps.
            // Production mode is for web servers exposed to the internet. We're localhost.
            // Just use Development. It works. Ship it.
            // ===================================================================================

            // Use CreateBuilder() with NO WebApplicationOptions - this is how v1.1.5 worked.
            // Passing WebApplicationOptions (even with correct values) breaks the internal
            // Blazor script serving mechanism that MapRazorComponents() relies on.
            var builder = WebApplication.CreateBuilder();

            // Set environment to Development for UseStaticWebAssets() to work
            builder.Environment.EnvironmentName = Environments.Development;

            // Set content root for static assets discovery
            var effectiveContentRoot = !string.IsNullOrEmpty(_options.ContentRoot)
                ? _options.ContentRoot
                : AppContext.BaseDirectory;

            builder.Environment.ContentRootPath = effectiveContentRoot;
            builder.WebHost.UseContentRoot(effectiveContentRoot);

            _logger.LogInformation("Created WebApplication builder with environment: {Environment}, ContentRoot: {ContentRoot}",
                builder.Environment.EnvironmentName, effectiveContentRoot);

            // Extract JavaScript bridge from embedded resources to physical wwwroot
            // This ensures the JS file is always available for serving (workaround for NuGet static assets issue)
            try
            {
                // Path.Combine is safe here: effectiveContentRoot comes from trusted app configuration
                // (_options.ContentRoot) or the runtime (AppContext.BaseDirectory), not from user/network input.
                // This is a localhost-only desktop app, not a web server processing external URLs.
                var wwwrootPath = Path.Combine(effectiveContentRoot, Constants.Paths.WwwRoot);

                _diagnosticLogger.LogDiagnostic("Extracting JavaScript bridge to: {WwwrootPath}", wwwrootPath);

                var extractedPath = JavaScriptBridgeExtractor.ExtractJavaScriptBridge(wwwrootPath, _diagnosticLogger);

                _diagnosticLogger.LogDiagnostic("JavaScript bridge extraction completed: {ExtractedPath}", extractedPath);
            }
            catch (Exception ex)
            {
                _diagnosticLogger.LogError(ex, "Failed to extract JavaScript bridge - application may not function correctly");
                // Don't throw - let the app start anyway, manual serving might work
            }

            // Configure services
            ConfigureServices(builder.Services);

            // Configure web host
            builder.WebHost.UseUrls(BaseUrl);
            builder.WebHost.UseStaticWebAssets();

            // Suppress console output in production
            if (!_options.EnableConsoleLogging)
            {
                builder.Logging.ClearProviders();
            }

            _app = builder.Build();

            // Configure pipeline
            ConfigurePipeline(_app);

            // Start the host
            _logger.LogInformation("Starting Blazor host task...");
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Running WebApplication.RunAsync...");
                    await _app.RunAsync(_hostCts.Token);
                    _logger.LogInformation("WebApplication.RunAsync completed");
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Blazor host cancelled (expected)");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Blazor host failed during RunAsync");
                }
            }, _hostCts.Token);

            // Wait for startup
            await WaitForStartupAsync(cancellationToken);

            IsRunning = true;
            _logger.LogInformation("Blazor host started at {BaseUrl}", BaseUrl);

            return BaseUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Blazor host");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning || _app == null)
        {
            return;
        }

        try
        {
            _hostCts?.Cancel();
            await _app.StopAsync(cancellationToken);
            await _app.DisposeAsync();

            IsRunning = false;
            _logger.LogInformation("Blazor host stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping Blazor host");
            throw;
        }
    }

    private void ConfigureServices(IServiceCollection services)
    {
        try
        {
            _logger.LogInformation("Configuring services for embedded Blazor host...");

            // Modern Blazor Web App pattern: AddRazorComponents + AddInteractiveServerComponents
            // Replaces legacy AddRazorPages + AddServerSideBlazor pattern
            _logger.LogDebug("Adding RazorComponents with InteractiveServerComponents...");
            services.AddRazorComponents()
                .AddInteractiveServerComponents(circuitOptions =>
                {
                    // Increase timeouts for embedded scenarios where the WebView might be slower
                    circuitOptions.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(10);
                    circuitOptions.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(2);
                    circuitOptions.DetailedErrors = true;
                });

            // Add circuit error handler to capture detailed exceptions
            services.AddSingleton<Microsoft.AspNetCore.Components.Server.Circuits.CircuitHandler>(sp =>
            {
                var circuitLogger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("CircuitHandler");
                return new DiagnosticCircuitHandler(circuitLogger);
            });

            // Add comprehensive diagnostics
            _diagnosticLogger.LogDiagnosticVerbose(Constants.Diagnostics.RazorComponentsAdded);

            // Find and store the App component type for use in ConfigurePipeline (MapRazorComponents<App>)
            _appType = Utilities.BlazorComponentMapper.DiscoverAppType();

            if (_appType != null)
            {
                _logger.LogInformation("Found App component: {AppType}", _appType.FullName);
            }
            else
            {
                _logger.LogWarning("Could not find App component in entry assembly. MapRazorComponents will fail.");
            }

            // Navigation services are automatically registered by AddRazorComponents()
            _logger.LogDebug("NavigationManager services automatically registered by AddRazorComponents");

            // DIAGNOSTICS: Log the complete service registration
            _diagnosticLogger.LogDiagnosticVerbose(Constants.Diagnostics.CompleteServiceRegistration);
            _diagnosticLogger.LogDiagnosticVerbose(Constants.Diagnostics.RazorComponentsAdded);
            _diagnosticLogger.LogDiagnosticVerbose(Constants.Diagnostics.DesktopInteropAdded);
            _diagnosticLogger.LogDiagnosticVerbose(Constants.Diagnostics.NavigationManagerAutoRegistered);
            _diagnosticLogger.LogDiagnosticVerbose("- RecommendedRenderMode: {RenderMode}", _options.RecommendedRenderMode);

            // Log all NavigationManager-related services
            if (_diagnosticLogger.DiagnosticsEnabled)
            {
                var serviceDescriptors = services.Where(s =>
                    s.ServiceType.Name.Contains("Navigation") ||
                    s.ServiceType.Name.Contains("Router") ||
                    s.ServiceType.Name.Contains("Circuit")).ToList();

                _diagnosticLogger.LogDiagnosticVerbose("Found {Count} navigation/routing related services:", serviceDescriptors.Count);
                foreach (var descriptor in serviceDescriptors)
                {
                    _diagnosticLogger.LogDiagnosticVerbose("- {ServiceType} -> {ImplementationType} ({Lifetime})",
                        descriptor.ServiceType.Name,
                        descriptor.ImplementationType?.Name ?? "Factory",
                        descriptor.Lifetime);
                }
            }

            // Add user-configured services, but exclude services that might duplicate core services
            if (_options.ConfigureServices != null)
            {
                _logger.LogDebug("Invoking user-configured services...");
                _options.ConfigureServices.Invoke(services);
            }

            // Register services from the runtime to ensure same instances are used
            // IMPORTANT: This must be AFTER _options.ConfigureServices to override any type registrations
            // PhotinoMessageHandler is attached to the Photino window in BlazorHostWindow
            _logger.LogDebug("Registering PhotinoMessageHandler from runtime (overriding any previous registration)...");
            var messageHandler = CheapAvaloniaBlazorRuntime.GetRequiredService<PhotinoMessageHandler>();
            services.AddSingleton(messageHandler);

            // Register the diagnostic logger factory from runtime
            _logger.LogDebug("Registering IDiagnosticLoggerFactory from runtime...");
            var loggerFactory = CheapAvaloniaBlazorRuntime.GetRequiredService<IDiagnosticLoggerFactory>();
            services.AddSingleton(loggerFactory);

            // Add the DesktopInteropService
            _logger.LogDebug("Adding DesktopInteropService...");
            services.AddScoped<IDesktopInteropService, DesktopInteropService>();

            _logger.LogInformation("Service configuration completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring services");
            throw;
        }
    }

    private void ConfigurePipeline(WebApplication app)
    {
        try
        {
            _logger.LogInformation("Configuring pipeline for embedded Blazor host...");
            
            if (!app.Environment.IsDevelopment())
            {
                _logger.LogDebug("Adding production middleware (ExceptionHandler, HSTS)...");
                app.UseExceptionHandler(Constants.Endpoints.ErrorPage);
                if (_options.UseHttps)
                {
                    app.UseHsts();
                }
            }

            if (_options.UseHttps)
            {
                _logger.LogDebug("Adding HTTPS redirection...");
                app.UseHttpsRedirection();
            }

            // UseStaticFiles MUST be before UseRouting to serve _framework files
            // that are copied to wwwroot/_framework/ during build
            _logger.LogDebug("Adding static files middleware...");
            app.UseStaticFiles();

            _logger.LogDebug("Adding routing...");
            app.UseRouting();

            // Required by MapRazorComponents
            _logger.LogDebug("Adding antiforgery middleware...");
            app.UseAntiforgery();

            // Custom middleware
            if (_options.ConfigurePipeline != null)
            {
                _logger.LogDebug("Invoking user-configured pipeline...");
                _options.ConfigurePipeline.Invoke(app);
            }

            // Add diagnostic middleware to log all requests
            app.Use(async (context, next) =>
            {
                _logger.LogInformation($"{Constants.Diagnostics.Prefix} HTTP {{Method}} {{Path}} from {{RemoteIP}}",
                    context.Request.Method,
                    context.Request.Path,
                    context.Connection.RemoteIpAddress);

                // Log headers that might be relevant to Blazor
                if (context.Request.Headers.ContainsKey(Constants.Http.ConnectionHeader))
                {
                    _logger.LogInformation($"{Constants.Diagnostics.Prefix} Connection header: {{Connection}}",
                        context.Request.Headers[Constants.Http.ConnectionHeader]);
                }

                try
                {
                    await next();
                    _logger.LogInformation($"{Constants.Diagnostics.Prefix} Response {{StatusCode}} for {{Path}}",
                        context.Response.StatusCode, context.Request.Path);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"{Constants.Diagnostics.Prefix} Exception during request {{Path}}: {{Message}}",
                        context.Request.Path, ex.Message);
                    throw;
                }
            });

            // Modern Blazor Web App pattern: MapRazorComponents<App>().AddInteractiveServerRenderMode()
            // Centralized in BlazorComponentMapper to avoid reflection duplication
            if (_appType != null)
            {
                _logger.LogDebug("Setting up MapRazorComponents<{AppType}> via reflection...", _appType.FullName);

                var mapped = Utilities.BlazorComponentMapper.TryMapRazorComponents(
                    app, _appType, typeof(EmbeddedBlazorHostService).Assembly, _logger);

                if (!mapped)
                {
                    _logger.LogError("Failed to map Razor components via reflection. " +
                        "The ASP.NET Core framework API may have changed.");
                }
            }
            else
            {
                _logger.LogError("Cannot configure MapRazorComponents - App component type was not found in entry assembly. " +
                    "Ensure your project has a Components/App.razor file that serves as the HTML document root.");
            }

            // Map additional endpoints
            if (_options.ConfigureEndpoints != null)
            {
                _logger.LogDebug("Invoking user-configured endpoints...");
                _options.ConfigureEndpoints.Invoke(app);
            }
            
            _logger.LogInformation("Pipeline configuration completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring pipeline");
            throw;
        }
    }

    private async Task WaitForStartupAsync(CancellationToken cancellationToken)
    {
        var maxWaitTime = TimeSpan.FromSeconds(Constants.Defaults.StartupTimeoutSeconds);
        var checkInterval = TimeSpan.FromMilliseconds(Constants.Defaults.StartupCheckIntervalMilliseconds);
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("Waiting for Blazor host to become available at {BaseUrl}...", BaseUrl);

        using var httpClient = HttpClientFactory.CreateForServerCheck();

        int attemptCount = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                attemptCount++;
                _logger.LogDebug("Startup check attempt {AttemptCount}: {BaseUrl}", attemptCount, BaseUrl);
                
                var response = await httpClient.GetAsync(BaseUrl, cancellationToken);
                _logger.LogDebug("Startup check response: {StatusCode}", response.StatusCode);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Blazor host is available after {AttemptCount} attempts", attemptCount);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Startup check attempt {AttemptCount} failed: {Error}", attemptCount, ex.Message);
            }

            if (DateTime.UtcNow - startTime > maxWaitTime)
            {
                _logger.LogError("Blazor host failed to start within {MaxWaitTime} seconds after {AttemptCount} attempts", maxWaitTime.TotalSeconds, attemptCount);
                throw new TimeoutException("Blazor host failed to start within timeout period");
            }

            await Task.Delay(checkInterval, cancellationToken);
        }
        
        _logger.LogWarning("WaitForStartupAsync cancelled");
    }

    public void Dispose()
    {
        if (IsRunning)
        {
            StopAsync().GetAwaiter().GetResult();
        }

        _hostCts?.Dispose();
        _app?.DisposeAsync().GetAwaiter().GetResult();
    }

    private int FindAvailablePort(int startPort)
    {
        for (int port = startPort; port < startPort + Constants.Defaults.PortScanRange; port++)
        {
            try
            {
                using var listener = new TcpListener(IPAddress.Loopback, port);
                listener.Start();
                listener.Stop();
                return port;
            }
            catch (SocketException)
            {
                // Port is in use, try next one
                continue;
            }
        }
        
        // If no port found in range, return a random available port
        using var randomListener = new TcpListener(IPAddress.Loopback, 0);
        randomListener.Start();
        var availablePort = ((IPEndPoint)randomListener.LocalEndpoint).Port;
        randomListener.Stop();
        return availablePort;
    }
}

