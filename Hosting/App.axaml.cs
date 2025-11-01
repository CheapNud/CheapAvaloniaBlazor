using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CheapAvaloniaBlazor.Configuration;
using CheapAvaloniaBlazor.Services;
using CheapAvaloniaBlazor.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace CheapAvaloniaBlazor.Hosting;

public partial class AvaloniaApp : Application
{
    private CheapAvaloniaBlazorOptions? _options;
    private IServiceProvider? _serviceProvider;
    private DiagnosticLogger? _logger;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void Initialize(CheapAvaloniaBlazorOptions options, IServiceProvider serviceProvider)
    {
        _options = options;
        _serviceProvider = serviceProvider;
        var loggerFactory = serviceProvider.GetRequiredService<IDiagnosticLoggerFactory>();
        _logger = loggerFactory.CreateLogger<AvaloniaApp>();

        _logger.LogVerbose("=== AVALONIA APP INITIALIZE CALLED ===");
        AvaloniaXamlLoader.Load(this);
        _logger.LogVerbose("=== AVALONIA APP INITIALIZE COMPLETED ===");
    }

    public override void OnFrameworkInitializationCompleted()
    {
        _logger?.LogVerbose("OnFrameworkInitializationCompleted called");

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _logger?.LogVerbose("Setting up desktop application lifetime");

            // Prevent the application from shutting down when all windows are closed
            desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;
            _logger?.LogVerbose("ShutdownMode set to OnExplicitShutdown");

            _logger?.LogVerbose("Creating BlazorHostWindow");
            var window = new BlazorHostWindow(_serviceProvider?.GetService<IBlazorHostService>());

            _logger?.LogVerbose("Setting as MainWindow");
            desktop.MainWindow = window;

            _logger?.LogVerbose("About to call window.Show()");
            // Explicitly show the window to trigger the Loaded event
            // (Window is invisible and off-screen, but StorageProvider will be initialized)
            window.Show();
            _logger?.LogVerbose("window.Show() called - window is hidden but functional");
        }

        _logger?.LogVerbose("Calling base.OnFrameworkInitializationCompleted()");
        base.OnFrameworkInitializationCompleted();
        _logger?.LogVerbose("OnFrameworkInitializationCompleted completed");
    }
}