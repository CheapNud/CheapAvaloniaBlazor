using CheapAvaloniaBlazor.Configuration;
using Microsoft.Extensions.Logging;
using Velopack;
using Velopack.Sources;

namespace CheapAvaloniaBlazor.Services;

/// <summary>
/// Velopack-backed <see cref="IUpdateService"/>. The release feed is the repo URL set via
/// WithVelopackUpdates() — a Gitea/Forgejo repo by default, GitHub when the URL says so.
/// </summary>
public sealed class UpdateService(CheapAvaloniaBlazorOptions options, ILogger<UpdateService> logger) : IUpdateService
{
    // State is written from the background check and read from Blazor circuits —
    // guard the trio so a reader never sees a half-published update.
    private readonly object _stateLock = new();
    private UpdateManager? _updateManager;
    private UpdateInfo? _pendingUpdate;
    private int _checkInProgress;

    public bool UpdateReady { get; private set; }

    public string? PendingVersion => _pendingUpdate?.TargetFullRelease?.Version?.ToString();

    public event Action? StateChanged;

    public async Task CheckAndDownloadAsync()
    {
        var repoUrl = options.UpdateRepoUrl;
        if (string.IsNullOrEmpty(repoUrl))
        {
            logger.LogDebug("UpdateService: no update repository configured, skipping check");
            return;
        }

        // Re-entrancy guard: a second check while one is running is a no-op
        if (Interlocked.CompareExchange(ref _checkInProgress, 1, 0) != 0)
        {
            logger.LogDebug("UpdateService: check already in progress, skipping");
            return;
        }

        try
        {
            var updateManager = new UpdateManager(CreateSource(repoUrl));

            // Portable zip or dev run — Velopack isn't managing this install
            if (!updateManager.IsInstalled)
            {
                logger.LogDebug("UpdateService: not a Velopack install, skipping update check");
                return;
            }

            var updateInfo = await updateManager.CheckForUpdatesAsync().ConfigureAwait(false);
            if (updateInfo is null)
            {
                logger.LogDebug("UpdateService: no update available");
                return;
            }

            logger.LogInformation("UpdateService: downloading update {Version}", updateInfo.TargetFullRelease?.Version);
            await updateManager.DownloadUpdatesAsync(updateInfo).ConfigureAwait(false);

            lock (_stateLock)
            {
                _updateManager = updateManager;
                _pendingUpdate = updateInfo;
                UpdateReady = true;
            }

            try
            {
                StateChanged?.Invoke();
            }
            catch (Exception handlerEx)
            {
                logger.LogError(handlerEx, "UpdateService: StateChanged handler threw");
            }
        }
        catch (Exception ex)
        {
            // Update checking is best-effort — never bother the user about it
            logger.LogDebug(ex, "UpdateService: update check failed");
        }
        finally
        {
            Volatile.Write(ref _checkInProgress, 0);
        }
    }

    public void ApplyAndRestart()
    {
        UpdateManager? updateManager;
        UpdateInfo? pendingUpdate;
        lock (_stateLock)
        {
            updateManager = _updateManager;
            pendingUpdate = _pendingUpdate;
        }

        if (updateManager is not null && pendingUpdate?.TargetFullRelease is not null)
        {
            updateManager.ApplyUpdatesAndRestart(pendingUpdate.TargetFullRelease);
        }
    }

    private static IUpdateSource CreateSource(string repoUrl)
    {
        // Host-based detection, not substring: a forge repo named "github.com-mirror"
        // must not be mistaken for GitHub.
        var host = new Uri(repoUrl).Host;
        var isGitHub = host.Equals("github.com", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".github.com", StringComparison.OrdinalIgnoreCase);

        return isGitHub
            ? new GithubSource(repoUrl, null, false)
            : new GiteaSource(repoUrl, null, false);
    }
}
