using Microsoft.AspNetCore.Http;

namespace CheapAvaloniaBlazor.Models;

/// <summary>
/// Represents a custom endpoint
/// </summary>
public class CustomEndpoint
{
    public required string Pattern { get; init; }
    public required RequestDelegate Handler { get; init; }
}