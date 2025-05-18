using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MyTestApp2;

public partial class App : Application
{
    private WebApplication? _blazorApp;
    private const int PORT = 5000;

    public static IServiceProvider? Services { get; private set; }
    public static string BlazorUrl => $"http://localhost:{PORT}";
    public static bool IsBlazorServerReady { get; private set; } = false;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        // Start the Blazor server immediately
        _ = Task.Run(StartBlazorServer);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
            desktop.Exit += OnExit;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task StartBlazorServer()
    {
        try
        {
            var builder = WebApplication.CreateBuilder();

            // Configure services for Blazor Server
            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();

            ConfigureServices(builder.Services);

            // Set paths
            builder.Environment.ContentRootPath = System.AppContext.BaseDirectory;
            builder.Environment.WebRootPath = Path.Combine(System.AppContext.BaseDirectory, "wwwroot");
            builder.WebHost.UseUrls(BlazorUrl);

            // Suppress console output for cleaner experience
            builder.Logging.ClearProviders();

            _blazorApp = builder.Build();
            Services = _blazorApp.Services;

            // Configure pipeline
            if (!_blazorApp.Environment.IsDevelopment())
            {
                _blazorApp.UseExceptionHandler("/Error");
                _blazorApp.UseHsts();
            }

            _blazorApp.UseStaticFiles();
            _blazorApp.UseRouting();

            // Map Blazor Hub
            _blazorApp.MapBlazorHub();
            _blazorApp.MapRazorPages();
            _blazorApp.MapFallbackToPage("/_Host");

            System.Diagnostics.Debug.WriteLine($"Starting Blazor server at {BlazorUrl}");

            // Mark server as ready
            IsBlazorServerReady = true;

            await _blazorApp.RunAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to start Blazor server: {ex}");
        }
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Register MudBlazor services
        services.AddMudServices(config =>
        {
            config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomLeft;
            config.SnackbarConfiguration.PreventDuplicates = false;
            config.SnackbarConfiguration.NewestOnTop = false;
            config.SnackbarConfiguration.ShowCloseIcon = true;
            config.SnackbarConfiguration.VisibleStateDuration = 10000;
            config.SnackbarConfiguration.HideTransitionDuration = 500;
            config.SnackbarConfiguration.ShowTransitionDuration = 500;
            config.SnackbarConfiguration.SnackbarVariant = MudBlazor.Variant.Filled;
        });

        // Register your application services
        //services.AddHttpClient();

    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        _blazorApp?.StopAsync().Wait(TimeSpan.FromSeconds(5));
        _blazorApp?.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(5));
    }
}