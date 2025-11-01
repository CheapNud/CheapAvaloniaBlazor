namespace CheapAvaloniaBlazor.Services;

/// <summary>
/// Factory for creating DiagnosticLogger instances
/// </summary>
public interface IDiagnosticLoggerFactory
{
    DiagnosticLogger CreateLogger<T>();
    DiagnosticLogger CreateLogger(string categoryName);
}
