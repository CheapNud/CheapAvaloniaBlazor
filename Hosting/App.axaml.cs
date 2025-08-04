using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CheapAvaloniaBlazor.Configuration;
using CheapAvaloniaBlazor.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace CheapAvaloniaBlazor.Hosting;

public partial class AvaloniaApp : Application
{
    private CheapAvaloniaBlazorOptions? _options;
    private IServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void Initialize(CheapAvaloniaBlazorOptions options, IServiceProvider serviceProvider)
    {
        _options = options;
        _serviceProvider = serviceProvider;
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new AvaloniaMainWindow(_options, _serviceProvider);
        }

        base.OnFrameworkInitializationCompleted();
    }
}