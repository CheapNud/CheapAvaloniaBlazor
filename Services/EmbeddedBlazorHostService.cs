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

namespace CheapAvaloniaBlazor.Services;

public class EmbeddedBlazorHostService : IBlazorHostService, IDisposable
{
    private WebApplication? _app;
    private readonly CheapAvaloniaBlazorOptions _options;
    private readonly ILogger<EmbeddedBlazorHostService> _logger;
    private readonly DiagnosticLogger _diagnosticLogger;
    private CancellationTokenSource? _hostCts;

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

            var builder = WebApplication.CreateBuilder();

            // Configure content root if specified
            if (!string.IsNullOrEmpty(_options.ContentRoot))
            {
                builder.Environment.ContentRootPath = _options.ContentRoot;
                builder.WebHost.UseContentRoot(_options.ContentRoot);
            }

            // Extract JavaScript bridge from embedded resources to physical wwwroot
            // This ensures the JS file is always available for serving (workaround for NuGet static assets issue)
            try
            {
                var contentRoot = !string.IsNullOrEmpty(_options.ContentRoot)
                    ? _options.ContentRoot
                    : Directory.GetCurrentDirectory();

                var wwwrootPath = Path.Combine(contentRoot, Constants.Paths.WwwRoot);

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
            
            // Add Blazor services directly here since this is a new service collection
            _logger.LogDebug("Adding RazorPages services...");
            services.AddRazorPages();
            
            // Configure RazorPages to look in Components directory instead of Pages
            services.Configure<Microsoft.AspNetCore.Mvc.RazorPages.RazorPagesOptions>(options =>
            {
                options.RootDirectory = Constants.SpecialFolders.RootDirectory;
            });
            
            _logger.LogDebug("Adding ServerSideBlazor services...");
            var blazorBuilder = services.AddServerSideBlazor(options =>
            {
                options.DetailedErrors = true;
                options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(Constants.Defaults.DisconnectedCircuitRetentionMinutes);
                options.DisconnectedCircuitMaxRetained = Constants.Defaults.DisconnectedCircuitMaxRetained;
            });
            
            // Add comprehensive diagnostics
            _diagnosticLogger.LogDiagnosticVerbose(Constants.Diagnostics.ServerSideBlazorAdded);

            // Log all registered services for NavigationManager debugging
            services.AddScoped(serviceProvider =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<EmbeddedBlazorHostService>>();
                logger.LogInformation(Constants.Diagnostics.ServiceProviderCreated);
                return serviceProvider;
            });
            
            // Configure hub options
            blazorBuilder.AddHubOptions(options =>
            {
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(Constants.Defaults.ClientTimeoutSeconds);
                options.HandshakeTimeout = TimeSpan.FromSeconds(Constants.Defaults.HandshakeTimeoutSeconds);
                options.MaximumReceiveMessageSize = Constants.Defaults.MaximumReceiveMessageSizeBytes;
            });
            
            // Find and log the App component
            var entryAssembly = System.Reflection.Assembly.GetEntryAssembly();
            var appType = entryAssembly?.GetType(Constants.ComponentNames.App) ?? entryAssembly?.GetTypes().FirstOrDefault(t => t.Name == Constants.ComponentNames.App);
            
            if (appType != null)
            {
                _logger.LogInformation("Found App component: {AppType}", appType.FullName);
            }
            else
            {
                _logger.LogWarning("Could not find App component in entry assembly");
            }

            // DesktopInteropService registration moved below after _options.ConfigureServices
            
            // Navigation services are automatically registered by AddServerSideBlazor()
            // RemoteNavigationManager is internal to the framework and not directly accessible
            _logger.LogDebug("NavigationManager services automatically registered by AddServerSideBlazor");
            
            // DIAGNOSTICS: Log the complete service registration
            _diagnosticLogger.LogDiagnosticVerbose(Constants.Diagnostics.CompleteServiceRegistration);
            _diagnosticLogger.LogDiagnosticVerbose(Constants.Diagnostics.RazorPagesAdded);
            _diagnosticLogger.LogDiagnosticVerbose(Constants.Diagnostics.ServerSideBlazorAdded);
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

            _logger.LogDebug("Adding static files and routing...");
            app.UseStaticFiles();
            app.UseRouting();

            // Custom middleware
            if (_options.ConfigurePipeline != null)
            {
                _logger.LogDebug("Invoking user-configured pipeline...");
                _options.ConfigurePipeline.Invoke(app);
            }

            _logger.LogDebug("Mapping Blazor hub...");
            app.MapBlazorHub();

            _diagnosticLogger.LogDiagnosticVerbose("Blazor SignalR hub mapped at {BlazorHub}", Constants.Endpoints.BlazorHub);
            
            _logger.LogDebug("Setting up Blazor Server...");
            
            // Find the App component type from the entry assembly
            var entryAssembly = System.Reflection.Assembly.GetEntryAssembly();

            _diagnosticLogger.LogDiagnosticVerbose("Entry assembly: {AssemblyName}", entryAssembly?.FullName ?? "NULL");

