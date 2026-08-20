namespace CheapAvaloniaBlazor.Services;

/// <summary>
/// Velopack auto-update: checks the configured release feed for a newer version,
/// downloads it in the background, and applies it on restart.
/// No-ops when the app runs portable (zip/dev) instead of Velopack-installed,
/// or when no update repository is configured via WithVelopackUpdates().
/// </summary>
public interface IUpdateService
{
    /// <summary>An update is downloaded and ready; restart applies it.</summary>
    bool UpdateReady { get; }

    /// <summary>Version string of the downloaded update, if any.</summary>
    string? PendingVersion { get; }

    /// <summary>Raised when UpdateReady changes (from a background thread — marshal UI work yourself).</summary>
    event Action? StateChanged;

    /// <summary>
    /// Check the release feed and download a newer version if one exists.
    /// Best-effort: failures are logged, never thrown.
    /// </summary>
    Task CheckAndDownloadAsync();

    /// <summary>Apply the downloaded update and restart the app. No-op when nothing is pending.</summary>
    void ApplyAndRestart();
}
