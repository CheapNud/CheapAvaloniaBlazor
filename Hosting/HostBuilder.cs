﻿using Avalonia.Controls;
using CheapAvaloniaBlazor.Configuration;
using CheapAvaloniaBlazor.Extensions;
using CheapAvaloniaBlazor.Services;
using CheapAvaloniaBlazor.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using System;
using System.Threading.Tasks;

namespace CheapAvaloniaBlazor.Hosting;

/// <summary>
/// Fluent builder for configuring Blazor host windows
/// </summary>
public class HostBuilder
{
    private readonly IServiceCollection _services;
    private readonly CheapAvaloniaBlazorOptions _options;
    private Action<Window>? _windowConfiguration;
    private Action<IServiceProvider>? _serviceProviderConfiguration;
    private bool _servicesConfigured = false;

    /// <summary>
    /// Gets the service collection for adding custom services
    /// </summary>
    public IServiceCollection Services => _services;

    /// <summary>
    /// Gets the options for configuring the Blazor host
    /// </summary>
    public CheapAvaloniaBlazorOptions Options => _options;

    /// <summary>
    /// Initializes a new instance of the HostBuilder
    /// </summary>
    public HostBuilder()
    {
        _services = new ServiceCollection();
        _options = new CheapAvaloniaBlazorOptions();

        // Add default services
        ConfigureDefaultServices();
    }

    /// <summary>
    /// Configure default services
    /// </summary>
    private void ConfigureDefaultServices()
    {
        // Add logging
        _services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Information);

            if (_options.EnableConsoleLogging)
            {
                logging.AddConsole();
            }

