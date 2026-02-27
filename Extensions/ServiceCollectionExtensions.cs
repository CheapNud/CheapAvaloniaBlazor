using CheapAvaloniaBlazor.Configuration;
using CheapAvaloniaBlazor.Services;
using CheapHelpers.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace CheapAvaloniaBlazor.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCheapAvaloniaBlazor(this IServiceCollection services, CheapAvaloniaBlazorOptions? options = null)
    {
        options ??= new CheapAvaloniaBlazorOptions();

        // ============================================================================
        // WARNING: DUAL DI CONTAINER ARCHITECTURE
        // ============================================================================
        // These services are registered here for the Avalonia-side container (HostBuilder).
        // A SECOND container exists in EmbeddedBlazorHostService.ConfigureServices() for
        // the Blazor WebApplication. All service descriptors from this container are copied
        // into the Blazor container via _options.ConfigureServices, which creates NEW
        // singleton instances (not the same objects).
        //
        // Any singleton that is used by BOTH Avalonia (tray, overlay windows, message
        // handler) AND Blazor (@inject in .razor components) MUST be re-registered as
        // the SAME INSTANCE in EmbeddedBlazorHostService.ConfigureServices() using:
        //     var svc = CheapAvaloniaBlazorRuntime.GetRequiredService<IMyService>();
        //     services.AddSingleton(svc);
        //
        // If you add a new singleton here that Blazor components will inject, you MUST
        // also add a runtime instance override in EmbeddedBlazorHostService.ConfigureServices().
        // Failure to do so will create two separate instances (e.g. two tray icons).
        //
        // See: EmbeddedBlazorHostService.cs → ConfigureServices() → "Runtime instance overrides"
        // ============================================================================

        services.AddSingleton(options);
        services.AddSingleton<IBlazorHostService, EmbeddedBlazorHostService>();

        services.AddSingleton<IDiagnosticLoggerFactory, DiagnosticLoggerFactory>();
        services.AddSingleton<PhotinoMessageHandler>();
        services.AddSingleton<ISystemTrayService, SystemTrayService>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddFileSettingsService(settingsOpts =>
        {
            settingsOpts.AppName = options.SettingsAppName ?? options.DefaultWindowTitle;
            settingsOpts.Folder = options.SettingsFolder;
            settingsOpts.FileName = options.SettingsFileName;
            settingsOpts.AutoSave = options.AutoSaveSettings;
        });
        services.AddSingleton<IAppLifecycleService, AppLifecycleService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IHotkeyService, HotkeyService>();
        services.AddSingleton<IMenuBarService, MenuBarService>();
        services.AddSingleton<IWindowService, WindowService>();
        services.AddSingleton<IDragDropService, DragDropService>();

        // Scoped services are fine - each Blazor circuit gets its own instance anyway
        services.AddScoped<IDesktopInteropService, DesktopInteropService>();

        return services;
    }

    public static IServiceCollection AddCheapBlazorDesktop(
        this IServiceCollection services,
        Action<IServiceCollection>? configureServices = null)
    {
        services.AddCheapAvaloniaBlazor();
        configureServices?.Invoke(services);
        return services;
    }
}