using Avalonia.Controls;
using Avalonia.Interactivity;
using CheapAvaloniaBlazor.Extensions;
using CheapAvaloniaBlazor.Services;
using Photino.NET;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

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
        Width = 1200;
        Height = 800;

        Loaded += OnWindowLoaded;
        Closing += OnWindowClosing;
    }

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        throw new NotImplementedException();
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

        // Create Photino window
        _photinoWindow = new PhotinoWindow()
            .SetTitle(Title)
            .SetSize((int)Width, (int)Height)
            .Center()
            .Load(_blazorHost.BaseUrl);

        // Hide Avalonia window
        Hide();

        // Show Photino window
        _photinoWindow.WaitForClose();
    }
}