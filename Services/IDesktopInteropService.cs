using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

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


// Supporting classes
public class FileDialogOptions
{
    public string? Title { get; set; }
    public bool MultiSelect { get; set; }
    public string? DefaultFileName { get; set; }
    public List<FileFilter>? Filters { get; set; }
}

public class FileFilter
{
    public string Name { get; set; } = "";
    public string[] Extensions { get; set; } = Array.Empty<string>();
}

public enum WindowState
{
    Normal,
    Minimized,
    Maximized
}

// Extension methods
internal static class FileFilterExtensions
{
    public static string[][] ToPhotinoFilters(this List<FileFilter> filters)
    {
        return filters.Select(f => new[] { f.Name, string.Join(";", f.Extensions) }).ToArray();
    }
}