using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CheapAvaloniaBlazor.Configuration;
using CheapAvaloniaBlazor.Extensions;
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
            window.ApplyOptions(_options);

            desktop.MainWindow = window;
            desktop.Exit += OnExit;

            // FIXED: Start Blazor host synchronously during initialization to avoid timing issues
            StartBlazorHostAsync().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    var logger = _serviceProvider.GetService<ILogger<CheapAvaloniaBlazorApp>>();
                    logger?.LogError(task.Exception, "Failed to start Blazor host during app initialization");
                }
            });
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task StartBlazorHostAsync()
    {
        _blazorHost = _serviceProvider.GetRequiredService<IBlazorHostService>();
        await _blazorHost.SafeStartAsync<CheapAvaloniaBlazorApp>(_serviceProvider);
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        if (_blazorHost?.IsRunning == true)
        {
            _blazorHost.StopAsync().Wait(TimeSpan.FromSeconds(Constants.Defaults.ServerShutdownTimeoutSeconds));
        }
    }
}