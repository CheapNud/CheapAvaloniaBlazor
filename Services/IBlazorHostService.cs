using System.Threading;
using System.Threading.Tasks;

namespace CheapAvaloniaBlazor.Services;

public interface IBlazorHostService
{
    Task<string> StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    bool IsRunning { get; }
    string BaseUrl { get; }
}