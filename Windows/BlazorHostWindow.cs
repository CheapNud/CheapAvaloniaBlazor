using Avalonia.Controls;
using Avalonia.Interactivity;
using CheapAvaloniaBlazor.Services;
using Photino.NET;

namespace CheapAvaloniaBlazor.Windows;

public class BlazorHostWindow : Window, IBlazorWindow
{
    private readonly IBlazorHostService _blazorHost;
    private PhotinoWindow? _photinoWindow;

    public BlazorHostWindow()
    {

    }

    public BlazorHostWindow(IBlazorHostService? blazorHost = null)
    {
        _blazorHost = blazorHost ?? CheapAvaloniaBlazorRuntime.GetRequiredService<IBlazorHostService>();
        InitializeWindow();
    }

    public new string Title // Explicitly override the nullability to match IBlazorWindow
    {
        get => base.Title ?? string.Empty; // Ensure non-null return
        set => base.Title = value;
    }

    protected virtual void InitializeWindow()
    {
        Title = "CheapAvaloniaBlazor App";
        Width = 1024;
        Height = 768;
        MinWidth = 640;
        MinHeight = 480;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        CanResize = true;
        ShowInTaskbar = true;

        Loaded += OnWindowLoaded;
        Closing += OnWindowClosing;
    }

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        // Clean up Photino window
        _photinoWindow?.Close();
        _photinoWindow = null;

        // Stop Blazor host service
        if (_blazorHost?.IsRunning == true)
        {
            _blazorHost.StopAsync().Wait(TimeSpan.FromSeconds(5));
        }
    }

    private async void OnWindowLoaded(object? sender, RoutedEventArgs e)
    {
        await InitializePhotinoAsync();
    }

    protected virtual async Task InitializePhotinoAsync()
    {
        // Start Blazor host if not running
        if (!_blazorHost.IsRunning)
        {
            await _blazorHost.StartAsync();
        }

        // Create Photino window with all properties
        _photinoWindow = new PhotinoWindow()
            .SetTitle(Title)
            .SetSize((int)Width, (int)Height)
            .SetMinSize((int)MinWidth, (int)MinHeight)
            .SetResizable(CanResize)
            .SetTopMost(false)
            .SetUseOsDefaultSize(false)
            .SetUseOsDefaultLocation(false);

        // Apply window startup location
        if (WindowStartupLocation == WindowStartupLocation.CenterScreen)
        {
            _photinoWindow.Center();
        }

        // Configure taskbar visibility
        if (!ShowInTaskbar)
        {
            // Note: Photino doesn't have direct taskbar control, but we can set window flags
            _photinoWindow.SetTopMost(true).SetTopMost(false); // Workaround for some cases
        }

        // Load the Blazor app
        _photinoWindow.Load(_blazorHost.BaseUrl);

        // Hide Avalonia window
        Hide();

        // Show Photino window
        _photinoWindow.WaitForClose();
    }

    /// <summary>
    /// Run the window and start the application
    /// </summary>
    public void Run()
    {
        // Skip Avalonia window creation and go directly to initializing Photino
        // This bypasses the Avalonia platform initialization issues
        Task.Run(async () =>
        {
            await InitializePhotinoAsync();
        }).Wait();
    }
}