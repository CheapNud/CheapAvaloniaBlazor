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

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void Initialize(CheapAvaloniaBlazorOptions options, IServiceProvider serviceProvider)
    {
        Console.WriteLine("=== AVALONIA APP INITIALIZE CALLED ===");
        _options = options;
        _serviceProvider = serviceProvider;
        AvaloniaXamlLoader.Load(this);
        Console.WriteLine("=== AVALONIA APP INITIALIZE COMPLETED ===");
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Console.WriteLine("OnFrameworkInitializationCompleted called");
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Console.WriteLine("Setting up desktop application lifetime");
            
            // Prevent the application from shutting down when all windows are closed
            desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;
            Console.WriteLine("ShutdownMode set to OnExplicitShutdown");
            
            Console.WriteLine("Creating BlazorHostWindow");
            var window = new BlazorHostWindow(_serviceProvider?.GetService<IBlazorHostService>());
            
            Console.WriteLine("Setting as MainWindow");
            desktop.MainWindow = window;
            
            Console.WriteLine("About to call window.Show()");
            // Explicitly show the window to trigger the Loaded event
            window.Show();
            Console.WriteLine("window.Show() called");
        }

        Console.WriteLine("Calling base.OnFrameworkInitializationCompleted()");
        base.OnFrameworkInitializationCompleted();
        Console.WriteLine("OnFrameworkInitializationCompleted completed");
    }
}