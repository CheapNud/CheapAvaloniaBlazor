using CheapAvaloniaBlazor.Configuration;
using CheapAvaloniaBlazor.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CheapAvaloniaBlazor.Hosting;

/// <summary>
/// Orchestrates the Blazor desktop hosting environment
/// </summary>
public class BlazorDesktopHost : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IBlazorHostService _blazorHostService;
    private readonly PhotinoWindowManager _windowManager;
    private readonly CheapAvaloniaBlazorOptions _options;
    private readonly ILogger<BlazorDesktopHost> _logger;
    private readonly IHostApplicationLifetime? _lifetime;

    private bool _isRunning;
    private bool _disposed;

    public BlazorDesktopHost(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _blazorHostService = serviceProvider.GetRequiredService<IBlazorHostService>();
        _windowManager = serviceProvider.GetRequiredService<PhotinoWindowManager>();
        _options = serviceProvider.GetRequiredService<CheapAvaloniaBlazorOptions>();
        _logger = serviceProvider.GetRequiredService<ILogger<BlazorDesktopHost>>();
        _lifetime = serviceProvider.GetService<IHostApplicationLifetime>();
    }

    /// <summary>
    /// Gets whether the host is currently running
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Gets the service provider
    /// </summary>
    public IServiceProvider Services => _serviceProvider;

    /// <summary>
    /// Start the desktop host
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning)
        {
            throw new InvalidOperationException("Host is already running");
        }

        try
        {
            _logger.LogInformation("Starting Blazor desktop host...");

            // Start the Blazor server
            var blazorUrl = await _blazorHostService.StartAsync(cancellationToken);
            _logger.LogInformation("Blazor server started at {Url}", blazorUrl);

            // Initialize the Photino window
            _windowManager.InitializeWindow(blazorUrl, _options);
            _logger.LogInformation("Photino window initialized");

            _isRunning = true;

            // Register for application lifetime events if available
            if (_lifetime != null)
            {
                _lifetime.ApplicationStopping.Register(() => _ = StopAsync());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Blazor desktop host");
            throw;
        }
    }

    /// <summary>
    /// Run the host and wait for window close
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
        {
            await StartAsync(cancellationToken);
        }

        _logger.LogInformation("Running Blazor desktop application...");

        // Show window and wait for close
        await _windowManager.ShowAndWaitForCloseAsync();

        _logger.LogInformation("Window closed, shutting down...");

        // Stop the host
        await StopAsync(cancellationToken);
    }

    /// <summary>
    /// Stop the desktop host
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
        {
            return;
        }

        try
        {
            _logger.LogInformation("Stopping Blazor desktop host...");

            // Close the window
            _windowManager.Close();

            // Stop the Blazor server
            await _blazorHostService.StopAsync(cancellationToken);

            _isRunning = false;
            _logger.LogInformation("Blazor desktop host stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping Blazor desktop host");
            throw;
        }
    }

    /// <summary>
    /// Create and configure a BlazorDesktopHost
    /// </summary>
    public static BlazorDesktopHost Create(Action<HostBuilder>? configure = null)
    {
        var builder = new HostBuilder();
        configure?.Invoke(builder);

        var serviceProvider = builder.Services.BuildServiceProvider();
        return new BlazorDesktopHost(serviceProvider);
    }

    /// <summary>
    /// Create and run a BlazorDesktopHost
    /// </summary>
    public static async Task RunAsync(Action<HostBuilder>? configure = null, CancellationToken cancellationToken = default)
    {
        using var host = Create(configure);
        await host.RunAsync(cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_isRunning)
        {
            StopAsync().GetAwaiter().GetResult();
        }

        (_serviceProvider as IDisposable)?.Dispose();
        _disposed = true;
    }
}