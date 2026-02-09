# Desktop Interop API

## Overview

The `IDesktopInteropService` is a bridge that provides Blazor components with direct access to desktop functionality in an Avalonia-based desktop application. This service enables your web-based UI to interact with native window management, file system operations, system paths, clipboard, and browser integration.

**Key Characteristics:**
- Injected as a dependency in Blazor components via `@inject IDesktopInteropService Desktop`
- Provides both `Task<T>` and `ValueTask<T>` return types for optimized performance
- Security-hardened URL validation and file path handling
- Works with Photino's native notification API and JavaScript interop

---

## File System Operations

### OpenFileDialogAsync

Opens a native file picker dialog and returns the selected file path.

**Signature:**
```csharp
Task<string?> OpenFileDialogAsync(FileDialogOptions? options = null)
```

**Parameters:**
- `options` - Optional configuration for the file dialog

**Returns:**
- `string?` - Full path to the selected file, or `null` if cancelled

**Example:**
```csharp
@inject IDesktopInteropService Desktop

var filePath = await Desktop.OpenFileDialogAsync(new FileDialogOptions
{
    Title = "Select a Document",
    MultiSelect = false,
    Filters = new List<FileFilter>
    {
        new FileFilter
        {
            Name = "Text Files",
            Extensions = new[] { "*.txt", "*.md" }
        },
        new FileFilter
        {
            Name = "All Files",
            Extensions = new[] { "*.*" }
        }
    }
});

if (!string.IsNullOrEmpty(filePath))
{
    // Use filePath
}
```

**Notes:**
- Returns `null` if the user cancels the dialog
- File extensions are automatically normalized to `*.extension` format
- Returns only the first selected file (use OpenFolderDialogAsync for directories)

---

### SaveFileDialogAsync

Opens a native save dialog and returns the selected file path.

**Signature:**
```csharp
Task<string?> SaveFileDialogAsync(FileDialogOptions? options = null)
```

**Parameters:**
- `options` - Optional configuration for the save dialog

**Returns:**
- `string?` - Full path where the file should be saved, or `null` if cancelled

**Example:**
```csharp
var savePath = await Desktop.SaveFileDialogAsync(new FileDialogOptions
{
    Title = "Save Document As",
    DefaultFileName = "document.txt",
    Filters = new List<FileFilter>
    {
        new FileFilter
        {
            Name = "Text Documents",
            Extensions = new[] { "txt" }
        }
    }
});

if (!string.IsNullOrEmpty(savePath))
{
    var data = Encoding.UTF8.GetBytes("File content");
    await Desktop.WriteFileAsync(savePath, data);
}
```

**Notes:**
- Does not automatically add file extensions - include in `DefaultFileName` if desired
- Returns `null` if the user cancels the dialog
- Use with `WriteFileAsync` to persist the file

---

### OpenFolderDialogAsync

Opens a native folder/directory picker dialog.

**Signature:**
```csharp
Task<string?> OpenFolderDialogAsync()
```

**Parameters:**
- None

**Returns:**
- `string?` - Full path to the selected folder, or `null` if cancelled

**Example:**
```csharp
var folderPath = await Desktop.OpenFolderDialogAsync();

if (!string.IsNullOrEmpty(folderPath))
{
    var files = Directory.GetFiles(folderPath);
    // Process files
}
```

**Notes:**
- Single selection only (non-configurable)
- Returns `null` if the user cancels the dialog

---

### ReadFileAsync

Asynchronously reads the complete contents of a file as bytes.

**Signature:**
```csharp
Task<byte[]> ReadFileAsync(string path)
```

**Parameters:**
- `path` - Full file path to read

**Returns:**
- `byte[]` - Complete file contents

**Example:**
```csharp
// Read binary file
var imageBytes = await Desktop.ReadFileAsync("image.png");

// Read text file
var textContent = Encoding.UTF8.GetString(
    await Desktop.ReadFileAsync("document.txt")
);

// Read JSON file
var jsonData = JsonSerializer.Deserialize<MyModel>(
    await Desktop.ReadFileAsync("config.json")
);
```

**Notes:**
- Runs on a thread pool to avoid blocking
- Returns entire file contents in memory
- Not recommended for very large files
- Throws `FileNotFoundException` if file doesn't exist

---

### WriteFileAsync

Asynchronously writes byte data to a file, creating or overwriting it.

**Signature:**
```csharp
Task WriteFileAsync(string path, byte[] data)
```

