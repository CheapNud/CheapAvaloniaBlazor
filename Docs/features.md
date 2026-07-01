# Features


## System Tray (v2.1.0)
Full system tray integration with icon management, context menus, and minimize/close-to-tray behavior.

```csharp
new HostBuilder()
    .WithTitle("My App")
    .EnableSystemTray()
    .CloseToTray()
    .WithTrayTooltip("My App - Click to restore")
    .AddMudBlazor()
    .RunApp(args);
```

Control the tray from Blazor components:
```csharp
@inject ISystemTrayService SystemTray

// Minimize to tray (hides window, shows tray icon)
SystemTray.MinimizeToTray();

// Custom context menu items
SystemTray.AddTrayMenuItem(
    TrayMenuItemDefinition.Create("Settings", () => NavigateToSettings()));

// Checkable menu items, submenus, separators, async handlers
SystemTray.SetTrayMenu([
    TrayMenuItemDefinition.CreateCheckable("Dark Mode", isChecked: true, onToggle: ToggleTheme),
    TrayMenuItemDefinition.Separator(),
    TrayMenuItemDefinition.CreateAsync("Sync", async () => await SyncDataAsync())
]);
```

> **Note:** When `EnableDevTools` is true, the taskbar icon will remain visible after minimizing to tray. This is a Photino/WebView2 limitation - the DevTools window maintains taskbar presence. Disable `EnableDevTools` for clean tray-only behavior.

## Dual-Channel Notifications (v2.1.0)
Two independent notification channels for different use cases:

**Desktop Toasts** - Avalonia-rendered overlay notifications. Cross-platform, always works, styled by type:
```csharp
@inject INotificationService Notifications

// Show a desktop toast (Information, Success, Warning, Error)
Notifications.ShowDesktopNotification("Build Complete", "All tests passed",
    NotificationType.Success);

// Custom expiration
Notifications.ShowDesktopNotification("Warning", "Disk space low",
    NotificationType.Warning, TimeSpan.FromSeconds(10));
```

**System Notifications** - OS notification center via JavaScript Web Notification API (opt-in):
```csharp
@inject INotificationService Notifications
@inject IJSRuntime JSRuntime

// Requires .EnableSystemNotifications() in HostBuilder
await Notifications.ShowSystemNotificationAsync(JSRuntime, "Alert", "New message received");
```

Configure notification behavior:
```csharp
new HostBuilder()
    .EnableSystemNotifications()                           // Opt-in to OS notifications
    .WithNotificationPosition(NotificationPosition.TopRight)  // Toast position
    .WithMaxNotifications(5)                               // Max visible toasts
    .AddMudBlazor()
    .RunApp(args);
```

## Settings Persistence (v2.1.0)
JSON-based settings service with key-value and typed section APIs. Thread-safe, lazy-loaded, auto-save.

```csharp
new HostBuilder()
    .WithTitle("My App")
    .WithSettingsAppName("MyApp")       // Folder under %LocalAppData%
    .AutoSaveSettings()                 // Auto-save on every change (default: true)
    .AddMudBlazor()
    .RunApp(args);
```

**Key-value API** - Store and retrieve individual values:
```csharp
@inject ISettingsService Settings

// Set a value (auto-saves)
await Settings.SetAsync("theme", "dark");
await Settings.SetAsync("fontSize", 14);

// Get a value with default fallback
var theme = await Settings.GetAsync<string>("theme", "light");

// Check existence and delete
if (await Settings.ExistsAsync("oldKey"))
    await Settings.DeleteAsync("oldKey");
```

**Typed section API** - Work with strongly-typed settings classes:
```csharp
// Define your settings class
public class AppSettings
{
    public bool IsDarkMode { get; set; } = true;
    public List<string> RecentFiles { get; set; } = [];
}

// Read a section (key = class name)
var appSettings = await Settings.GetSectionAsync<AppSettings>();

// Update a section (read-modify-write, auto-saves)
await Settings.UpdateSectionAsync<AppSettings>(s => s.IsDarkMode = false);

// Replace a section entirely
await Settings.SetSectionAsync(new AppSettings { IsDarkMode = true });
```

Settings are stored at `%LocalAppData%/{appName}/settings.json`. Configure the location with `WithSettingsFolder()` or `WithSettingsFileName()`.

## App Lifecycle Events (v2.1.0)
Subscribe to native window lifecycle events from Blazor components. Track window state and react to minimize, maximize, restore, and focus changes.

```csharp
@inject IAppLifecycleService Lifecycle

// Read-only state properties
var minimized = Lifecycle.IsMinimized;
var maximized = Lifecycle.IsMaximized;
var focused = Lifecycle.IsFocused;

// Subscribe to events
Lifecycle.Minimized += () => Console.WriteLine("Window minimized");
Lifecycle.Maximized += () => Console.WriteLine("Window maximized");
Lifecycle.Restored += () => Console.WriteLine("Window restored");
Lifecycle.Activated += () => Console.WriteLine("Window focused");
Lifecycle.Deactivated += () => Console.WriteLine("Window lost focus");

// Prevent close with cancellation support
Lifecycle.Closing += (sender, args) =>
{
    if (HasUnsavedChanges)
        args.Cancel = true; // Prevent window from closing
};
```