            var appType = entryAssembly?.GetType(Constants.ComponentNames.App) ?? entryAssembly?.GetTypes().FirstOrDefault(t => t.Name == Constants.ComponentNames.App);
            if (appType != null)
            {
                _diagnosticLogger.LogDiagnosticVerbose("Found App component: {AppType}", appType.FullName);
                _diagnosticLogger.LogDiagnosticVerbose("App component assembly: {Assembly}", appType.Assembly.FullName);
                _diagnosticLogger.LogDiagnosticVerbose("App component base type: {BaseType}", appType.BaseType?.FullName ?? "NULL");
            }
            else
            {
                _diagnosticLogger.LogWarning($"{Constants.Diagnostics.Prefix} App component NOT FOUND in entry assembly");
                if (_diagnosticLogger.DiagnosticsEnabled)
                {
                    var allTypes = entryAssembly?.GetTypes().Where(t => t.Name.Contains(Constants.ComponentNames.App)).ToList();
                    _diagnosticLogger.LogDiagnosticVerbose("Found {Count} types containing 'App':", allTypes?.Count ?? 0);
                    if (allTypes != null)
                    {
                        foreach (var type in allTypes)
                        {
                            _diagnosticLogger.LogDiagnosticVerbose("- {TypeName} ({FullName})", type.Name, type.FullName);
                        }
                    }
                }
            }
            
            _logger.LogDebug("Setting up standard Blazor Server routing...");
            
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
            
            // Use standard Blazor Server approach - user should create Components/_Host.cshtml
            app.MapRazorPages();
            
            // Add diagnostics to check if _Host.cshtml exists
            if (_diagnosticLogger.DiagnosticsEnabled)
            {
                var contentRoot = !string.IsNullOrEmpty(_options.ContentRoot) ? _options.ContentRoot : Directory.GetCurrentDirectory();
                var hostPath = Path.Combine(contentRoot, Constants.Paths.ComponentsDirectory, Constants.Paths.HostFile);
                var hostExists = File.Exists(hostPath);
                _diagnosticLogger.LogDiagnosticVerbose("Content root: {ContentRoot}", contentRoot);
                _diagnosticLogger.LogDiagnosticVerbose("Looking for _Host.cshtml at: {HostPath}", hostPath);
                _diagnosticLogger.LogDiagnosticVerbose("_Host.cshtml exists: {HostExists}", hostExists);

                if (!hostExists)
                {
                    _diagnosticLogger.LogWarning($"{Constants.Diagnostics.Prefix} _Host.cshtml NOT FOUND! This will cause 'Cannot find the fallback endpoint' error");
                    _diagnosticLogger.LogDiagnosticVerbose("Content root directory contents:");
                    try
                    {
                        var files = Directory.GetFileSystemEntries(contentRoot, Constants.SearchPatterns.AllFiles, SearchOption.AllDirectories)
                            .Where(f => f.Contains(Constants.SearchPatterns.HostFiles) || f.Contains(Constants.SearchPatterns.CsHtmlFiles))
                            .Take(10);
                        foreach (var file in files)
                        {
                            _diagnosticLogger.LogDiagnosticVerbose("- {File}", Path.GetRelativePath(contentRoot, file));
                        }
                    }
                    catch (Exception ex)
                    {
                        _diagnosticLogger.LogWarning($"{Constants.Diagnostics.Prefix} Could not enumerate directory: {{Error}}", ex.Message);
                    }
                }
            }

            app.MapFallbackToPage(Constants.Endpoints.HostPage);
            _diagnosticLogger.LogDiagnosticVerbose("Razor pages mapped, fallback to {HostPage} configured", Constants.Endpoints.HostPage);
            
            // Add a more informative error page if _Host.cshtml is missing
            app.Use(async (context, next) =>
            {
                await next();
                if (context.Response.StatusCode == Constants.Http.StatusCodeNotFound && context.Request.Path == "/")
                {
                    context.Response.StatusCode = Constants.Http.StatusCodeInternalServerError;
                    context.Response.ContentType = Constants.Http.ContentTypeHtml;
                    var errorHtml = $@"
<html>
<head><title>Configuration Error</title></head>
<body>
<h1>CheapAvaloniaBlazor Configuration Error</h1>
<p><strong>Missing Components/_Host.cshtml file</strong></p>
<p>Please create Components/_Host.cshtml as documented in the README:</p>
<pre>
@page ""/"" 
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@{{
    Layout = ""_Layout"";
    ViewData[""Title""] = ""Home"";
}}

&lt;component type=""typeof(App)"" render-mode=""{_options.RecommendedRenderMode}"" /&gt;
</pre>
<p><strong>Render Mode Options:</strong></p>
<ul>
<li><code>ServerPrerendered</code> - Faster initial load, but requires NavigationManager initialization</li>
<li><code>Server</code> - Slower initial load, but more reliable with complex components</li>
</ul>
<p>If you get 'RemoteNavigationManager has not been initialized' errors, try changing render-mode to ""Server"".</p>
<p>See the full setup guide at: <a href=""https://github.com/CheapNud/CheapAvaloniaBlazor"">https://github.com/CheapNud/CheapAvaloniaBlazor</a></p>
</body>
</html>";
                    await context.Response.Body.WriteAsync(System.Text.Encoding.UTF8.GetBytes(errorHtml));
                }
            });
            
            _logger.LogInformation("Blazor Server routing configured - expecting user to create Components/_Host.cshtml as per README");

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

