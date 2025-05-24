using Avalonia.Controls;
using CheapAvaloniaBlazor.Extensions;
using CheapAvaloniaBlazor.Windows;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Fluent builder for configuring Blazor host windows
/// </summary>
public class HostBuilder
{
    private readonly IServiceCollection _services;
    private readonly CheapAvaloniaBlazorOptions _options;
    private Action<Window>? _windowConfiguration;

    public IServiceCollection Services => _services;
    public CheapAvaloniaBlazorOptions Options => _options;

    public HostBuilder()
    {
        _services = new ServiceCollection();
        _options = new CheapAvaloniaBlazorOptions();

        // Add default services
        _services.AddLogging();
        _services.AddCheapAvaloniaBlazor(_options);
    }

    /// <summary>
    /// Configure the window properties
    /// </summary>
    public HostBuilder ConfigureWindow(Action<Window> configure)
    {
        _windowConfiguration = configure;
        return this;
    }

    /// <summary>
    /// Set the window title
    /// </summary>
    public HostBuilder WithTitle(string title)
    {
        _options.DefaultWindowTitle = title;
        return this;
    }

    /// <summary>
    /// Set the window size
    /// </summary>
    public HostBuilder WithSize(int width, int height)
    {
        _options.DefaultWindowWidth = width;
        _options.DefaultWindowHeight = height;
        return this;
    }

    /// <summary>
    /// Configure the Blazor host options
    /// </summary>
    public HostBuilder ConfigureOptions(Action<CheapAvaloniaBlazorOptions> configure)
    {
        configure(_options);
        return this;
    }

    /// <summary>
    /// Use a specific port for the Blazor server
    /// </summary>
    public HostBuilder UsePort(int port)
    {
        _options.Port = port;
        return this;
    }

    /// <summary>
    /// Enable HTTPS for the Blazor server
    /// </summary>
    public HostBuilder UseHttps(bool useHttps = true)
    {
        _options.UseHttps = useHttps;
        return this;
    }

    /// <summary>
    /// Build the window with default type
    /// </summary>
    public BlazorHostWindow Build()
    {
        return Build<BlazorHostWindow>();
    }

    /// <summary>
    /// Build the window with custom type
    /// </summary>
    public T Build<T>() where T : Window, IBlazorWindow, new()
    {
        // Configure services with user options
        _options.ConfigureServices = serviceCollection =>
        {
            foreach (var service in _services)
            {
                serviceCollection.Add(service);
            }
        };

        var serviceProvider = _services.BuildServiceProvider();

        // Initialize runtime
        CheapAvaloniaBlazorRuntime.Initialize(serviceProvider);

        // Create window
        var window = new T();

        // Apply window configuration
        if (window is BlazorHostWindow blazorWindow)
        {
            blazorWindow.Title = _options.DefaultWindowTitle;
            blazorWindow.Width = _options.DefaultWindowWidth;
            blazorWindow.Height = _options.DefaultWindowHeight;
        }

        _windowConfiguration?.Invoke(window);

        return window;
    }
}