No builder configuration required - `IAppLifecycleService` is always available.

## Theme Detection (v2.1.0)
Detect OS dark/light mode preference and automatically react to runtime theme switches.

```csharp
@inject IThemeService Theme

// Read current OS theme
var theme = Theme.CurrentTheme;  // SystemTheme.Light or SystemTheme.Dark
var isDark = Theme.IsDarkMode;   // Convenience bool

// React to OS theme changes at runtime
Theme.ThemeChanged += (newTheme) =>
{
    // Auto-sync MudBlazor dark mode with OS preference
    _isDarkMode = newTheme == SystemTheme.Dark;
    InvokeAsync(StateHasChanged);
};
```

Uses Avalonia's built-in platform theme detection under the hood. No builder configuration required - `IThemeService` is always available.

## Global Hotkeys (v2.1.0)
Register system-wide keyboard shortcuts that fire even when the application window is not focused. Supported on Windows and Linux, with `IsSupported` for runtime platform detection.

**Platform support:**
- **Windows**: Win32 `RegisterHotKey` API
- **Linux (Wayland)**: D-Bus GlobalShortcuts portal (KDE 5.27+, GNOME 48+, Hyprland)
- **Linux (X11)**: `XGrabKey` fallback (X11 sessions and XWayland)
- **macOS**: Not supported (`IsSupported` returns false)

```csharp
@inject IHotkeyService Hotkeys

// Check platform support
if (Hotkeys.IsSupported)
{
    // Register a global hotkey (Ctrl+Shift+H)
    var id = Hotkeys.RegisterHotkey(
        HotkeyModifiers.Ctrl | HotkeyModifiers.Shift,
        Key.H,
        () => Console.WriteLine("Hotkey pressed!"));

    // Unregister when no longer needed
    Hotkeys.UnregisterHotkey(id);

    // Or unregister all at once
    Hotkeys.UnregisterAll();
}

// Global event for any hotkey press
Hotkeys.HotkeyPressed += (hotkeyId) =>
    Console.WriteLine($"Hotkey {hotkeyId} fired");
```

**Supported keys** (via `Avalonia.Input.Key`):
- Letters: `A`–`Z`
- Digits: `D0`–`D9`
- Function keys: `F1`–`F24`
- NumPad: `NumPad0`–`NumPad9`
- Navigation: `Home`, `End`, `PageUp`, `PageDown`, `Insert`, `Delete`
- Arrows: `Left`, `Up`, `Right`, `Down`
- Special: `Space`, `Return`, `Escape`, `Tab`, `Back`
- Misc: `PrintScreen`, `Pause`, `CapsLock`, `NumLock`, `Scroll`

**Modifiers** (combinable with `|`): `Ctrl`, `Alt`, `Shift`, `Win`

No builder configuration required - `IHotkeyService` is always available. Automatically selects the best backend for the current platform.

## Native Menu Bar (v2.1.0)
Attach a Win32 native menu bar to the Photino window with File, Edit, View, Help menus or any custom layout. Supports mnemonics (Alt+F), accelerator display text, checkable items, and dynamic enable/disable.

```csharp
using CheapAvaloniaBlazor.Models;

new HostBuilder()
    .WithTitle("My App")
    .WithMenuBar(
    [
        MenuItemDefinition.CreateSubMenu("&File",
        [
            MenuItemDefinition.Create("&New", () => NewFile(), id: "file_new", accelerator: "Ctrl+N"),
            MenuItemDefinition.Create("&Open...", () => OpenFile(), id: "file_open", accelerator: "Ctrl+O"),
            MenuItemDefinition.Separator(),
            MenuItemDefinition.Create("E&xit", () => Environment.Exit(0), id: "file_exit"),
        ]),
        MenuItemDefinition.CreateSubMenu("&View",
        [
            MenuItemDefinition.CreateCheckable("&Dark Mode", false, () => ToggleTheme(), id: "view_dark"),
        ]),
        MenuItemDefinition.CreateSubMenu("&Help",
        [
            MenuItemDefinition.Create("&About", () => ShowAbout(), id: "help_about"),
        ]),
    ])
    .AddMudBlazor()
    .RunApp(args);
```

Control from Blazor components:
```csharp
@inject IMenuBarService MenuBar

// Enable/disable menu items dynamically
MenuBar.EnableMenuItem("file_new", isEnabled: false);

// Toggle check state
MenuBar.CheckMenuItem("view_dark", isChecked: true);

// React to any menu click
MenuBar.MenuItemClicked += (menuItemId) =>
    Console.WriteLine($"Menu clicked: {menuItemId}");
```

**Platform support:** Windows only (Win32 native menu). `IsSupported` returns false on Linux/macOS. Accelerator text is display-only — use `IHotkeyService` for actual keyboard bindings.

## Multi-Window Support (v2.1.0)
Create child windows and modal dialogs from Blazor components. Each window runs on its own background thread with an independent Blazor SignalR circuit, connected to the same embedded server.

