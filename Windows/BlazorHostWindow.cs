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

public class BlazorHostWindow : Window, IBlazorWindow
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
        
        Title = "CheapAvaloniaBlazor App";
        Width = 1024;
        Height = 768;
        MinWidth = 640;
        MinHeight = 480;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        CanResize = true;
        ShowInTaskbar = true;

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
        
        // Minimize the Avalonia window so it's available for StorageProvider but not visible
        WindowState = Avalonia.Controls.WindowState.Minimized;
        Console.WriteLine("Avalonia window minimized - available for StorageProvider but not visible");
        
        // Create Photino window directly
        Console.WriteLine("Creating Photino window...");
        var photinoWindow = new PhotinoWindow()
            .SetTitle(_options.DefaultWindowTitle)
            .SetSize(_options.DefaultWindowWidth, _options.DefaultWindowHeight)
            .SetMinSize(640, 480)
            .SetResizable(_options.Resizable)
            .SetTopMost(false)
            .SetUseOsDefaultSize(false)
            .SetUseOsDefaultLocation(false)
            .SetDevToolsEnabled(true);

        // Apply window startup location
        if (_options.CenterWindow)
        {
            photinoWindow.Center();
        }

        Console.WriteLine($"Loading Photino window with URL: {baseUrl}");
        photinoWindow.Load(baseUrl);

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


    private async Task WaitForServerReady(string baseUrl)
    {
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(5);
        
        for (int i = 0; i < 10; i++)
        {
            try
            {
                Console.WriteLine($"Checking server readiness... attempt {i + 1}");
                var response = await httpClient.GetAsync(baseUrl);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Server is ready!");
                    // Extra delay to ensure the server is fully stabilized
                    await Task.Delay(1000);
                    Console.WriteLine("Server stabilization delay completed");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server not ready yet: {ex.Message}");
            }
            
            await Task.Delay(500);
        }
        
        Console.WriteLine("Warning: Server readiness check failed, proceeding anyway...");
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