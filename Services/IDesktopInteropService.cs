using CheapAvaloniaBlazor.Models;

namespace CheapAvaloniaBlazor.Services;

/// <summary>
/// Service that provides desktop functionality to Blazor components
/// </summary>
public interface IDesktopInteropService
{
    // File System Operations
    Task<string?> OpenFileDialogAsync(FileDialogOptions? options = null);
    Task<string?> SaveFileDialogAsync(FileDialogOptions? options = null);
    Task<string?> OpenFolderDialogAsync();
    Task<byte[]> ReadFileAsync(string path);
    Task WriteFileAsync(string path, byte[] data);
    Task<bool> FileExistsAsync(string path);

    // Window Operations
    Task MinimizeWindowAsync();
    Task MaximizeWindowAsync();
    Task RestoreWindowAsync();
    Task SetWindowTitleAsync(string title);
    Task<WindowState> GetWindowStateAsync();

    // System Operations
    Task<string> GetAppDataPathAsync();
    Task<string> GetDocumentsPathAsync();
    Task OpenUrlInBrowserAsync(string url);
    Task ShowNotificationAsync(string title, string message);

    // Clipboard Operations
    Task<string?> GetClipboardTextAsync();
    Task SetClipboardTextAsync(string text);
}
