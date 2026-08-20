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
    private UpdateManager? _updateManager;
    private UpdateInfo? _pendingUpdate;

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

            _updateManager = updateManager;
            _pendingUpdate = updateInfo;
            UpdateReady = true;

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
    }

    public void ApplyAndRestart()
    {
        if (_updateManager is not null && _pendingUpdate is not null)
        {
            _updateManager.ApplyUpdatesAndRestart(_pendingUpdate.TargetFullRelease);
        }
    }

    private static IUpdateSource CreateSource(string repoUrl)
        => repoUrl.Contains("github.com", StringComparison.OrdinalIgnoreCase)
            ? new GithubSource(repoUrl, null, false)
            : new GiteaSource(repoUrl, null, false);
}
