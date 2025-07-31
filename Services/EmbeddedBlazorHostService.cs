using CheapAvaloniaBlazor.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CheapAvaloniaBlazor.Services;

public class EmbeddedBlazorHostService : IBlazorHostService, IDisposable
{
    private WebApplication? _app;
    private readonly CheapAvaloniaBlazorOptions _options;
    private readonly ILogger<EmbeddedBlazorHostService> _logger;
    private CancellationTokenSource? _hostCts;

    public bool IsRunning { get; private set; }
    public string BaseUrl => $"{(_options.UseHttps ? "https" : "http")}://localhost:{_options.Port}";

    public EmbeddedBlazorHostService(
        CheapAvaloniaBlazorOptions options,
        ILogger<EmbeddedBlazorHostService> logger)
    {
        _options = options;
        _logger = logger;
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

            var builder = WebApplication.CreateBuilder();

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
            _ = Task.Run(async () =>
            {
                try
                {
                    await _app.RunAsync(_hostCts.Token);
                }
                catch (OperationCanceledException)
                {
                    // Expected when stopping
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Blazor host failed");
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
        // Add Blazor services
        services.AddRazorPages();
        services.AddServerSideBlazor();

        // Add user-configured services
        _options.ConfigureServices?.Invoke(services);

        // Add desktop interop services
        services.AddScoped<IDesktopInteropService, DesktopInteropService>();
    }

    private void ConfigurePipeline(WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            if (_options.UseHttps)
            {
                app.UseHsts();
            }
        }

        if (_options.UseHttps)
        {
            app.UseHttpsRedirection();
        }

        app.UseStaticFiles();
        app.UseRouting();

        // Custom middleware
        _options.ConfigurePipeline?.Invoke(app);

        app.MapBlazorHub();
        app.MapFallbackToPage("/_Host");

        // Map additional endpoints
        _options.ConfigureEndpoints?.Invoke(app);
    }

    private async Task WaitForStartupAsync(CancellationToken cancellationToken)
    {
        var maxWaitTime = TimeSpan.FromSeconds(30);
        var checkInterval = TimeSpan.FromMilliseconds(100);
        var startTime = DateTime.UtcNow;

        using var httpClient = new HttpClient();

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var response = await httpClient.GetAsync(BaseUrl, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch
            {
                // Server not ready yet
            }

            if (DateTime.UtcNow - startTime > maxWaitTime)
            {
                throw new TimeoutException("Blazor host failed to start within timeout period");
            }

            await Task.Delay(checkInterval, cancellationToken);
        }
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
}

