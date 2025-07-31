using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CheapAvaloniaBlazor.Configuration;
using CheapAvaloniaBlazor.Services;
using CheapAvaloniaBlazor.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CheapAvaloniaBlazor.Hosting;

/// <summary>
/// Avalonia Application implementation for CheapAvaloniaBlazor
/// </summary>
public class CheapAvaloniaBlazorApp : Application
{
    private readonly CheapAvaloniaBlazorOptions _options;
    private readonly IServiceProvider _serviceProvider;
    private IBlazorHostService? _blazorHost;

    public CheapAvaloniaBlazorApp(CheapAvaloniaBlazorOptions options, IServiceProvider serviceProvider)
    {
        _options = options;
        _serviceProvider = serviceProvider;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Create the main window
            var window = new BlazorHostWindow(_serviceProvider.GetService<IBlazorHostService>());

            // Apply configuration
            window.Title = _options.DefaultWindowTitle;
            window.Width = _options.DefaultWindowWidth;
            window.Height = _options.DefaultWindowHeight;

            desktop.MainWindow = window;
            desktop.Exit += OnExit;

            // Start Blazor host
            _ = Task.Run(StartBlazorHostAsync);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task StartBlazorHostAsync()
    {
        try
        {
            _blazorHost = _serviceProvider.GetRequiredService<IBlazorHostService>();
            await _blazorHost.StartAsync();

            var logger = _serviceProvider.GetService<ILogger<CheapAvaloniaBlazorApp>>();
            logger?.LogInformation("Blazor host started successfully");
        }
        catch (Exception ex)
        {
            var logger = _serviceProvider.GetService<ILogger<CheapAvaloniaBlazorApp>>();
            logger?.LogError(ex, "Failed to start Blazor host");
        }
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        if (_blazorHost?.IsRunning == true)
        {
            _blazorHost.StopAsync().Wait(TimeSpan.FromSeconds(5));
        }
    }
}