using Microsoft.Extensions.Logging;
using CheapAvaloniaBlazor.Configuration;

namespace CheapAvaloniaBlazor.Services;

/// <summary>
/// Logger wrapper that automatically respects EnableDiagnostics flag for verbose output
/// </summary>
public class DiagnosticLogger
{
    private readonly ILogger _logger;
    private readonly CheapAvaloniaBlazorOptions _options;

    public DiagnosticLogger(ILogger logger, CheapAvaloniaBlazorOptions options)
    {
        _logger = logger;
        _options = options;
    }

    /// <summary>
    /// Log diagnostic information (only when EnableDiagnostics is true)
    /// </summary>
    public void LogDiagnostic(string message, params object?[] args)
    {
        if (_options.EnableDiagnostics)
        {
            _logger.LogDebug(message, args);
        }
    }

    /// <summary>
    /// Log verbose information (only when EnableDiagnostics is true)
    /// </summary>
    public void LogVerbose(string message, params object?[] args)
    {
        if (_options.EnableDiagnostics)
        {
            _logger.LogInformation(message, args);
        }
    }

    /// <summary>
    /// Log verbose diagnostic information with automatic prefix (only when EnableDiagnostics is true)
    /// </summary>
    public void LogDiagnosticVerbose(string message, params object?[] args)
    {
        if (_options.EnableDiagnostics)
        {
            _logger.LogInformation($"{Constants.Diagnostics.Prefix} {message}", args);
        }
    }

    /// <summary>
    /// Always log information (not affected by EnableDiagnostics)
    /// </summary>
    public void LogInformation(string message, params object?[] args)
    {
        _logger.LogInformation(message, args);
    }

    /// <summary>
    /// Always log warning (not affected by EnableDiagnostics)
    /// </summary>
    public void LogWarning(string message, params object?[] args)
    {
        _logger.LogWarning(message, args);
    }

    /// <summary>
    /// Always log error (not affected by EnableDiagnostics)
    /// </summary>
    public void LogError(Exception? exception, string message, params object?[] args)
    {
        _logger.LogError(exception, message, args);
    }

    /// <summary>
    /// Always log error (not affected by EnableDiagnostics)
    /// </summary>
    public void LogError(string message, params object?[] args)
    {
        _logger.LogError(message, args);
    }

    /// <summary>
    /// Check if diagnostics are enabled
    /// </summary>
    public bool DiagnosticsEnabled => _options.EnableDiagnostics;
}
