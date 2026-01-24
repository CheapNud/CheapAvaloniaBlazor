using CheapAvaloniaBlazor.Configuration;
using CheapAvaloniaBlazor.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CheapAvaloniaBlazor.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCheapAvaloniaBlazor(this IServiceCollection services, CheapAvaloniaBlazorOptions? options = null)
    {
        options ??= new CheapAvaloniaBlazorOptions();

        services.AddSingleton(options);
        services.AddSingleton<IBlazorHostService, EmbeddedBlazorHostService>();

        // Add diagnostic logger factory
        services.AddSingleton<IDiagnosticLoggerFactory, DiagnosticLoggerFactory>();

        // Add DesktopInteropService using Avalonia's StorageProvider
        services.AddScoped<IDesktopInteropService, DesktopInteropService>();

        // Add lightweight message handler for JavaScript ↔ C# communication
        services.AddSingleton<PhotinoMessageHandler>();

        // Note: Blazor services (RazorPages, ServerSideBlazor) are registered
        // in EmbeddedBlazorHostService.ConfigureServices() to avoid duplication issues
        // Projects should use Microsoft.NET.Sdk.Razor with FrameworkReference to Microsoft.AspNetCore.App

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