            logging.AddDebug();
        });

        // Add CheapAvaloniaBlazor core services
        _services.AddCheapAvaloniaBlazor(_options);
    }

    /// <summary>
    /// Configure the window properties
    /// </summary>
    /// <param name="configure">Action to configure the window</param>
    /// <returns>The builder for chaining</returns>
    public HostBuilder ConfigureWindow(Action<Window> configure)
    {
        _windowConfiguration = configure;
        return this;
    }

    /// <summary>
    /// Configure services after the service provider is built
    /// </summary>
    /// <param name="configure">Action to configure services</param>
    /// <returns>The builder for chaining</returns>
    public HostBuilder ConfigureServices(Action<IServiceProvider> configure)
    {
        _serviceProviderConfiguration = configure;
        return this;
    }

    /// <summary>
    /// Set the window title
    /// </summary>
    /// <param name="title">The window title</param>
    /// <returns>The builder for chaining</returns>
    public HostBuilder WithTitle(string title)
    {
        _options.DefaultWindowTitle = title;
        return this;
    }

    /// <summary>
    /// Set the window size
    /// </summary>
    /// <param name="width">Window width</param>
    /// <param name="height">Window height</param>
    /// <returns>The builder for chaining</returns>
    public HostBuilder WithSize(int width, int height)
    {
        _options.DefaultWindowWidth = width;
        _options.DefaultWindowHeight = height;
        return this;
    }

    /// <summary>
    /// Set the window position
    /// </summary>
    /// <param name="left">Window left position</param>
    /// <param name="top">Window top position</param>
    /// <returns>The builder for chaining</returns>
    public HostBuilder WithPosition(int left, int top)
    {
        _options.WindowLeft = left;
        _options.WindowTop = top;
        _options.CenterWindow = false;
        return this;
    }

    /// <summary>
    /// Configure the Blazor host options
    /// </summary>
    /// <param name="configure">Action to configure options</param>
    /// <returns>The builder for chaining</returns>
    public HostBuilder ConfigureOptions(Action<CheapAvaloniaBlazorOptions> configure)
    {
        configure(_options);
        return this;
    }

    /// <summary>
    /// Use a specific port for the Blazor server
    /// </summary>
    /// <param name="port">Port number</param>
    /// <returns>The builder for chaining</returns>
    public HostBuilder UsePort(int port)
    {
        _options.Port = port;
        return this;
    }

    /// <summary>
    /// Enable HTTPS for the Blazor server
    /// </summary>
    /// <param name="useHttps">Whether to use HTTPS</param>
    /// <returns>The builder for chaining</returns>
    public HostBuilder UseHttps(bool useHttps = true)
    {
        _options.UseHttps = useHttps;
        return this;
    }

    /// <summary>
    /// Enable console logging
    /// </summary>
    /// <param name="enable">Whether to enable console logging</param>
    /// <returns>The builder for chaining</returns>
    public HostBuilder EnableConsoleLogging(bool enable = true)
    {
        _options.EnableConsoleLogging = enable;

        // Reconfigure logging if services not yet built
        if (!_servicesConfigured)
        {
            _services.Configure<LoggerFilterOptions>(options =>
            {
                options.MinLevel = enable ? LogLevel.Debug : LogLevel.Information;
            });
        }

        return this;
    }

    /// <summary>
    /// Enable developer tools in the web view
    /// </summary>
    /// <param name="enable">Whether to enable dev tools</param>
    /// <returns>The builder for chaining</returns>
    public HostBuilder EnableDevTools(bool enable = true)
    {
        _options.EnableDevTools = enable;
        return this;
    }

    /// <summary>
    /// Set whether the window should be centered on screen
    /// </summary>
    /// <param name="center">Whether to center the window</param>
    /// <returns>The builder for chaining</returns>
    public HostBuilder CenterWindow(bool center = true)
    {
        _options.CenterWindow = center;
        if (center)
        {
            _options.WindowLeft = null;
            _options.WindowTop = null;
        }
        return this;
    }

    /// <summary>
    /// Set whether the window should be resizable
    /// </summary>
    /// <param name="resizable">Whether the window is resizable</param>
    /// <returns>The builder for chaining</returns>
    public HostBuilder Resizable(bool resizable = true)
    {
        _options.Resizable = resizable;
        return this;
    }

    /// <summary>
    /// Set the window to be chromeless
    /// </summary>
    /// <param name="chromeless">Whether the window should be chromeless</param>
    /// <returns>The builder for chaining</returns>
    public HostBuilder Chromeless(bool chromeless = true)
    {
        _options.Chromeless = chromeless;
        return this;
    }

    /// <summary>
    /// Set the window icon
    /// </summary>
    /// <param name="iconPath">Path to the icon file</param>
    /// <returns>The builder for chaining</returns>
    public HostBuilder WithIcon(string iconPath)
    {
        _options.IconPath = iconPath;
        return this;
    }

    /// <summary>
    /// Configure the Blazor server pipeline
    /// </summary>
    /// <param name="configure">Action to configure the pipeline</param>
    /// <returns>The builder for chaining</returns>
    public HostBuilder ConfigurePipeline(Action<Microsoft.AspNetCore.Builder.WebApplication> configure)
    {
        _options.ConfigurePipeline = configure;
        return this;
    }

    /// <summary>
    /// Configure custom endpoints
    /// </summary>
    /// <param name="configure">Action to configure endpoints</param>
    /// <returns>The builder for chaining</returns>
    public HostBuilder ConfigureEndpoints(Action<Microsoft.AspNetCore.Builder.WebApplication> configure)
    {
        _options.ConfigureEndpoints = configure;
        return this;
    }

    /// <summary>
    /// Set the content root path
    /// </summary>
    /// <param name="path">Content root path</param>
    /// <returns>The builder for chaining</returns>
    public HostBuilder UseContentRoot(string path)
    {
        _options.ContentRoot = path;
        return this;
    }

    /// <summary>
    /// Set the web root path
    /// </summary>
    /// <param name="path">Web root path</param>
    /// <returns>The builder for chaining</returns>
    public HostBuilder UseWebRoot(string path)
    {
        _options.WebRoot = path;
        return this;
    }

    /// <summary>
    /// Build the window with default type
    /// </summary>
    /// <returns>A configured BlazorHostWindow</returns>
    public BlazorHostWindow Build()
    {
        return Build<BlazorHostWindow>();
    }

    /// <summary>
    /// Build the window with custom type
    /// </summary>
    /// <typeparam name="T">The window type</typeparam>
    /// <returns>A configured window of type T</returns>
    public T Build<T>() where T : Window, IBlazorWindow, new()
    {
        // Mark services as configured
        _servicesConfigured = true;

        // Configure services with user-provided services
        _options.ConfigureServices = serviceCollection =>
        {
            // Copy all services from the builder to the actual service collection
            foreach (var service in _services)
            {
                serviceCollection.Add(service);
            }
        };

        // Build the service provider
        var serviceProvider = _services.BuildServiceProvider();

        // Initialize the runtime with the service provider
        CheapAvaloniaBlazorRuntime.Initialize(serviceProvider);

        // Apply service provider configuration
        _serviceProviderConfiguration?.Invoke(serviceProvider);

        // Create the window instance
        var window = CreateWindow<T>(serviceProvider);

        // Start the Blazor host asynchronously
        _ = Task.Run(async () =>
        {
            try
            {
                var blazorHost = serviceProvider.GetRequiredService<IBlazorHostService>();
                await blazorHost.StartAsync();

                var logger = serviceProvider.GetService<ILogger<HostBuilder>>();
                logger?.LogInformation("Blazor host started successfully");
            }
            catch (Exception ex)
            {
                var logger = serviceProvider.GetService<ILogger<HostBuilder>>();
                logger?.LogError(ex, "Failed to start Blazor host");
                throw;
            }
        });

        return window;
    }

    /// <summary>
    /// Create and configure the window
    /// </summary>
    private T CreateWindow<T>(IServiceProvider serviceProvider) where T : Window, IBlazorWindow, new()
    {
        // Create the window instance
        var window = new T();

        // Configure window based on type
        if (window is BlazorHostWindow blazorWindow)
        {
            // Set window properties from options
            blazorWindow.Title = _options.DefaultWindowTitle;
            blazorWindow.Width = _options.DefaultWindowWidth;
            blazorWindow.Height = _options.DefaultWindowHeight;

            // Configure additional Avalonia window properties
            if (window is Window avaloniaWindow)
            {
                avaloniaWindow.CanResize = _options.Resizable;

                if (_options.CenterWindow)
                {
                    avaloniaWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }
                else if (_options.WindowLeft.HasValue && _options.WindowTop.HasValue)
                {
                    avaloniaWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                    avaloniaWindow.Position = new Avalonia.PixelPoint(
                        _options.WindowLeft.Value,
                        _options.WindowTop.Value);
                }

                // Set icon if specified
                if (!string.IsNullOrEmpty(_options.IconPath) && System.IO.File.Exists(_options.IconPath))
                {
                    try
                    {
                        avaloniaWindow.Icon = new WindowIcon(_options.IconPath);
                    }
                    catch (Exception ex)
                    {
                        var logger2 = serviceProvider.GetService<ILogger<HostBuilder>>();
                        logger2?.LogWarning(ex, "Failed to load window icon from: {IconPath}", _options.IconPath);
                    }
                }
            }
        }

        // Apply any custom window configuration
        _windowConfiguration?.Invoke(window);

        // Log window creation
        var logger = serviceProvider.GetService<ILogger<HostBuilder>>();
        logger?.LogInformation("Created {WindowType} with title '{Title}' at {Width}x{Height}",
            typeof(T).Name,
            window.Title,
            window.Width,
            window.Height);

        return window;
    }
}