**Open a child window (URL path):**
```csharp
@inject IWindowService WindowService

var windowId = await WindowService.CreateWindowAsync(
    WindowOptions.FromUrl("/settings", "Settings"));
```

**Open a modal dialog (component type, no @page needed):**
```csharp
var options = WindowOptions.FromComponent<SettingsDialog>("Settings");
options.Width = 500;
options.Height = 400;
options.Resizable = false;

ModalResult result = await WindowService.CreateModalAsync(options);

if (result.Confirmed)
{
    var data = result.GetData<Dictionary<string, object>>();
    // Use the returned data
}
```

**Complete a modal from inside the dialog component:**
```csharp
[Parameter] public string? WindowId { get; set; }

// Save button
WindowService.CompleteModal(WindowId, ModalResult.Ok(myData));

// Cancel button
WindowService.CompleteModal(WindowId, ModalResult.Cancel());
```

**Inter-window messaging:**
```csharp
// Send to a specific window
WindowService.SendMessage(targetWindowId, "refresh", payload);

// Broadcast to all windows
WindowService.BroadcastMessage("theme_changed", newTheme);

// Receive messages (filter by your own window ID)
WindowService.MessageReceived += (targetId, type, payload) =>
{
    if (targetId == myWindowId || targetId == "*")
        HandleMessage(type, payload);
};
```

**Platform support:** Child windows work on all platforms. Modal behavior (parent window disabling) is Windows-only via Win32 `EnableWindow`. On Linux/macOS, `IsModalSupported` returns false but dialogs still open as regular windows.

**Limits:** When using `WindowOptions.ComponentType`, each distinct component type is registered in an internal security whitelist (prevents arbitrary type instantiation from URL parameters). The whitelist is capped at **256 distinct types** (`Constants.Window.MaxRegisteredComponentTypes`). Re-using the same type across multiple windows does not count again. This limit is a safety guard — typical apps use far fewer component types. URL-path windows (`WindowOptions.FromUrl`) are not affected.

## Drag-and-Drop Files (v2.1.0)

Receive file drop events from the OS in Blazor components. Uses HTML5 drag-and-drop in WebView2, bridged to C# via the Photino message channel.

```csharp
@inject IDragDropService DragDropService

// Subscribe to file drops
DragDropService.FilesDropped += (files) =>
{
    foreach (var file in files)
    {
        Console.WriteLine($"{file.Name} ({file.Size} bytes, {file.ContentType})");
    }
};

// Visual feedback during drag-over
DragDropService.DragEnter += () => showDropZone = true;
DragDropService.DragLeave += () => showDropZone = false;

// Check drag state at any time
if (DragDropService.IsDragOver) { /* highlight UI */ }
```

**File metadata:** Name, Size, ContentType, LastModified. File system paths (`FilePath`) are null in V1 — WebView2's browser sandbox does not expose paths from HTML5 drag events. Native file path extraction (Win32 `IDropTarget`) is planned for V2.

**Cross-platform:** Works on Windows, Linux, and macOS. Auto-initialized when the application starts — no builder configuration needed.

## Splash Screen (v1.1.0)
Enabled by default - Shows a loading screen while your app initializes.

```csharp
// Customize splash screen
new HostBuilder()
    .WithTitle("My App")
    .WithSplashScreen("My App", "Loading workspace...")
    .AddMudBlazor()
    .RunApp(args);
```

**Full documentation:** See **[Splash Screen Guide](splash-screen.md)** for customization options, custom content, and advanced configuration.

## Desktop Interop API
Full native desktop capabilities with **ValueTask optimization** for zero-allocation performance:

```csharp
@inject IDesktopInteropService Desktop

// File dialogs
var file = await Desktop.OpenFileDialogAsync(new FileDialogOptions
{
    Title = "Select a file",
    Filters = [new FileFilter { Name = "Images", Extensions = ["png", "jpg"] }]
});
var savePath = await Desktop.SaveFileDialogAsync();
var folder = await Desktop.OpenFolderDialogAsync();

// Window management
await Desktop.MinimizeWindowAsync();
await Desktop.MaximizeWindowAsync();
await Desktop.SetWindowTitleAsync("New Title");
var state = await Desktop.GetWindowStateAsync();

// Clipboard
await Desktop.SetClipboardTextAsync("Copied!");
var text = await Desktop.GetClipboardTextAsync();

// System integration
await Desktop.OpenUrlInBrowserAsync("https://github.com");
var appData = await Desktop.GetAppDataPathAsync();
```

**Full API reference:** See **[Desktop Interop API](desktop-interop.md)** for all available methods and examples.

## Diagnostics & Logging
Comprehensive diagnostic system for troubleshooting:

```csharp
new HostBuilder()
    .EnableDiagnostics()  // Detailed startup and operation logging
    .AddMudBlazor()
    .RunApp(args);
```

**Full guide:** See **[Diagnostics & Debugging](diagnostics.md)** for DiagnosticLogger usage and troubleshooting scenarios.
