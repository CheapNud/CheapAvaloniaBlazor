namespace CheapAvaloniaBlazor.Utilities;

/// <summary>
/// Factory for creating configured HttpClient instances
/// </summary>
public static class HttpClientFactory
{
    /// <summary>
    /// Create an HttpClient configured for server readiness checks
    /// </summary>
    public static HttpClient CreateForServerCheck()
    {
        return new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(Constants.Defaults.HttpClientTimeoutSeconds)
        };
    }
}
