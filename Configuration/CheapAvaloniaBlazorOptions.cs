﻿using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace CheapAvaloniaBlazor.Configuration;

/// <summary>
/// Configuration options for CheapAvaloniaBlazor
/// </summary>
public class CheapAvaloniaBlazorOptions
{
    // Blazor Server Options
    /// <summary>
    /// Port number for the Blazor server
    /// </summary>
    public int Port { get; set; } = 5000;

    /// <summary>
    /// Whether to use HTTPS for the Blazor server
    /// </summary>
    public bool UseHttps { get; set; } = false;

    /// <summary>
    /// Enable console logging
    /// </summary>
    public bool EnableConsoleLogging { get; set; } = false;

    /// <summary>
    /// Enable comprehensive diagnostics logging
    /// </summary>
    public bool EnableDiagnostics { get; set; } = false;

    /// <summary>
    /// Recommended render mode for components (Server vs ServerPrerendered)
    /// This is informational only - actual render mode is set in _Host.cshtml
    /// </summary>
    public string RecommendedRenderMode { get; set; } = "ServerPrerendered";

    /// <summary>
    /// Content root path for the Blazor server
    /// </summary>
    public string? ContentRoot { get; set; }

    /// <summary>
    /// Web root path for static files
    /// </summary>
    public string? WebRoot { get; set; }

    // Window Options
    /// <summary>
    /// Default window title
    /// </summary>
    public string DefaultWindowTitle { get; set; } = "Blazor Desktop App";

    /// <summary>
    /// Default window width
    /// </summary>
    public int DefaultWindowWidth { get; set; } = 1200;

    /// <summary>
    /// Default window height
    /// </summary>
    public int DefaultWindowHeight { get; set; } = 800;

    /// <summary>
    /// Window left position (null for default)
    /// </summary>
    public int? WindowLeft { get; set; }

    /// <summary>
    /// Window top position (null for default)
    /// </summary>
    public int? WindowTop { get; set; }

    /// <summary>
    /// Whether to center the window on startup
    /// </summary>
    public bool CenterWindow { get; set; } = true;

    /// <summary>
    /// Whether the window is resizable
    /// </summary>
    public bool Resizable { get; set; } = true;

    /// <summary>
    /// Whether to show window chrome
    /// </summary>
    public bool Chromeless { get; set; } = false;

    /// <summary>
    /// Path to window icon
    /// </summary>
    public string? IconPath { get; set; }

    /// <summary>
    /// Default zoom level (percentage)
    /// </summary>
    public int DefaultZoom { get; set; } = 100;

    // WebView Options
    /// <summary>
    /// Enable developer tools in the web view
    /// </summary>
    public bool EnableDevTools { get; set; } = false;

    /// <summary>
    /// Enable context menu in the web view
    /// </summary>
    public bool EnableContextMenu { get; set; } = true;

    /// <summary>
    /// Grant all browser permissions automatically
    /// </summary>
    public bool GrantBrowserPermissions { get; set; } = true;

    // Configuration Delegates
    /// <summary>
    /// Configure services for the Blazor server
    /// </summary>
    public Action<IServiceCollection>? ConfigureServices { get; set; }

    /// <summary>
    /// Configure the Blazor server pipeline
    /// </summary>
    public Action<WebApplication>? ConfigurePipeline { get; set; }

    /// <summary>
    /// Configure custom endpoints
    /// </summary>
    public Action<WebApplication>? ConfigureEndpoints { get; set; }

    // Advanced Options
    /// <summary>
    /// Custom user agent string
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Browser arguments for Chromium
    /// </summary>
    public string[]? BrowserArgs { get; set; }

    /// <summary>
    /// Enable GPU acceleration
    /// </summary>
    public bool EnableGpuAcceleration { get; set; } = true;

    /// <summary>
    /// Max retry attempts for starting Blazor server
    /// </summary>
    public int MaxStartupRetries { get; set; } = 3;

    /// <summary>
    /// Timeout for Blazor server startup
    /// </summary>
    public TimeSpan StartupTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Whether to automatically open dev tools in debug mode
    /// </summary>
    public bool AutoOpenDevToolsInDebug { get; set; } = true;

    /// <summary>
    /// Custom CSS to inject into the web view
    /// </summary>
    public string? CustomCss { get; set; }

    /// <summary>
    /// Custom JavaScript to inject into the web view
    /// </summary>
    public string? CustomJavaScript { get; set; }

    /// <summary>
    /// Custom static file options
    /// </summary>
    public StaticFileOptions? CustomStaticFileOptions { get; set; }

    /// <summary>
    /// Maximum SignalR message size
    /// </summary>
    public long? MaximumReceiveMessageSize { get; set; }

    /// <summary>
    /// Enable automatic reconnect
    /// </summary>
    public bool EnableAutoReconnect { get; set; } = true;

    /// <summary>
    /// Reconnect interval in seconds
    /// </summary>
    public int[] ReconnectIntervals { get; set; } = { 0, 2, 10, 30 };

    /// <summary>
    /// Enable hot reload in development
    /// </summary>
    public bool EnableHotReload { get; set; } = true;

    /// <summary>
    /// Custom root component type
    /// </summary>
    public Type? RootComponentType { get; set; }

    /// <summary>
    /// Additional assemblies for component discovery
    /// </summary>
    public List<System.Reflection.Assembly> AdditionalAssemblies { get; set; } = new();

    /// <summary>
    /// Create default options
    /// </summary>
    public static CheapAvaloniaBlazorOptions CreateDefault()
    {
        return new CheapAvaloniaBlazorOptions
        {
            Port = 5000,
            UseHttps = false,
            EnableConsoleLogging = false,
            DefaultWindowTitle = "Blazor Desktop App",
            DefaultWindowWidth = 1200,
            DefaultWindowHeight = 800,
            CenterWindow = true,
            Resizable = true,
            EnableDevTools = false,
            EnableContextMenu = true,
            GrantBrowserPermissions = true,
            EnableAutoReconnect = true,
            EnableHotReload = true
        };
    }
}