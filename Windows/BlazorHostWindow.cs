using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CheapAvaloniaBlazor.Configuration;
using CheapAvaloniaBlazor.Services;
using CheapAvaloniaBlazor.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Photino.NET;

namespace CheapAvaloniaBlazor.Windows;

/// <summary>
/// Main window that hosts the Blazor application
/// </summary>
/// <remarks>
/// Marked as partial for future splash screen expansion with XAML code-behind
/// </remarks>
public partial class BlazorHostWindow : Window, IBlazorWindow
{
    private readonly IBlazorHostService? _blazorHost;
    private readonly CheapAvaloniaBlazorOptions? _options;
    private readonly DiagnosticLogger? _logger;

    public BlazorHostWindow()
    {

    }

    public BlazorHostWindow(IBlazorHostService? blazorHost = null)
    {
        _blazorHost = blazorHost ?? CheapAvaloniaBlazorRuntime.GetRequiredService<IBlazorHostService>();
        _options = CheapAvaloniaBlazorRuntime.GetRequiredService<CheapAvaloniaBlazorOptions>();
        var loggerFactory = CheapAvaloniaBlazorRuntime.GetRequiredService<IDiagnosticLoggerFactory>();
        _logger = loggerFactory.CreateLogger<BlazorHostWindow>();

        _logger.LogVerbose("BlazorHostWindow constructor called");
        _logger.LogVerbose("Services initialized, calling InitializeWindow");
        InitializeWindow();
        _logger.LogVerbose("BlazorHostWindow constructor completed");
    }

    public new string? Title // Match IBlazorWindow interface
    {
        get => base.Title;
        set => base.Title = value;
    }

    protected virtual void InitializeWindow()
    {
        _logger?.LogVerbose("InitializeWindow called - setting up Avalonia window");

        var splashConfig = _options?.SplashScreen;
        var showSplash = splashConfig?.Enabled ?? false;

        if (showSplash)
        {
            _logger?.LogVerbose("Splash screen enabled - showing splash during startup");

            // Configure window as visible splash screen
            Title = splashConfig!.Title;
            Width = splashConfig.Width;
            Height = splashConfig.Height;
            MinWidth = splashConfig.Width;
            MinHeight = splashConfig.Height;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            CanResize = false;
            ShowInTaskbar = true;
            Opacity = 1;
            SystemDecorations = Avalonia.Controls.SystemDecorations.None;

            // Set splash content
            Content = splashConfig.CustomContentFactory?.Invoke() ?? splashConfig.CreateDefaultContent();

            _logger?.LogVerbose($"Splash screen configured: {splashConfig.Width}x{splashConfig.Height}");
        }
        else
        {
            _logger?.LogVerbose("Splash screen disabled - hiding Avalonia window");

            // Configure window to be completely hidden but functional for StorageProvider
            Title = Constants.Framework.Name;
            Width = Constants.Defaults.MinimumWindowSize;
            Height = Constants.Defaults.MinimumWindowSize;
            MinWidth = Constants.Defaults.MinimumWindowSize;
            MinHeight = Constants.Defaults.MinimumWindowSize;
            Position = new Avalonia.PixelPoint(Constants.Defaults.OffScreenPosition, Constants.Defaults.OffScreenPosition);
            WindowStartupLocation = WindowStartupLocation.Manual;
            CanResize = false;
            ShowInTaskbar = false;
            Opacity = 0;
            SystemDecorations = Avalonia.Controls.SystemDecorations.None;
            TransparencyLevelHint = new[] { Avalonia.Controls.WindowTransparencyLevel.Transparent };

            _logger?.LogVerbose("Avalonia window configured as hidden (no decorations, transparent, off-screen)");
        }

        _logger?.LogVerbose("Subscribing to Loaded event");
        Loaded += OnWindowLoaded;

        _logger?.LogVerbose("InitializeWindow completed");
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // Don't interfere with cleanup - let the Photino window handle shutdown
        // The Avalonia window is just a bootstrap and should close cleanly
        base.OnClosing(e);
    }

