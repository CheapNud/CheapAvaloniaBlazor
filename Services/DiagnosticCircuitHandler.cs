using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.Logging;

namespace CheapAvaloniaBlazor.Services;

/// <summary>
/// Circuit handler that logs circuit lifecycle events for debugging embedded Blazor scenarios.
/// </summary>
internal sealed class DiagnosticCircuitHandler(ILogger logger) : CircuitHandler
{
    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        logger.LogWarning("[CIRCUIT] Circuit OPENED: {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        logger.LogWarning("[CIRCUIT] Connection UP: {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        logger.LogWarning("[CIRCUIT] Connection DOWN: {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        logger.LogWarning("[CIRCUIT] Circuit CLOSED: {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }
}
