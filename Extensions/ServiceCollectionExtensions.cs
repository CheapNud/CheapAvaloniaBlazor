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
        services.AddSingleton<PhotinoWindowManager>();
        services.AddScoped<IDesktopInteropService, DesktopInteropService>();

        // Add Blazor services
        services.AddRazorPages();
        services.AddServerSideBlazor();

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