**Parameters:**
- `path` - Full file path to write to
- `data` - Byte array containing the file contents

**Returns:**
- `Task` - Completes when write is finished

**Example:**
```csharp
// Write text file
var content = "Hello, Desktop!";
await Desktop.WriteFileAsync("output.txt", Encoding.UTF8.GetBytes(content));

// Write JSON file
var settings = new { theme = "dark", language = "en" };
var json = JsonSerializer.Serialize(settings);
await Desktop.WriteFileAsync("config.json", Encoding.UTF8.GetBytes(json));

// Copy file contents
var original = await Desktop.ReadFileAsync("source.txt");
await Desktop.WriteFileAsync("backup.txt", original);
```

**Notes:**
- Runs on a thread pool to avoid blocking
- Creates directories if they don't exist (you must create parent directories yourself)
- Overwrites existing files without warning
- Requires appropriate file system permissions

---

### FileExistsAsync

Checks whether a file exists at the specified path.

**Signature:**
```csharp
ValueTask<bool> FileExistsAsync(string path)
```

**Parameters:**
- `path` - Full file path to check

**Returns:**
- `ValueTask<bool>` - `true` if file exists, `false` otherwise

**Example:**
```csharp
if (await Desktop.FileExistsAsync("config.json"))
{
    var config = await Desktop.ReadFileAsync("config.json");
    // Load configuration
}
else
{
    // Initialize default configuration
}
```

**Notes:**
- Uses `ValueTask` optimization (no allocation if not awaited)
- Synchronous operation with minimal overhead
- Returns `false` for directories (checks files only)

---

## Window Management

### MinimizeWindowAsync

Minimizes the main application window.

**Signature:**
```csharp
ValueTask MinimizeWindowAsync()
```

**Parameters:**
- None

**Returns:**
- `ValueTask` - Completes when window state changes

**Example:**
```csharp
<MudButton OnClick="@(async () => await Desktop.MinimizeWindowAsync())">
    Minimize
</MudButton>
```

**Notes:**
- ValueTask optimization for minimal overhead
- Silently fails if no main window is available

---

### MaximizeWindowAsync

Maximizes the main application window.

**Signature:**
```csharp
ValueTask MaximizeWindowAsync()
```

**Parameters:**
- None

**Returns:**
- `ValueTask` - Completes when window state changes

**Example:**
```csharp
<MudButton OnClick="@(async () => await Desktop.MaximizeWindowAsync())">
    Maximize
</MudButton>
```

**Notes:**
- ValueTask optimization for minimal overhead
- Silently fails if no main window is available

---

### RestoreWindowAsync

Restores the window to normal (non-minimized, non-maximized) state.

**Signature:**
```csharp
ValueTask RestoreWindowAsync()
```

**Parameters:**
- None

**Returns:**
- `ValueTask` - Completes when window state changes

**Example:**
```csharp
<MudButton OnClick="@(async () => await Desktop.RestoreWindowAsync())">
    Restore
</MudButton>
```

**Notes:**
- ValueTask optimization for minimal overhead
- Silently fails if no main window is available

---

### SetWindowTitleAsync

Changes the main application window's title.

**Signature:**
```csharp
ValueTask SetWindowTitleAsync(string title)
```

**Parameters:**
- `title` - New window title text

**Returns:**
- `ValueTask` - Completes when title is updated

**Example:**
```csharp
await Desktop.SetWindowTitleAsync("My App - Editing: document.txt");
```

**Notes:**
- ValueTask optimization for minimal overhead
- Silently fails if no main window is available
- Useful for displaying document name or status in window title

---

### GetWindowStateAsync

Retrieves the current state of the main application window.

**Signature:**
```csharp
ValueTask<WindowState> GetWindowStateAsync()
```

**Parameters:**
- None

**Returns:**
- `ValueTask<WindowState>` - Current window state enum

**WindowState Values:**
- `Normal` - Regular window size
- `Minimized` - Window is minimized
- `Maximized` - Window is maximized

**Example:**
```csharp
var state = await Desktop.GetWindowStateAsync();

switch (state)
{
    case WindowState.Normal:
        Debug.WriteLine("Window is in normal state");
        break;
    case WindowState.Minimized:
        Debug.WriteLine("Window is minimized");
        break;
    case WindowState.Maximized:
        Debug.WriteLine("Window is maximized");
        break;
}
```

**Notes:**
- ValueTask optimization for minimal overhead
- Returns `WindowState.Normal` if no main window is available
- Useful for conditional UI rendering based on window state

