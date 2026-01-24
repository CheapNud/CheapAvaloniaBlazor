using System.Diagnostics;
using System.Text.Json;
using DesktopFeatures.Models;

namespace DesktopFeatures.Services;

/// <summary>
/// Manages application settings persistence.
/// Settings are stored in %LocalAppData%\DesktopFeatures\settings.json
/// </summary>
public class SettingsService
{
    private static readonly string SettingsFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DesktopFeatures");

    private static readonly string SettingsPath = Path.Combine(SettingsFolder, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private AppSettings? _settings;
    private readonly SemaphoreSlim _saveLock = new(1, 1);

    /// <summary>
    /// Event fired when settings change
    /// </summary>
    public event Action? SettingsChanged;

    /// <summary>
    /// Current settings (lazy loaded)
    /// </summary>
    public AppSettings Settings => _settings ??= Load();

    /// <summary>
    /// Load settings from disk, or create defaults if not found
    /// </summary>
    private AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (settings is not null)
                {
                    Debug.WriteLine($"Settings loaded from {SettingsPath}");
                    return settings;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load settings: {ex.Message}");
        }

        Debug.WriteLine("Using default settings");
        return new AppSettings();
    }

    /// <summary>
    /// Save settings to disk
    /// </summary>
    public async Task SaveAsync()
    {
        await _saveLock.WaitAsync();
        try
        {
            // Ensure directory exists
            Directory.CreateDirectory(SettingsFolder);

            var json = JsonSerializer.Serialize(Settings, JsonOptions);
            await File.WriteAllTextAsync(SettingsPath, json);

            Debug.WriteLine($"Settings saved to {SettingsPath}");
            SettingsChanged?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
        finally
        {
            _saveLock.Release();
        }
    }

    /// <summary>
    /// Update a setting and save
    /// </summary>
    public async Task UpdateAsync(Action<AppSettings> update)
    {
        update(Settings);
        await SaveAsync();
    }
}
