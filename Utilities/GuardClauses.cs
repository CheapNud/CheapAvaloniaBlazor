using CheapAvaloniaBlazor.Services;
using Microsoft.Extensions.Logging;

namespace CheapAvaloniaBlazor.Utilities;

/// <summary>
/// Guard clause helpers for null checking and validation
/// </summary>
public static class GuardClauses
{
    /// <summary>
    /// Ensure all required services are not null
    /// </summary>
    /// <returns>True if all services are available, false otherwise</returns>
    public static bool RequireServices(DiagnosticLogger? logger, params object?[] services)
    {
        if (services.Any(s => s == null))
        {
            logger?.LogError("Required services not available");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Ensure all required services are not null
    /// </summary>
    /// <returns>True if all services are available, false otherwise</returns>
    public static bool RequireServices(ILogger? logger, params object?[] services)
    {
        if (services.Any(s => s == null))
        {
            logger?.LogError("Required services not available");
            return false;
        }
        return true;
    }
}