---

## System Integration

### GetAppDataPathAsync

Retrieves the application data directory, creating it if necessary.

**Signature:**
```csharp
ValueTask<string> GetAppDataPathAsync()
```

**Parameters:**
- None

**Returns:**
- `ValueTask<string>` - Full path to application data directory

**Example:**
```csharp
var appDataPath = await Desktop.GetAppDataPathAsync();
var settingsFile = Path.Combine(appDataPath, "settings.json");

// Use for saving user preferences, cached data, etc.
var settings = new { theme = "dark" };
await Desktop.WriteFileAsync(
    settingsFile,
    Encoding.UTF8.GetBytes(JsonSerializer.Serialize(settings))
);
```

**Notes:**
- ValueTask optimization for minimal overhead
- Automatically creates directory if it doesn't exist
- Uses `Environment.SpecialFolder.ApplicationData`
- Platform-specific locations:
  - Windows: `C:\Users\[User]\AppData\Roaming\[AppDataFolderName]`
  - Linux: `~/.config/[AppDataFolderName]`
  - macOS: `~/Library/Application Support/[AppDataFolderName]`
- Directory name comes from `Constants.Defaults.AppDataFolderName`

---

### GetDocumentsPathAsync

Retrieves the user's Documents directory.

**Signature:**
```csharp
ValueTask<string> GetDocumentsPathAsync()
```

**Parameters:**
- None

**Returns:**
- `ValueTask<string>` - Full path to Documents directory

**Example:**
```csharp
var documentsPath = await Desktop.GetDocumentsPathAsync();
var defaultSaveLocation = Path.Combine(documentsPath, "MyApp");

// Use as default save location
var savePath = await Desktop.SaveFileDialogAsync(new FileDialogOptions
{
    Title = "Save Document",
    DefaultFileName = Path.Combine(defaultSaveLocation, "document.txt")
});
```

**Notes:**
- ValueTask optimization for minimal overhead
- Platform-specific locations:
  - Windows: `C:\Users\[User]\Documents`
  - Linux: `~/Documents`
  - macOS: `~/Documents`

---

### OpenUrlInBrowserAsync

Opens a URL in the system's default web browser.

**Signature:**
```csharp
Task OpenUrlInBrowserAsync(string url)
```

**Parameters:**
- `url` - Complete URL to open (http, https, or mailto)

**Returns:**
- `Task` - Completes when browser is opened

**Example:**
```csharp
// Open website
await Desktop.OpenUrlInBrowserAsync("https://github.com");

// Open mailto link
await Desktop.OpenUrlInBrowserAsync("mailto:user@example.com?subject=Hello");

// Open with error handling
try
{
    await Desktop.OpenUrlInBrowserAsync("https://example.com");
}
catch (ArgumentException ex)
{
    // Handle invalid URL or unsupported scheme
    Debug.WriteLine($"URL error: {ex.Message}");
}
```

**Supported URL Schemes:**
- `http://`
- `https://`
- `mailto:`

**Throws:**
- `ArgumentException` - If URL is null, empty, invalid format, or uses unsupported scheme

**Notes:**
- Security-hardened: validates URL format and scheme before opening
- Runs on a thread pool
- Uses `UseShellExecute = true` for cross-platform browser launching
- Prevents command injection attacks through strict validation

---

### ShowNotificationAsync

Displays a system notification.

**Signature:**
```csharp
Task ShowNotificationAsync(string title, string message)
```

**Parameters:**
- `title` - Notification title
- `message` - Notification message body

**Returns:**
- `Task` - Completes when notification is shown

**Example:**
```csharp
await Desktop.ShowNotificationAsync(
    "File Saved",
    "Your document has been saved successfully."
);

await Desktop.ShowNotificationAsync(
    "Warning",
    "This action cannot be undone."
);
```

**Notes:**
- Uses Photino's native notification API or JavaScript fallback
- Platform-specific notification appearance
- Async call completes immediately; notification may display after

---

## Clipboard Operations

### GetClipboardTextAsync

Retrieves the current text content from the system clipboard.

**Signature:**
```csharp
Task<string?> GetClipboardTextAsync()
```

**Parameters:**
- None

**Returns:**
- `Task<string?>` - Clipboard text content, or `null` if empty

**Example:**
```csharp
var clipboardContent = await Desktop.GetClipboardTextAsync();

if (!string.IsNullOrEmpty(clipboardContent))
{
    Debug.WriteLine($"Clipboard: {clipboardContent}");
}
```

