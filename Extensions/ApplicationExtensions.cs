using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CheapAvaloniaBlazor.Hosting;
using CheapAvaloniaBlazor.Windows;

namespace CheapAvaloniaBlazor.Extensions;

/// <summary>
/// Extension methods for Avalonia Application
/// </summary>
public static class ApplicationExtensions
{
    /// <summary>
    /// Creates a Blazor-hosted window with custom service configuration
    /// </summary>
    public static BlazorHostWindow CreateBlazorWindow(
        this Application application,
        Action<HostBuilder>? configure = null)
    {
        var builder = new HostBuilder();
        configure?.Invoke(builder);

        return builder.Build();
    }

    /// <summary>
    /// Creates a Blazor-hosted window with a custom window type
    /// </summary>
    public static T CreateBlazorWindow<T>(
        this Application application,
        Action<HostBuilder>? configure = null)
        where T : Window, IBlazorWindow, new()
    {
        var builder = new HostBuilder();
        configure?.Invoke(builder);

        return builder.Build<T>();
    }

    /// <summary>
    /// Set the main window to a Blazor-hosted window
    /// </summary>
    public static void SetBlazorMainWindow(
        this IClassicDesktopStyleApplicationLifetime desktop,
        Action<HostBuilder>? configure = null)
    {
        if (desktop == null)
        {
            throw new ArgumentNullException(nameof(desktop));
        }

        var app = Application.Current ?? throw new InvalidOperationException("No current application");
        desktop.MainWindow = app.CreateBlazorWindow(configure);
    }

    /// <summary>
    /// Set the main window to a custom Blazor-hosted window
    /// </summary>
    public static void SetBlazorMainWindow<T>(
        this IClassicDesktopStyleApplicationLifetime desktop,
        Action<HostBuilder>? configure = null)
        where T : Window, IBlazorWindow, new()
    {
        if (desktop == null)
        {
            throw new ArgumentNullException(nameof(desktop));
        }

        var app = Application.Current ?? throw new InvalidOperationException("No current application");
        desktop.MainWindow = app.CreateBlazorWindow<T>(configure);
    }

    /// <summary>
    /// Show a new Blazor window
    /// </summary>
    public static BlazorHostWindow ShowBlazorWindow(
        this Application application,
        Action<HostBuilder>? configure = null,
        Window? owner = null)
    {
        var window = application.CreateBlazorWindow(configure);

        if (owner != null)
        {
            window.ShowDialog(owner);
        }
        else
        {
            window.Show();
        }

        return window;
    }

    /// <summary>
    /// Show a new custom Blazor window
    /// </summary>
    public static T ShowBlazorWindow<T>(
        this Application application,
        Action<HostBuilder>? configure = null,
        Window? owner = null)
        where T : Window, IBlazorWindow, new()
    {
        var window = application.CreateBlazorWindow<T>(configure);

        if (owner != null)
        {
            window.ShowDialog(owner);
        }
        else
        {
            window.Show();
        }

        return window;
    }

    /// <summary>
    /// Get the CheapAvaloniaBlazor service provider
    /// </summary>
    public static IServiceProvider? GetBlazorServices(this Application application)
    {
        return CheapAvaloniaBlazorRuntime.GetService<IServiceProvider>();
    }

    /// <summary>
    /// Get a specific service from the CheapAvaloniaBlazor container
    /// </summary>
    public static T? GetBlazorService<T>(this Application application) where T : class
    {
        return CheapAvaloniaBlazorRuntime.GetService<T>();
    }

    /// <summary>
    /// Check if CheapAvaloniaBlazor is initialized
    /// </summary>
    public static bool IsBlazorInitialized(this Application application)
    {
        try
        {
            var services = CheapAvaloniaBlazorRuntime.GetService<IServiceProvider>();
            return services != null;
        }
        catch
        {
            return false;
        }
    }
}