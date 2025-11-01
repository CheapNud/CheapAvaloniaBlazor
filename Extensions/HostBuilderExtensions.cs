using CheapAvaloniaBlazor.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace CheapAvaloniaBlazor.Extensions;

/// <summary>
/// Extension methods for HostBuilder
/// </summary>
public static class HostBuilderExtensions
{
    /// <summary>
    /// Add MudBlazor services with default configuration
    /// </summary>
    public static HostBuilder AddMudBlazor(this HostBuilder builder)
    {
        builder.Services.AddMudServices();
        return builder;
    }

    /// <summary>
    /// Add MudBlazor services with custom configuration
    /// </summary>
    public static HostBuilder AddMudBlazor(this HostBuilder builder,
        Action<MudServicesConfiguration> configure)
    {
        builder.Services.AddMudServices(configure);
        return builder;
    }

    /// <summary>
    /// Add HttpClient services
    /// </summary>
    public static HostBuilder AddHttpClient(this HostBuilder builder)
    {
        builder.Services.AddHttpClient();
        return builder;
    }

    /// <summary>
    /// Add a named HttpClient
    /// </summary>
    public static HostBuilder AddHttpClient(this HostBuilder builder,
        string name,
        Action<HttpClient> configure)
    {
        builder.Services.AddHttpClient(name, configure);
        return builder;
    }

    /// <summary>
    /// Add a typed HttpClient
    /// </summary>
    public static HostBuilder AddHttpClient<TClient>(this HostBuilder builder,
        Action<HttpClient>? configure = null)
        where TClient : class
    {
        if (configure != null)
        {
            builder.Services.AddHttpClient<TClient>(configure);
        }
        else
        {
            builder.Services.AddHttpClient<TClient>();
        }
        return builder;
    }
}
