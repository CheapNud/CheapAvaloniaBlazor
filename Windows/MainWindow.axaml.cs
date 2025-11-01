using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CheapAvaloniaBlazor.Configuration;
using CheapAvaloniaBlazor.Extensions;
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
            this.ApplyOptions(_options);
        }
    }

    private async void StartBlazorHost()
    {
        if (_serviceProvider == null) return;

        _blazorHost = _serviceProvider.GetRequiredService<IBlazorHostService>();
        await _blazorHost.SafeStartAsync<AvaloniaMainWindow>(_serviceProvider);

        // Hide this window and show Photino window
        // Hide(); // TODO: Properly configure Photino window before hiding
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