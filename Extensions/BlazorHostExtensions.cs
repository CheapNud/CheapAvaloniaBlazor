using CheapAvaloniaBlazor.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CheapAvaloniaBlazor.Extensions;

/// <summary>
/// Extension methods for IBlazorHostService
/// </summary>
public static class BlazorHostExtensions
{
    /// <summary>
    /// Safely start the Blazor host with error handling and logging
    /// </summary>
    public static async Task<bool> SafeStartAsync<T>(
        this IBlazorHostService blazorHost,
        IServiceProvider serviceProvider)
    {
        try
        {
            await blazorHost.StartAsync();
            serviceProvider.GetService<ILogger<T>>()
                ?.LogInformation("Blazor host started successfully");
            return true;
        }
        catch (Exception ex)
        {
            serviceProvider.GetService<ILogger<T>>()
                ?.LogError(ex, "Failed to start Blazor host");
            return false;
        }
    }
}
