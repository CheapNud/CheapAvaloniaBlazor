using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.FileProviders;
using System.Reflection;

namespace CheapAvaloniaBlazor.Configuration;

/// <summary>
/// Configuration for the Blazor host environment
/// </summary>
public class BlazorHostConfiguration
{
    /// <summary>
    /// The root component type to render
    /// </summary>
    public Type? RootComponentType { get; set; }

    /// <summary>
    /// The selector for the root component
    /// </summary>
    public string RootComponentSelector { get; set; } = Constants.ComponentNames.RootComponentSelector;

    /// <summary>
    /// Additional assemblies to scan for components
    /// </summary>
    public List<Assembly> AdditionalAssemblies { get; set; } = new();

    /// <summary>
    /// Static file options
    /// </summary>
    public StaticFileOptions? StaticFileOptions { get; set; }

    /// <summary>
    /// Custom file provider for embedded resources
    /// </summary>
    public IFileProvider? EmbeddedFileProvider { get; set; }

    /// <summary>
    /// Enable detailed errors
    /// </summary>
    public bool DetailedErrors { get; set; } = false;

    /// <summary>
    /// SignalR hub options
    /// </summary>
    public Action<Microsoft.AspNetCore.SignalR.HubOptions>? ConfigureSignalR { get; set; }

    /// <summary>
    /// Circuit options for Blazor Server
    /// </summary>
    public Action<Microsoft.AspNetCore.Components.Server.CircuitOptions>? ConfigureCircuitOptions { get; set; }

    /// <summary>
    /// JSInterop default timeout
    /// </summary>
    public TimeSpan? JSInteropDefaultCallTimeout { get; set; }

    /// <summary>
    /// Maximum message size for SignalR
    /// </summary>
    public long? MaximumReceiveMessageSize { get; set; }

    /// <summary>
    /// Disconnected circuit retention period
    /// </summary>
    public TimeSpan DisconnectedCircuitRetentionPeriod { get; set; } = TimeSpan.FromMinutes(Constants.Defaults.DisconnectedCircuitRetentionMinutes);

    /// <summary>
    /// Create default configuration
    /// </summary>
    public static BlazorHostConfiguration CreateDefault()
    {
        return new BlazorHostConfiguration
        {
            DetailedErrors =
#if DEBUG
                true,
#else
                false,
#endif
            JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(1),
            MaximumReceiveMessageSize = Constants.Defaults.MaximumReceiveMessageSizeBytes
        };
    }

    /// <summary>
    /// Apply configuration to the circuit options
    /// </summary>
    internal void ApplyToCircuitOptions(Microsoft.AspNetCore.Components.Server.CircuitOptions options)
    {
        options.DetailedErrors = DetailedErrors;
        options.DisconnectedCircuitRetentionPeriod = DisconnectedCircuitRetentionPeriod;

        if (JSInteropDefaultCallTimeout.HasValue)
        {
            options.JSInteropDefaultCallTimeout = JSInteropDefaultCallTimeout.Value;
        }

        if (MaximumReceiveMessageSize.HasValue)
        {
            //this doesn't exist in .NET 8, but is used in earlier versions
            //options.MaximumReceiveBufferSize = MaximumReceiveMessageSize.Value;
        }

        ConfigureCircuitOptions?.Invoke(options);
    }
}