    private async void OnWindowLoaded(object? sender, RoutedEventArgs e)
    {
        _logger?.LogVerbose("OnWindowLoaded called - Avalonia window loaded");

        try
        {
            // Initialize Photino using the direct approach
            await InitializePhotino();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error initializing Photino: {ErrorMessage}", ex.Message);
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }
    }

    private async Task InitializePhotino()
    {
        if (!GuardClauses.RequireServices(_logger, _blazorHost, _options))
            return;

        _logger?.LogVerbose("InitializePhotino - Using direct Photino + Avalonia StorageProvider approach");

        // Start Blazor host if not running
        if (!_blazorHost.IsRunning)
        {
            _logger?.LogVerbose("Starting Blazor host...");
            await _blazorHost.StartAsync();
        }

        // Wait for the server to be fully ready
        var baseUrl = _blazorHost.BaseUrl;
        _logger?.LogVerbose($"Blazor server URL: {baseUrl}");

        // Test the server connectivity
        await WaitForServerReady(baseUrl);

        // Hide splash screen and transition to hidden storage provider mode
        _logger?.LogVerbose("Server ready - hiding splash screen and transitioning to hidden mode");
        HideSplashScreen();
        _logger?.LogVerbose("Avalonia window transitioned to hidden mode - available for StorageProvider");

        // Create Photino window directly
        _logger?.LogVerbose("Creating Photino window...");
        var photinoWindow = new PhotinoWindow()
            .SetTitle(_options.DefaultWindowTitle)
            .SetSize(_options.DefaultWindowWidth, _options.DefaultWindowHeight)
            .SetMinSize(Constants.Defaults.MinimumResizableWidth, Constants.Defaults.MinimumResizableHeight)
            .SetResizable(_options.Resizable)
            .SetTopMost(false)
            .SetUseOsDefaultSize(false)
            .SetUseOsDefaultLocation(false)  // Prevent OS from positioning window
            .SetDevToolsEnabled(_options.EnableDevTools)
            .SetContextMenuEnabled(_options.EnableContextMenu)
            // Control Photino's native console logging based on console logging preference
            // LogVerbosity: 0=Critical only, 1=+Warnings, 2=Verbose (default), >2=All
            // Use level 1 (warnings) when disabled to still catch important issues
            .SetLogVerbosity(_options.EnableConsoleLogging ? 2 : 1);

        // ALWAYS center the window on each launch to prevent Windows from caching position
        // This ensures the window appears in the center, not in a saved position from previous runs
        _logger?.LogVerbose("Centering Photino window (prevents position caching)...");
        photinoWindow.Center();

        _logger?.LogVerbose($"Loading Photino window with URL: {baseUrl}");
        photinoWindow.Load(baseUrl);

        // Bring Photino window to foreground by temporarily setting it as TopMost
        // This ensures the window appears in front instead of staying hidden in taskbar
        _logger?.LogVerbose("Bringing Photino window to foreground...");
        photinoWindow.SetTopMost(true);

        // Small delay to ensure the window is actually shown
        await Task.Delay(Constants.Defaults.WindowBringToFrontDelayMilliseconds);

        // Remove TopMost flag so window behaves normally
        photinoWindow.SetTopMost(false);
        _logger?.LogVerbose("Photino window brought to foreground");

        // Attach message handler for JavaScript ↔ C# communication
        var messageHandler = CheapAvaloniaBlazorRuntime.GetRequiredService<PhotinoMessageHandler>();
        messageHandler.AttachToWindow(photinoWindow);

        // Wire up lifecycle service to Photino window events
        var lifecycleService = CheapAvaloniaBlazorRuntime.GetRequiredService<IAppLifecycleService>()
            as AppLifecycleService;

        photinoWindow.WindowMinimized += (s, e) => lifecycleService?.OnMinimized();
        photinoWindow.WindowMaximized += (s, e) => lifecycleService?.OnMaximized();
        photinoWindow.WindowRestored += (s, e) => lifecycleService?.OnRestored();
        photinoWindow.WindowFocusIn += (s, e) => lifecycleService?.OnActivated();
        photinoWindow.WindowFocusOut += (s, e) => lifecycleService?.OnDeactivated();

        // Register window closing handler
        photinoWindow.WindowClosing += (sender, args) =>
        {
            // Let lifecycle subscribers cancel the close first
            if (lifecycleService?.OnClosing() == true)
            {
                _logger?.LogVerbose("Photino window close cancelled by lifecycle subscriber");
                return true; // A subscriber cancelled the close
            }

            // Check if close-to-tray is enabled
            if (_options.CloseToTray)
            {
                _logger?.LogVerbose("Photino window closing - minimizing to tray instead");
                var trayService = CheapAvaloniaBlazorRuntime.GetService<ISystemTrayService>();
                if (trayService != null)
                {
                    trayService.MinimizeToTray();
                    return true; // Cancel the close, minimize to tray instead
                }
            }

            _logger?.LogVerbose("Photino window closing - shutting down application");
            Dispatcher.UIThread.Post(() =>
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
                {
                    lifetime.Shutdown();
                }
            });
            return false; // Allow window to close
        };

