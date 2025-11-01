using Microsoft.Extensions.Logging;
using CheapAvaloniaBlazor.Configuration;

namespace CheapAvaloniaBlazor.Services;

/// <summary>
/// Implementation of DiagnosticLogger factory
/// </summary>
public class DiagnosticLoggerFactory : IDiagnosticLoggerFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly CheapAvaloniaBlazorOptions _options;

    public DiagnosticLoggerFactory(ILoggerFactory loggerFactory, CheapAvaloniaBlazorOptions options)
    {
        _loggerFactory = loggerFactory;
        _options = options;
    }

    public DiagnosticLogger CreateLogger<T>()
    {
        return new DiagnosticLogger(_loggerFactory.CreateLogger<T>(), _options);
    }

    public DiagnosticLogger CreateLogger(string categoryName)
    {
        return new DiagnosticLogger(_loggerFactory.CreateLogger(categoryName), _options);
    }
}
