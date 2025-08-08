using Microsoft.AspNetCore.Http;

namespace CheapAvaloniaBlazor.Models;

/// <summary>
/// Options for Blazor endpoint configuration
/// </summary>
public class BlazorEndpointOptions
{
    /// <summary>
    /// Enable health check endpoint
    /// </summary>
    public bool EnableHealthCheck { get; set; } = true;

    /// <summary>
    /// Health check endpoint path
    /// </summary>
    public string HealthCheckPath { get; set; } = "/health";

    /// <summary>
    /// Enable version endpoint
    /// </summary>
    public bool EnableVersionEndpoint { get; set; } = true;

    /// <summary>
    /// Version endpoint path
    /// </summary>
    public string VersionPath { get; set; } = "/version";

    /// <summary>
    /// Custom endpoints to map
    /// </summary>
    public List<CustomEndpoint> CustomEndpoints { get; } = new();

    /// <summary>
    /// Add a custom endpoint
    /// </summary>
    public void AddEndpoint(string pattern, RequestDelegate handler)
    {
        CustomEndpoints.Add(new CustomEndpoint { Pattern = pattern, Handler = handler });
    }
}