**Notes:**
- Returns `null` if clipboard is empty
- Only retrieves text (binary clipboard data not supported)
- Requires appropriate system permissions

---

### SetClipboardTextAsync

Copies text to the system clipboard.

**Signature:**
```csharp
Task SetClipboardTextAsync(string text)
```

**Parameters:**
- `text` - Text to copy to clipboard

**Returns:**
- `Task` - Completes when text is copied

**Example:**
```csharp
<MudButton OnClick="@(async () => {
    await Desktop.SetClipboardTextAsync(\"Copied text!\");
    await Desktop.ShowNotificationAsync(\"Copied\", \"Text copied to clipboard\");
})">
    Copy to Clipboard
</MudButton>
```

**Notes:**
- Only supports text content
- Overwrites previous clipboard contents
- Completes immediately; system may buffer the operation

---

## File Dialog Configuration

### FileDialogOptions

Configuration class for file and folder picker dialogs.

**Properties:**

| Property | Type | Description |
|----------|------|-------------|
| `Title` | `string?` | Dialog window title. Defaults to "Open File" or "Save File" if not specified. |
| `MultiSelect` | `bool` | Whether multiple files can be selected (OpenFileDialogAsync only). Default: `false`. Note: Only first selection is returned. |
| `DefaultFileName` | `string?` | Suggested filename for save dialogs. Not added automatically - include extension if needed. |
| `Filters` | `List<FileFilter>?` | List of file type filters to display in the dialog. |

**Example:**
```csharp
var options = new FileDialogOptions
{
    Title = "Select Image Files",
    DefaultFileName = "image.png",
    Filters = new List<FileFilter>
    {
        new FileFilter
        {
            Name = "Image Files",
            Extensions = new[] { "*.jpg", "*.png", "*.gif" }
        },
        new FileFilter
        {
            Name = "All Files",
            Extensions = new[] { "*.*" }
        }
    }
};

var selectedFile = await Desktop.OpenFileDialogAsync(options);
```

---

### FileFilter

Represents a file type filter in file dialogs.

**Properties:**

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Display name for this filter (e.g., "Image Files", "Text Documents"). |
| `Extensions` | `string[]` | Array of file extensions (e.g., `["*.jpg", "*.png"]`). Automatically normalized to `*.extension` format. |

**Example:**
```csharp
var filters = new List<FileFilter>
{
    new FileFilter
    {
        Name = "C# Files",
        Extensions = new[] { "cs", "csx" }  // Both formats accepted
    },
    new FileFilter
    {
        Name = "Text Files",
        Extensions = new[] { "*.txt", "*.md" }  // Both formats accepted
    },
    new FileFilter
    {
        Name = "All Files",
        Extensions = new[] { "*.*" }
    }
};
```