        _logger?.LogVerbose("About to call WaitForClose - direct approach");

        // Direct blocking call - simple and reliable
        photinoWindow.WaitForClose();
        _logger?.LogVerbose("WaitForClose completed");
    }


    private void HideSplashScreen()
    {
        // Transition Avalonia window to hidden mode for StorageProvider
        Width = Constants.Defaults.MinimumWindowSize;
        Height = Constants.Defaults.MinimumWindowSize;
        MinWidth = Constants.Defaults.MinimumWindowSize;
        MinHeight = Constants.Defaults.MinimumWindowSize;
        Position = new Avalonia.PixelPoint(Constants.Defaults.OffScreenPosition, Constants.Defaults.OffScreenPosition);
        ShowInTaskbar = false;
        Opacity = 0;
        SystemDecorations = Avalonia.Controls.SystemDecorations.None;
        TransparencyLevelHint = new[] { Avalonia.Controls.WindowTransparencyLevel.Transparent };

        // Clear content to free memory
        Content = null;
    }

    private async Task WaitForServerReady(string baseUrl)
    {
        using var httpClient = HttpClientFactory.CreateForServerCheck();

        for (int i = 0; i < Constants.Defaults.ServerReadinessMaxAttempts; i++)
        {
            try
            {
                _logger?.LogVerbose($"Checking server readiness... attempt {i + 1}");
                var response = await httpClient.GetAsync(baseUrl);
                if (response.IsSuccessStatusCode)
                {
                    _logger?.LogVerbose("Server is ready!");
                    // Extra delay to ensure the server is fully stabilized
                    await Task.Delay(Constants.Defaults.ServerStabilizationDelayMilliseconds);
                    _logger?.LogVerbose("Server stabilization delay completed");
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogVerbose($"Server not ready yet: {ex.Message}");
            }


            await Task.Delay(Constants.Defaults.ServerReadinessCheckDelayMilliseconds);
        }

        _logger?.LogWarning("Warning: Server readiness check failed, proceeding anyway...");
    }

    /// <summary>
    /// Show the window as a dialog (explicit interface implementation to match nullable signature)
    /// </summary>
    /// <param name="owner">The owner window (nullable to match interface contract)</param>
    async Task IBlazorWindow.ShowDialog(Window? owner)
    {
        await base.ShowDialog(owner!);
    }

    /// <summary>
    /// Run the window and start the application (interface implementation)
    /// </summary>
    public void Run()
    {
        // The Avalonia window initialization will trigger OnWindowLoaded
        // which will then create and run the Photino window directly
        // This method satisfies the interface requirement
    }
}