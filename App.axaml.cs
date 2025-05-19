using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace CheapAvaloniaBlazor;

public partial class App : Application
{
    private WebApplication? _blazorApp;
    private static int _currentPort = 5000;

    public static App Instance { get; private set; } = null!;
    public static IServiceProvider? Services { get; private set; }
    public static string BlazorUrl => $"http://localhost:{_currentPort}";
    public static bool IsBlazorServerReady { get; private set; } = false;

    private static int FindAvailablePort(int startPort = 5000)
    {
        for (int port = startPort; port <= startPort + 100; port++)
        {
            try
            {
                using var listener = new TcpListener(IPAddress.Loopback, port);
                listener.Start();
                listener.Stop();
                System.Diagnostics.Debug.WriteLine($"Found available port: {port}");
                return port;
            }
            catch (SocketException)
            {
                // Port is in use, try next one
                continue;
            }
        }
        throw new Exception("No available ports found in range");
    }

    public static void ResetBlazorServer()
    {
        IsBlazorServerReady = false;
        _currentPort = FindAvailablePort();
        System.Diagnostics.Debug.WriteLine($"Reset Blazor server to port: {_currentPort}");
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        Instance = this; // Set instance for debug window

        // Find an available port on startup
        ResetBlazorServer();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Log platform information for debugging
            PlatformHelper.LogPlatformInfo();

            // Check if we're in debug mode
#if DEBUG
            // Show debug strategy selection window
            System.Diagnostics.Debug.WriteLine("Debug mode: Showing strategy selection window");
            desktop.MainWindow = new DebugStrategyWindow();
#else
            // Production mode: Use automatic strategy selection
            var strategy = PlatformHelper.GetRecommendedWebViewStrategy();
            System.Diagnostics.Debug.WriteLine($"Primary strategy: {strategy}");

            // Use Avalonia with multi-tier fallback (Photino.NET -> WebView.Avalonia -> Embedded Browser)
            System.Diagnostics.Debug.WriteLine("Using Avalonia multi-tier fallback mode");
            desktop.MainWindow = new MainWindow();
            _ = Task.Run(StartBlazorServer);
#endif

            desktop.Exit += OnExit;
        }

        base.OnFrameworkInitializationCompleted();
    }

    public async Task StartBlazorServerForFallback()
    {
        await StartBlazorServer();
    }

    private async Task StartBlazorServer()
    {
        try
        {
            // Stop any existing server first
            if (_blazorApp != null)
            {
                await _blazorApp.StopAsync();
                await _blazorApp.DisposeAsync();
                _blazorApp = null;
                IsBlazorServerReady = false;
            }

            // Find a new available port
            _currentPort = FindAvailablePort(_currentPort);

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
            IsBlazorServerReady = false;
        }
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.Configure<RazorPagesOptions>(options =>
        {
            options.RootDirectory = "/Components";
        });

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

#if DEBUG
        services.AddLogging(builder => builder.AddDebug());
#endif
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        _blazorApp?.StopAsync().Wait(TimeSpan.FromSeconds(5));
        _blazorApp?.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(5));
    }
}