/// <summary>
/// Extension methods for HostBuilder
/// </summary>
public static class HostBuilderExtensions
{
    /// <summary>
    /// Add MudBlazor services with default configuration
    /// </summary>
    public static HostBuilder AddMudBlazor(this HostBuilder builder)
    {
        builder.Services.AddMudServices();
        return builder;
    }

    /// <summary>
    /// Add MudBlazor services with custom configuration
    /// </summary>
    public static HostBuilder AddMudBlazor(this HostBuilder builder,
        Action<MudServicesConfiguration> configure)
    {
        builder.Services.AddMudServices(configure);
        return builder;
    }

    /// <summary>
    /// Add HttpClient services
    /// </summary>
    public static HostBuilder AddHttpClient(this HostBuilder builder)
    {
        builder.Services.AddHttpClient();
        return builder;
    }

    /// <summary>
    /// Add a named HttpClient
    /// </summary>
    public static HostBuilder AddHttpClient(this HostBuilder builder,
        string name,
        Action<HttpClient> configure)
    {
        builder.Services.AddHttpClient(name, configure);
        return builder;
    }

    /// <summary>
    /// Add a typed HttpClient
    /// </summary>
    public static HostBuilder AddHttpClient<TClient>(this HostBuilder builder,
        Action<HttpClient>? configure = null)
        where TClient : class
    {
        if (configure != null)
        {
            builder.Services.AddHttpClient<TClient>(configure);
        }
        else
        {
            builder.Services.AddHttpClient<TClient>();
        }
        return builder;
    }
}