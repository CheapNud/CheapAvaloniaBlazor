using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CheapAvaloniaBlazor.Configuration;
using CheapAvaloniaBlazor.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CheapAvaloniaBlazor.Windows;

public partial class AvaloniaMainWindow : Window
{
    private readonly CheapAvaloniaBlazorOptions? _options;
    private readonly IServiceProvider? _serviceProvider;
    private IBlazorHostService? _blazorHost;

    public AvaloniaMainWindow()
    {
        InitializeComponent();
    }

    public AvaloniaMainWindow(CheapAvaloniaBlazorOptions? options, IServiceProvider? serviceProvider)
    {
        _options = options;
        _serviceProvider = serviceProvider;
        InitializeComponent();
        ConfigureWindow();
        StartBlazorHost();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void ConfigureWindow()
    {
        if (_options != null)
        {
            Title = _options.DefaultWindowTitle;
            Width = _options.DefaultWindowWidth;
            Height = _options.DefaultWindowHeight;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            CanResize = _options.Resizable;
        }
    }

    private async void StartBlazorHost()
    {
        if (_serviceProvider == null) return;

        try
        {
            _blazorHost = _serviceProvider.GetRequiredService<IBlazorHostService>();
            await _blazorHost.StartAsync();

            var logger = _serviceProvider.GetService<ILogger<AvaloniaMainWindow>>();
            logger?.LogInformation("Blazor host started successfully in AvaloniaMainWindow");

            // Hide this window and show Photino window
            // Hide(); // TODO: Properly configure Photino window before hiding
        }
        catch (Exception ex)
        {
            var logger = _serviceProvider.GetService<ILogger<AvaloniaMainWindow>>();
            logger?.LogError(ex, "Failed to start Blazor host in AvaloniaMainWindow");
        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (_blazorHost?.IsRunning == true)
        {
            _blazorHost.StopAsync().Wait(TimeSpan.FromSeconds(5));
        }
        base.OnClosing(e);
    }
}