**Notes:**
- Extensions are automatically normalized (don't worry about `*.` prefix)
- Can use either `"txt"` or `"*.txt"` - both are handled identically
- Extension comparison is case-insensitive

---

## Complete Examples

### File Manager Component

A complete component demonstrating file operations:

```csharp
@page "/file-manager"
@inject IDesktopInteropService Desktop
@using System.Text

<MudCard>
    <MudCardHeader>
        <MudText Typo="Typo.h5">File Manager</MudText>
    </MudCardHeader>
    <MudCardContent>
        <MudStack>
            <MudButtonGroup>
                <MudButton Variant="Variant.Filled"
                    OnClick="@OpenFile">
                    Open File
                </MudButton>
                <MudButton Variant="Variant.Filled"
                    OnClick="@SaveFile">
                    Save File
                </MudButton>
                <MudButton Variant="Variant.Filled"
                    OnClick="@SelectFolder">
                    Select Folder
                </MudButton>
            </MudButtonGroup>

            @if (!string.IsNullOrEmpty(currentFile))
            {
                <MudPaper Class="pa-4">
                    <MudText Typo="Typo.body1">
                        <strong>Current File:</strong> @currentFile
                    </MudText>
                    @if (!string.IsNullOrEmpty(fileContent))
                    {
                        <MudText Typo="Typo.body2" Class="mt-2">
                            @fileContent
                        </MudText>
                    }
                </MudPaper>
            }
        </MudStack>
    </MudCardContent>
</MudCard>

@code {
    private string? currentFile;
    private string? fileContent;

    private async Task OpenFile()
    {
        var filePath = await Desktop.OpenFileDialogAsync(new FileDialogOptions
        {
            Title = "Select a Text File",
            Filters = new List<FileFilter>
            {
                new FileFilter
                {
                    Name = "Text Files",
                    Extensions = new[] { "*.txt", "*.md" }
                },
                new FileFilter
                {
                    Name = "All Files",
                    Extensions = new[] { "*.*" }
                }
            }
        });

        if (!string.IsNullOrEmpty(filePath))
        {
            currentFile = filePath;
            var data = await Desktop.ReadFileAsync(filePath);
            fileContent = Encoding.UTF8.GetString(data);
        }
    }

    private async Task SaveFile()
    {
        if (string.IsNullOrEmpty(fileContent))
        {
            await Desktop.ShowNotificationAsync("Error", "No content to save");
            return;
        }

        var savePath = await Desktop.SaveFileDialogAsync(new FileDialogOptions
        {
            Title = "Save Text File",
            DefaultFileName = "document.txt",
            Filters = new List<FileFilter>
            {
                new FileFilter
                {
                    Name = "Text Files",
                    Extensions = new[] { "txt" }
                }
            }
        });

        if (!string.IsNullOrEmpty(savePath))
        {
            await Desktop.WriteFileAsync(savePath, Encoding.UTF8.GetBytes(fileContent));
            await Desktop.ShowNotificationAsync("Success", "File saved successfully");
            currentFile = savePath;
        }
    }

    private async Task SelectFolder()
    {
        var folderPath = await Desktop.OpenFolderDialogAsync();
        if (!string.IsNullOrEmpty(folderPath))
        {
            currentFile = folderPath;
            fileContent = null;
        }
    }
}
```

---

### Settings Manager

A component for managing application settings:

```csharp
@page "/settings"
@inject IDesktopInteropService Desktop
@using System.Text.Json
@using System.Text

<MudCard>
    <MudCardHeader>
        <MudText Typo="Typo.h5">Application Settings</MudText>
    </MudCardHeader>
    <MudCardContent>
        <MudStack>
            <MudTextField @bind-Value="settings.Theme"
                Label="Theme" />
            <MudTextField @bind-Value="settings.Language"
                Label="Language" />
            <MudCheckBox @bind-Checked="settings.NotificationsEnabled">
                Enable Notifications
            </MudCheckBox>

            <MudButtonGroup>
                <MudButton Variant="Variant.Filled"
                    OnClick="@LoadSettings">
                    Load
                </MudButton>
                <MudButton Variant="Variant.Filled"
                    OnClick="@SaveSettings">
                    Save
                </MudButton>
            </MudButtonGroup>
        </MudStack>
    </MudCardContent>
</MudCard>

@code {
    private Settings settings = new();
    private const string SettingsFileName = "settings.json";

    protected override async Task OnInitializedAsync()
    {
        await LoadSettings();
    }

    private async Task LoadSettings()
    {
        try
        {
            var appDataPath = await Desktop.GetAppDataPathAsync();
            var settingsPath = Path.Combine(appDataPath, SettingsFileName);

            if (await Desktop.FileExistsAsync(settingsPath))
            {
                var json = Encoding.UTF8.GetString(
                    await Desktop.ReadFileAsync(settingsPath)
                );
                settings = JsonSerializer.Deserialize<Settings>(json) ?? new();
            }
        }
        catch (Exception ex)
        {
            await Desktop.ShowNotificationAsync("Error", $"Failed to load settings: {ex.Message}");
        }
    }

    private async Task SaveSettings()
    {
        try
        {
            var appDataPath = await Desktop.GetAppDataPathAsync();
            var settingsPath = Path.Combine(appDataPath, SettingsFileName);
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await Desktop.WriteFileAsync(settingsPath, Encoding.UTF8.GetBytes(json));
            await Desktop.ShowNotificationAsync("Success", "Settings saved");
        }
        catch (Exception ex)
        {
            await Desktop.ShowNotificationAsync("Error", $"Failed to save settings: {ex.Message}");
        }
    }

    public class Settings
    {
        public string Theme { get; set; } = "Light";
        public string Language { get; set; } = "English";
        public bool NotificationsEnabled { get; set; } = true;
    }
}
```

---

## Performance Notes

### ValueTask Optimization

Several methods return `ValueTask` or `ValueTask<T>` instead of `Task` or `Task<T>`:

- `FileExistsAsync` - `ValueTask<bool>`
- `MinimizeWindowAsync` - `ValueTask`
- `MaximizeWindowAsync` - `ValueTask`
- `RestoreWindowAsync` - `ValueTask`
- `SetWindowTitleAsync` - `ValueTask`
- `GetWindowStateAsync` - `ValueTask<WindowState>`
- `GetAppDataPathAsync` - `ValueTask<string>`
- `GetDocumentsPathAsync` - `ValueTask<string>`

**Why ValueTask?**

`ValueTask` is a value type that avoids heap allocation when the operation completes synchronously. This is beneficial for:

1. **Window Operations**: These complete immediately without asynchronous work
2. **File Existence Check**: Simple synchronous file system check
3. **System Paths**: Computed synchronously from environment variables
4. **Reduced GC Pressure**: No task object allocation on the heap

**Usage Guidelines:**

Always `await` ValueTask methods just like regular Tasks:

```csharp
// Correct - works with both Task and ValueTask
var state = await Desktop.GetWindowStateAsync();

// Avoid - don't call .ConfigureAwait(false) with ValueTask
// This can cause additional allocations
```

For library code that needs to handle ValueTask generically, use the Microsoft.Bcl.AsyncInterfaces NuGet package.

---

## Security Considerations

### URL Validation

The `OpenUrlInBrowserAsync` method includes strict security validation:

**Validation Rules:**
1. URL cannot be null or empty
2. URL must be a valid absolute URI format
3. Only `http`, `https`, and `mailto` schemes are allowed
4. Any other scheme throws `ArgumentException`

**Implementation:**
```csharp
// Built-in validation
if (string.IsNullOrWhiteSpace(url))
    throw new ArgumentException("URL cannot be null or empty");

if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
    throw new ArgumentException("Invalid URL format");

if (!Constants.Security.AllowedUrlSchemes.Contains(uri.Scheme))
    throw new ArgumentException($"URL scheme '{uri.Scheme}' is not allowed");
```

**Security Benefits:**
- Prevents command injection through URL malformation
- Restricts to safe URL schemes only
- Fails fast with clear error messages
- No string concatenation with user input

**Usage Pattern:**
```csharp
try
{
    await Desktop.OpenUrlInBrowserAsync(userProvidedUrl);
}
catch (ArgumentException ex)
{
    // Log and display error to user
    Debug.WriteLine($"Invalid URL: {ex.Message}");
}
```

---

### File Path Safety

File operations accept user-provided paths. Security best practices:

1. **Validate Paths**: Check for path traversal attempts
   ```csharp
   var fullPath = Path.GetFullPath(userProvidedPath);
   if (!fullPath.StartsWith(allowedDirectory))
       throw new ArgumentException("Path outside allowed directory");
   ```

2. **Use Sandboxed Directories**: Restrict to:
   - `GetAppDataPathAsync()` for application data
   - `GetDocumentsPathAsync()` for user documents
   - Dialog-selected paths only for arbitrary locations

3. **Avoid Direct File Paths**: Use dialogs when possible
   ```csharp
   // Safer - user selects the location
   var path = await Desktop.OpenFileDialogAsync(options);

   // Riskier - direct path from input
   var path = userProvidedPath; // Validate carefully
   ```

4. **Example Safe Pattern:**
   ```csharp
   private async Task<string?> SafeReadFile(string userPath)
   {
       try
       {
           var fullPath = Path.GetFullPath(userPath);
           var appDataPath = await Desktop.GetAppDataPathAsync();

           // Verify path is within app data
           if (!fullPath.StartsWith(appDataPath))
               throw new UnauthorizedAccessException("Access denied");

           if (!await Desktop.FileExistsAsync(fullPath))
               throw new FileNotFoundException("File not found");

           var data = await Desktop.ReadFileAsync(fullPath);
           return Encoding.UTF8.GetString(data);
       }
       catch (Exception ex)
       {
           Debug.WriteLine($"File access error: {ex.Message}");
           return null;
       }
   }
   ```

---

## Dependency Injection

Register `IDesktopInteropService` in your application startup:

```csharp
// Program.cs
builder.Services.AddScoped<IDesktopInteropService, DesktopInteropService>();
```

Inject in Blazor components:

```csharp
@inject IDesktopInteropService Desktop

@code {
    private async Task DoSomething()
    {
        await Desktop.OpenUrlInBrowserAsync("https://example.com");
    }
}
```

---

## Related Resources

- [Avalonia Documentation](https://docs.avaloniaui.net)
- [Blazor Integration Guide](../README.md)
- [File System Operations Pattern](../examples/file-operations.md)
- [Window Management Pattern](../examples/window-management.md)
