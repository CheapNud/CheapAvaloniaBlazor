using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using CheapAvaloniaBlazor.Configuration;
using Microsoft.Extensions.Logging;

namespace CheapAvaloniaBlazor.Services;

/// <summary>
/// JSON-based settings persistence service.
/// Thread-safe via SemaphoreSlim, lazy-loaded on first access, optional auto-save.
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly CheapAvaloniaBlazorOptions _options;
    private readonly ILogger<SettingsService>? _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly string _settingsFilePath;

    private JsonObject? _root;
    private bool _disposed;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public event Action? SettingsChanged;

    public SettingsService(CheapAvaloniaBlazorOptions options, ILogger<SettingsService>? logger = null)
    {
        _options = options;
        _logger = logger;
        _settingsFilePath = ResolveSettingsPath();
        Debug.WriteLine($"SettingsService: file path = {_settingsFilePath}");
    }

    // ───────────────────────────── Key-value API ─────────────────────────────

    public async Task<T?> GetAsync<T>(string key, T? defaultValue = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        await EnsureLoadedAsync();

        await _semaphore.WaitAsync();
        try
        {
            if (_root!.TryGetPropertyValue(key, out var node) && node is not null)
            {
                return node.Deserialize<T>(SerializerOptions);
            }
            return defaultValue;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task SetAsync<T>(string key, T settingValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(settingValue);
        await EnsureLoadedAsync();

        await _semaphore.WaitAsync();
        try
        {
            _root![key] = JsonSerializer.SerializeToNode(settingValue, SerializerOptions);
        }
        finally
        {
            _semaphore.Release();
        }

        if (_options.AutoSaveSettings)
            await SaveAsync();
    }

    public async Task<bool> DeleteAsync(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        await EnsureLoadedAsync();

        bool removed;
        await _semaphore.WaitAsync();
        try
        {
            removed = _root!.Remove(key);
        }
        finally
        {
            _semaphore.Release();
        }

        if (removed && _options.AutoSaveSettings)
            await SaveAsync();

        return removed;
    }

    public async Task<bool> ExistsAsync(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        await EnsureLoadedAsync();

        await _semaphore.WaitAsync();
        try
        {
            return _root!.ContainsKey(key);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    // ───────────────────────────── Typed section API ─────────────────────────

    public async Task<T> GetSectionAsync<T>(T? defaultValue = default) where T : class, new()
    {
        var sectionKey = typeof(T).Name;
        await EnsureLoadedAsync();

        await _semaphore.WaitAsync();
        try
        {
            if (_root!.TryGetPropertyValue(sectionKey, out var node) && node is not null)
            {
                var section = node.Deserialize<T>(SerializerOptions);
                if (section is not null)
                    return section;
            }
            return defaultValue ?? new T();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task SetSectionAsync<T>(T settingValue) where T : class
    {
        ArgumentNullException.ThrowIfNull(settingValue);
        var sectionKey = typeof(T).Name;
        await EnsureLoadedAsync();

        await _semaphore.WaitAsync();
        try
        {
            _root![sectionKey] = JsonSerializer.SerializeToNode(settingValue, SerializerOptions);
        }
        finally
        {
            _semaphore.Release();
        }

        if (_options.AutoSaveSettings)
            await SaveAsync();
    }

    public async Task UpdateSectionAsync<T>(Action<T> updateAction) where T : class, new()
    {
        ArgumentNullException.ThrowIfNull(updateAction);

        var section = await GetSectionAsync<T>();
        updateAction(section);
        await SetSectionAsync(section);
    }

    // ───────────────────────────── Utility ────────────────────────────────────

    public async Task SaveAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_root is null) return;

            var directory = Path.GetDirectoryName(_settingsFilePath)!;
            Directory.CreateDirectory(directory);

            var json = _root.ToJsonString(SerializerOptions);
            await File.WriteAllTextAsync(_settingsFilePath, json);

            Debug.WriteLine($"SettingsService: saved to {_settingsFilePath}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save settings to {Path}", _settingsFilePath);
            Debug.WriteLine($"SettingsService: save failed - {ex.Message}");
        }
        finally
        {
            _semaphore.Release();
        }

        SettingsChanged?.Invoke();
    }

    public async Task ReloadAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            _root = await LoadFromDiskAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    // ───────────────────────────── Internal ───────────────────────────────────

    private async Task EnsureLoadedAsync()
    {
        if (_root is not null) return;

        await _semaphore.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            _root ??= await LoadFromDiskAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<JsonObject> LoadFromDiskAsync()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = await File.ReadAllTextAsync(_settingsFilePath);
                var parsed = JsonNode.Parse(json);
                if (parsed is JsonObject jsonObject)
                {
                    Debug.WriteLine($"SettingsService: loaded from {_settingsFilePath}");
                    return jsonObject;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to load settings from {Path}, starting fresh", _settingsFilePath);
            Debug.WriteLine($"SettingsService: load failed - {ex.Message}");
        }

        Debug.WriteLine("SettingsService: using empty settings");
        return [];
    }

    private string ResolveSettingsPath()
    {
        // Full path override takes precedence
        if (!string.IsNullOrWhiteSpace(_options.SettingsFolder))
        {
            return Path.Combine(_options.SettingsFolder, _options.SettingsFileName);
        }

        // Build from AppData + sanitized app name
        var appName = _options.SettingsAppName ?? _options.DefaultWindowTitle;
        var sanitized = SanitizeFolderName(appName);
        var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        return Path.Combine(appDataFolder, sanitized, _options.SettingsFileName);
    }

    private static string SanitizeFolderName(string folderName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(folderName
            .Select(c => invalidChars.Contains(c) ? '_' : c)
            .ToArray());

        return string.IsNullOrWhiteSpace(sanitized) ? "App" : sanitized;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _semaphore.Dispose();
    }
}
