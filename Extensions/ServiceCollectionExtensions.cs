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

        // Note: Blazor services (RazorPages, ServerSideBlazor) are registered 
        // in EmbeddedBlazorHostService.ConfigureServices() to avoid duplication issues
        // Projects should use Microsoft.NET.Sdk.Web which includes MVC services automatically

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