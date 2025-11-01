using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CheapAvaloniaBlazor.Configuration;
using CheapAvaloniaBlazor.Services;
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

    public BlazorHostWindow()
    {

    }

    public BlazorHostWindow(IBlazorHostService? blazorHost = null)
    {
        Console.WriteLine("BlazorHostWindow constructor called");
        
        _blazorHost = blazorHost ?? CheapAvaloniaBlazorRuntime.GetRequiredService<IBlazorHostService>();
        _options = CheapAvaloniaBlazorRuntime.GetRequiredService<CheapAvaloniaBlazorOptions>();
        
        Console.WriteLine("Services initialized, calling InitializeWindow");
        InitializeWindow();
        
        Console.WriteLine("BlazorHostWindow constructor completed");
    }

    public new string? Title // Match IBlazorWindow interface
    {
        get => base.Title;
        set => base.Title = value;
    }

    protected virtual void InitializeWindow()
    {
        Console.WriteLine("InitializeWindow called - setting up Avalonia window");

        var splashConfig = _options?.SplashScreen;
        var showSplash = splashConfig?.Enabled ?? false;

        if (showSplash)
        {
            Console.WriteLine("Splash screen enabled - showing splash during startup");

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

            Console.WriteLine($"Splash screen configured: {splashConfig.Width}x{splashConfig.Height}");
        }
        else
        {
            Console.WriteLine("Splash screen disabled - hiding Avalonia window");

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

            Console.WriteLine("Avalonia window configured as hidden (no decorations, transparent, off-screen)");
        }

        Console.WriteLine("Subscribing to Loaded event");
        Loaded += OnWindowLoaded;

        Console.WriteLine("InitializeWindow completed");
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // Don't interfere with cleanup - let the Photino window handle shutdown
        // The Avalonia window is just a bootstrap and should close cleanly
        base.OnClosing(e);
    }

    private async void OnWindowLoaded(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine("OnWindowLoaded called - Avalonia window loaded");
        
        try
        {
            // Initialize Photino using the direct approach
            await InitializePhotino();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing Photino: {ex}");
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }
    }

    private async Task InitializePhotino()
    {
        if (_blazorHost == null || _options == null)
        {
            Console.WriteLine("Error: Required services not available");
            return;
        }

        Console.WriteLine("InitializePhotino - Using direct Photino + Avalonia StorageProvider approach");
        
        // Start Blazor host if not running
        if (!_blazorHost.IsRunning)
        {
            Console.WriteLine("Starting Blazor host...");
            await _blazorHost.StartAsync();
        }

        // Wait for the server to be fully ready
        var baseUrl = _blazorHost.BaseUrl;
        Console.WriteLine($"Blazor server URL: {baseUrl}");

        // Test the server connectivity
        await WaitForServerReady(baseUrl);

        // Hide splash screen and transition to hidden storage provider mode
        Console.WriteLine("Server ready - hiding splash screen and transitioning to hidden mode");
        HideSplashScreen();
        Console.WriteLine("Avalonia window transitioned to hidden mode - available for StorageProvider");

        // Create Photino window directly
        Console.WriteLine("Creating Photino window...");
        var photinoWindow = new PhotinoWindow()
            .SetTitle(_options.DefaultWindowTitle)
            .SetSize(_options.DefaultWindowWidth, _options.DefaultWindowHeight)
            .SetMinSize(Constants.Defaults.MinimumResizableWidth, Constants.Defaults.MinimumResizableHeight)
            .SetResizable(_options.Resizable)
            .SetTopMost(false)
            .SetUseOsDefaultSize(false)
            .SetUseOsDefaultLocation(false)  // Prevent OS from positioning window
            .SetDevToolsEnabled(true);

        // ALWAYS center the window on each launch to prevent Windows from caching position
        // This ensures the window appears in the center, not in a saved position from previous runs
        Console.WriteLine("Centering Photino window (prevents position caching)...");
        photinoWindow.Center();

        Console.WriteLine($"Loading Photino window with URL: {baseUrl}");
        photinoWindow.Load(baseUrl);

        // Bring Photino window to foreground by temporarily setting it as TopMost
        // This ensures the window appears in front instead of staying hidden in taskbar
        Console.WriteLine("Bringing Photino window to foreground...");
        photinoWindow.SetTopMost(true);

        // Small delay to ensure the window is actually shown
        await Task.Delay(Constants.Defaults.WindowBringToFrontDelayMilliseconds);

        // Remove TopMost flag so window behaves normally
        photinoWindow.SetTopMost(false);
        Console.WriteLine("Photino window brought to foreground");

        // Attach message handler for JavaScript ↔ C# communication
        var messageHandler = CheapAvaloniaBlazorRuntime.GetRequiredService<PhotinoMessageHandler>();
        messageHandler.AttachToWindow(photinoWindow);

        // Register window closing handler
        photinoWindow.WindowClosing += (sender, args) =>
        {
            Console.WriteLine("Photino window closing - shutting down application");
            Dispatcher.UIThread.Post(() =>
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
                {
                    lifetime.Shutdown();
                }
            });
            return false; // Allow window to close
        };

        Console.WriteLine("About to call WaitForClose - direct approach");
        
        // Direct blocking call - simple and reliable
        photinoWindow.WaitForClose();
        Console.WriteLine("WaitForClose completed");
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
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(Constants.Defaults.HttpClientTimeoutSeconds);

        for (int i = 0; i < Constants.Defaults.ServerReadinessMaxAttempts; i++)
        {
            try
            {
                Console.WriteLine($"Checking server readiness... attempt {i + 1}");
                var response = await httpClient.GetAsync(baseUrl);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Server is ready!");
                    // Extra delay to ensure the server is fully stabilized
                    await Task.Delay(Constants.Defaults.ServerStabilizationDelayMilliseconds);
                    Console.WriteLine("Server stabilization delay completed");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server not ready yet: {ex.Message}");
            }


            await Task.Delay(Constants.Defaults.ServerReadinessCheckDelayMilliseconds);
        }

        Console.WriteLine("Warning: Server readiness check failed, proceeding anyway...");
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