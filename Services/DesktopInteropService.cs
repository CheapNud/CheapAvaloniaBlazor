using CheapAvaloniaBlazor.Models;
using CheapAvaloniaBlazor.Services;
using Microsoft.JSInterop;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia;
using Avalonia.Controls;

public class DesktopInteropService : IDesktopInteropService
{
    private readonly IJSRuntime _jsRuntime;

    public DesktopInteropService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    // File System Operations
    public async Task<string?> OpenFileDialogAsync(FileDialogOptions? options = null)
    {
        options ??= new FileDialogOptions();

        var topLevel = GetTopLevel();
        if (topLevel?.StorageProvider is not { } storage)
            return null;

        var fileTypes = options.Filters?.Select(f => new FilePickerFileType(f.Name)
        {
            Patterns = f.Extensions.ToArray()
        }).ToArray() ?? [];

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

        var topLevel = GetTopLevel();
        if (topLevel?.StorageProvider is not { } storage)
            return null;

        var fileTypes = options.Filters?.Select(f => new FilePickerFileType(f.Name)
        {
            Patterns = f.Extensions.ToArray()
        }).ToArray() ?? [];

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
        var topLevel = GetTopLevel();
        if (topLevel?.StorageProvider is not { } storage)
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

    public Task<bool> FileExistsAsync(string path)
    {
        return Task.FromResult(File.Exists(path));
    }

    // Window Operations
    public Task MinimizeWindowAsync()
    {
        var window = GetTopLevel();
        if (window != null)
        {
            window.WindowState = Avalonia.Controls.WindowState.Minimized;
        }
        return Task.CompletedTask;
    }

    public Task MaximizeWindowAsync()
    {
        var window = GetTopLevel();
        if (window != null)
        {
            window.WindowState = Avalonia.Controls.WindowState.Maximized;
        }
        return Task.CompletedTask;
    }

    public Task RestoreWindowAsync()
    {
        var window = GetTopLevel();
        if (window != null)
        {
            window.WindowState = Avalonia.Controls.WindowState.Normal;
        }
        return Task.CompletedTask;
    }

    public Task SetWindowTitleAsync(string title)
    {
        var window = GetTopLevel();
        if (window != null)
        {
            window.Title = title;
        }
        return Task.CompletedTask;
    }

    public Task<CheapAvaloniaBlazor.Models.WindowState> GetWindowStateAsync()
    {
        var window = GetTopLevel();
        if (window == null)
            return Task.FromResult(CheapAvaloniaBlazor.Models.WindowState.Normal);

        return Task.FromResult(window.WindowState switch
        {
            Avalonia.Controls.WindowState.Maximized => CheapAvaloniaBlazor.Models.WindowState.Maximized,
            Avalonia.Controls.WindowState.Minimized => CheapAvaloniaBlazor.Models.WindowState.Minimized,
            _ => CheapAvaloniaBlazor.Models.WindowState.Normal
        });
    }

    // System Operations
    public Task<string> GetAppDataPathAsync()
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CheapAvaloniaBlazor");

        Directory.CreateDirectory(path);
        return Task.FromResult(path);
    }

    public Task<string> GetDocumentsPathAsync()
    {
        return Task.FromResult(
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
            if (uri.Scheme != "http" && uri.Scheme != "https" && uri.Scheme != "mailto")
                throw new ArgumentException($"URL scheme '{uri.Scheme}' is not allowed. Only http, https, and mailto are supported.", nameof(url));

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
        await _jsRuntime.InvokeVoidAsync("cheapBlazor.showNotification", title, message);
    }

    // Clipboard Operations
    public async Task<string?> GetClipboardTextAsync()
    {
        return await _jsRuntime.InvokeAsync<string?>("cheapBlazor.getClipboardText");
    }

    public async Task SetClipboardTextAsync(string text)
    {
        await _jsRuntime.InvokeVoidAsync("cheapBlazor.setClipboardText", text);
    }
}
