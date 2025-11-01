using CheapAvaloniaBlazor.Models;
using CheapAvaloniaBlazor.Services;
using Microsoft.JSInterop;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia;
using Avalonia.Controls;
using System.Text.Json;
using CheapAvaloniaBlazor;

public class DesktopInteropService : IDesktopInteropService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly DiagnosticLogger _logger;

    public DesktopInteropService(IJSRuntime jsRuntime, IDiagnosticLoggerFactory loggerFactory)
    {
        _jsRuntime = jsRuntime;
        _logger = loggerFactory.CreateLogger<DesktopInteropService>();
    }

    // File System Operations
    public async Task<string?> OpenFileDialogAsync(FileDialogOptions? options = null)
    {
        options ??= new FileDialogOptions();

        if (GetStorageProvider() is not { } storage)
            return null;

        var fileTypes = ConvertToFilePickerTypes(options);

        var result = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = options.Title ?? "Open File",
            AllowMultiple = options.MultiSelect,
            FileTypeFilter = fileTypes
        });

        return result.FirstOrDefault()?.Path.LocalPath;
    }

    public async Task<string?> SaveFileDialogAsync(FileDialogOptions? options = null)
    {
        options ??= new FileDialogOptions();

        if (GetStorageProvider() is not { } storage)
            return null;

        var fileTypes = ConvertToFilePickerTypes(options);

        var result = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = options.Title ?? "Save File",
            SuggestedFileName = options.DefaultFileName,
            FileTypeChoices = fileTypes
        });

        return result?.Path.LocalPath;
    }

    public async Task<string?> OpenFolderDialogAsync()
    {
        if (GetStorageProvider() is not { } storage)
            return null;

        var result = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Folder",
            AllowMultiple = false
        });

        return result.FirstOrDefault()?.Path.LocalPath;
    }

    private Window? GetTopLevel()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }
        return null;
    }

    public Task<byte[]> ReadFileAsync(string path)
    {
        return Task.Run(() => File.ReadAllBytes(path));
    }

    public Task WriteFileAsync(string path, byte[] data)
    {
        return Task.Run(() => File.WriteAllBytes(path, data));
    }

    public ValueTask<bool> FileExistsAsync(string path)
    {
        return new ValueTask<bool>(File.Exists(path));
    }

    // Window Operations
    public ValueTask MinimizeWindowAsync()
    {
        var window = GetTopLevel();
        if (window != null)
        {
            window.WindowState = Avalonia.Controls.WindowState.Minimized;
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask MaximizeWindowAsync()
    {
        var window = GetTopLevel();
        if (window != null)
        {
            window.WindowState = Avalonia.Controls.WindowState.Maximized;
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask RestoreWindowAsync()
    {
        var window = GetTopLevel();
        if (window != null)
        {
            window.WindowState = Avalonia.Controls.WindowState.Normal;
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask SetWindowTitleAsync(string title)
    {
        var window = GetTopLevel();
        if (window != null)
        {
            window.Title = title;
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask<CheapAvaloniaBlazor.Models.WindowState> GetWindowStateAsync()
    {
        var window = GetTopLevel();
        if (window == null)
            return new ValueTask<CheapAvaloniaBlazor.Models.WindowState>(CheapAvaloniaBlazor.Models.WindowState.Normal);

        return new ValueTask<CheapAvaloniaBlazor.Models.WindowState>(window.WindowState switch
        {
            Avalonia.Controls.WindowState.Maximized => CheapAvaloniaBlazor.Models.WindowState.Maximized,
            Avalonia.Controls.WindowState.Minimized => CheapAvaloniaBlazor.Models.WindowState.Minimized,
            _ => CheapAvaloniaBlazor.Models.WindowState.Normal
        });
    }

    // System Operations
    public ValueTask<string> GetAppDataPathAsync()
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Constants.Defaults.AppDataFolderName);

        Directory.CreateDirectory(path);
        return new ValueTask<string>(path);
    }

    public ValueTask<string> GetDocumentsPathAsync()
    {
        return new ValueTask<string>(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
    }

    public Task OpenUrlInBrowserAsync(string url)
    {
        return Task.Run(() =>
        {
            // Security fix: Validate URL to prevent command injection
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL cannot be null or empty", nameof(url));

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                throw new ArgumentException("Invalid URL format", nameof(url));

            // Only allow http, https, and mailto schemes
            if (!Constants.Security.AllowedUrlSchemes.Contains(uri.Scheme))
                throw new ArgumentException($"URL scheme '{uri.Scheme}' is not allowed. Only {string.Join(", ", Constants.Security.AllowedUrlSchemes)} are supported.", nameof(url));

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = uri.ToString(),
                UseShellExecute = true
            });
        });
    }

    public async Task ShowNotificationAsync(string title, string message)
    {
        // Use Photino's notification API if available, otherwise use JS
        await _jsRuntime.InvokeVoidAsync(Constants.JavaScript.ShowNotificationMethod, title, message);
    }

    // Clipboard Operations
    public async Task<string?> GetClipboardTextAsync()
    {
        return await _jsRuntime.InvokeAsync<string?>(Constants.JavaScript.GetClipboardTextMethod);
    }

    public async Task SetClipboardTextAsync(string text)
    {
        await _jsRuntime.InvokeVoidAsync(Constants.JavaScript.SetClipboardTextMethod, text);
    }

    // JavaScript Bridge Initialization
    public async Task InitializeJavaScriptBridgeAsync()
    {
        var objRef = DotNetObjectReference.Create(this);
        await _jsRuntime.InvokeVoidAsync(Constants.JavaScript.EvalFunction,
            $"window.{Constants.JavaScript.CheapBlazorInteropService} = arguments[0];", objRef);
    }

    // File Drop Operations
    public event Action<object[]>? OnFilesDroppedEvent;

    [JSInvokable]
    public Task OnFilesDropped(JsonElement[] files)
    {
        try
        {
            var fileInfos = files.Select(f => new
            {
                Name = f.TryGetProperty("name", out var name) ? name.GetString() : "",
                Size = f.TryGetProperty("size", out var size) ? size.GetInt64() : 0,
                Type = f.TryGetProperty("type", out var type) ? type.GetString() : "",
                LastModified = f.TryGetProperty("lastModified", out var lastMod) ? lastMod.GetInt64() : 0
            }).ToArray();

            OnFilesDroppedEvent?.Invoke(fileInfos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling dropped files: {ErrorMessage}", ex.Message);
        }

        return Task.CompletedTask;
    }

    // Helper Methods
    private IStorageProvider? GetStorageProvider()
    {
        return GetTopLevel()?.StorageProvider;
    }

    private static FilePickerFileType[] ConvertToFilePickerTypes(FileDialogOptions? options)
    {
        return options?.Filters?.Select(f => new FilePickerFileType(f.Name)
        {
            Patterns = f.Extensions.Select(ext =>
                ext.StartsWith("*.") ? ext : $"*.{ext.TrimStart('*', '.')}"
            ).ToArray()
        }).ToArray() ?? Array.Empty<FilePickerFileType>();
    }
}
