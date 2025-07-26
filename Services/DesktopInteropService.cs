using CheapAvaloniaBlazor.Services;
using Microsoft.JSInterop;

public class DesktopInteropService : IDesktopInteropService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly PhotinoWindowManager _windowManager;

    public DesktopInteropService(IJSRuntime jsRuntime, PhotinoWindowManager windowManager)
    {
        _jsRuntime = jsRuntime;
        _windowManager = windowManager;
    }

    // File System Operations
    public async Task<string?> OpenFileDialogAsync(FileDialogOptions? options = null)
    {
        options ??= new FileDialogOptions();

        var result = await _windowManager.InvokeAsync<string[]?>(window =>
        {
            return window.ShowOpenFile(
                options.Title ?? "Open File",
                defaultPath: null, // Provide a default path as the second argument
                options.MultiSelect,
                options.Filters?.ToPhotinoFilters());
        });

        return result?.Length > 0 ? result[0] : null;
    }

    public async Task<string?> SaveFileDialogAsync(FileDialogOptions? options = null)
    {
        options ??= new FileDialogOptions();

        return await _windowManager.InvokeAsync<string?>(window =>
        {
            return window.ShowSaveFile(
                options.Title ?? "Save File",
                options.DefaultFileName,
                options.Filters?.ToPhotinoFilters());
        });
    }

    public async Task<string?> OpenFolderDialogAsync()
    {
        var result = await _windowManager.InvokeAsync<string?>(window =>
        {
            // Use ShowOpenFolder instead of the non-existent ShowSelectFolder
            var folders = window.ShowOpenFolder("Select Folder");
            return folders?.Length > 0 ? folders[0] : null;
        });

        return result;
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
        return _windowManager.InvokeAsync(window => window.SetMinimized(true));
    }

    public Task MaximizeWindowAsync()
    {
        return _windowManager.InvokeAsync(window => window.SetMaximized(true));
    }

    public Task RestoreWindowAsync()
    {
        return _windowManager.InvokeAsync(window => window.SetMaximized(true)); //Restore() not available in Photino
    }

    public Task SetWindowTitleAsync(string title)
    {
        return _windowManager.InvokeAsync(window => window.SetTitle(title));
    }

    public async Task<WindowState> GetWindowStateAsync()
    {
        var state = await _windowManager.InvokeAsync<string>(window =>
        {
            if (window.Maximized) return "maximized";
            if (window.Minimized) return "minimized";
            return "normal";
        });

        return state switch
        {
            "maximized" => WindowState.Maximized,
            "minimized" => WindowState.Minimized,
            _ => WindowState.Normal
        };
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
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
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


// Fix for CS1503: Convert the `Filters` property to the expected type `(string Name, string[] Extensions)[]`
// by adding a helper method `ToPhotinoFilters` to handle the conversion.
public static class FileDialogOptionsExtensions
{
    public static (string Name, string[] Extensions)[]? ToPhotinoFilters(this List<FileFilter>? filters)
    {
        return filters?.Select(filter => (filter.Name, filter.Extensions)).ToArray();
    }
}
