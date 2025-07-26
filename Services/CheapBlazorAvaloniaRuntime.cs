using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Runtime context for CheapAvaloniaBlazor
/// </summary>
public static class CheapAvaloniaBlazorRuntime
{
    private static IServiceProvider? _serviceProvider;

    internal static void Initialize(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public static T GetRequiredService<T>() where T : notnull
    {
        if (_serviceProvider == null)
        {
            throw new InvalidOperationException(
                "CheapAvaloniaBlazor has not been initialized. " +
                "Make sure to call UseCheapAvaloniaBlazor() in your AppBuilder configuration.");
        }

        return _serviceProvider.GetRequiredService<T>();
    }

    public static T? GetService<T>() where T : class
    {
        return _serviceProvider?.GetService<T>();
    }
}