# CheapAvaloniaBlazor

Build cross-platform desktop applications using Blazor Server, Avalonia, and Photino.

Combines Blazor Server + MudBlazor + Avalonia + Photino to create native desktop apps with full file system access across Windows, Linux, and macOS using familiar Razor pages and C# components.

[![NuGet](https://img.shields.io/nuget/v/CheapAvaloniaBlazor.svg)](https://www.nuget.org/packages/CheapAvaloniaBlazor)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)

> **PRE-ALPHA HOBBY PROJECT**
> This is an experimental project developed as a personal hobby. Expect breaking changes, incomplete features, and limited support. Use at your own risk in non-production environments.

---

## Why CheapAvaloniaBlazor?

Building cross-platform desktop apps traditionally requires learning different UI frameworks for each platform or dealing with complex native interop. CheapAvaloniaBlazor allows you to use existing web development skills (HTML, CSS, Blazor, C#) to build desktop applications with native capabilities.

### Use Cases:
- Web developers transitioning to desktop development
- Rapid prototyping of desktop applications
- Line-of-business apps requiring native file system access
- Cross-platform tools that need to run on Windows, Linux, and macOS
- Blazor developers wanting desktop capabilities beyond browser limitations

---

## Documentation

**Complete documentation available in the [docs](./docs) folder:**

- **[Installation Guide](./docs/installation.md)** - System requirements, installation methods, verification, and troubleshooting
- **[Getting Started](./docs/getting-started.md)** - Step-by-step tutorial for your first desktop app
- **[Splash Screen](./docs/splash-screen.md)** - Splash screen configuration and customization
- **[Desktop Interop API](./docs/desktop-interop.md)** - File dialogs, window management, system integration, clipboard operations
- **[Diagnostics & Debugging](./docs/diagnostics.md)** - Diagnostic system, logging, and troubleshooting
- **[Advanced Configuration](./docs/advanced-configuration.md)** - HostBuilder API reference, SignalR, hot reload, custom pipeline

---

## Quick Installation

```bash
# Create a new console project
dotnet new console -n MyDesktopApp
cd MyDesktopApp

# Add CheapAvaloniaBlazor package
dotnet add package CheapAvaloniaBlazor
```

**More installation options:** See the **[Installation Guide](./docs/installation.md)** for Visual Studio GUI instructions, Avalonia templates, and troubleshooting.

---

## Quick Start

### Minimal Example (3 Steps)

**1. Update your `.csproj` to use Razor SDK:**
```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <PackageReference Include="CheapAvaloniaBlazor" Version="2.0.2" />
  </ItemGroup>
</Project>
```

**2. Replace `Program.cs`:**
```csharp
using CheapAvaloniaBlazor.Hosting;

namespace MyDesktopApp;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        new HostBuilder()
            .WithTitle("My Desktop App")
            .WithSize(1200, 800)
            .AddMudBlazor()
            .RunApp(args);
    }
}
```

**3. Add Blazor components** (App.razor as HTML root, Routes.razor for routing, MainLayout.razor, Index.razor, etc.)

**Full tutorial:** See the **[Getting Started Guide](./docs/getting-started.md)** for complete step-by-step instructions with all files.

---

## Features

### System Tray (v2.0.0)
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

### Dual-Channel Notifications (v2.0.2)
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

### Settings Persistence (v2.1.0)
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

### App Lifecycle Events (v2.2.0)
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

### Theme Detection (v2.2.0)
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

### Global Hotkeys (v2.3.0)
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

### Splash Screen (v1.1.0)
Enabled by default - Shows a loading screen while your app initializes.

```csharp
// Customize splash screen
new HostBuilder()
    .WithTitle("My App")
    .WithSplashScreen("My App", "Loading workspace...")
    .AddMudBlazor()
    .RunApp(args);
```

**Full documentation:** See **[Splash Screen Guide](./docs/splash-screen.md)** for customization options, custom content, and advanced configuration.

### Desktop Interop API
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

**Full API reference:** See **[Desktop Interop API](./docs/desktop-interop.md)** for all available methods and examples.

### Diagnostics & Logging
Comprehensive diagnostic system for troubleshooting:

```csharp
new HostBuilder()
    .EnableDiagnostics()  // Detailed startup and operation logging
    .AddMudBlazor()
    .RunApp(args);
```

**Full guide:** See **[Diagnostics & Debugging](./docs/diagnostics.md)** for DiagnosticLogger usage and troubleshooting scenarios.

---

## Configuration

### HostBuilder Fluent API
Configure your application with an intuitive fluent interface:

```csharp
new HostBuilder()
    // Window
    .WithTitle("My App").WithSize(1400, 900).WithIcon("icon.ico")
    .Chromeless(false).CenterWindow()

    // Splash Screen
    .WithSplashScreen("My App", "Loading...")

    // System Tray
    .EnableSystemTray().CloseToTray()
    .WithTrayTooltip("My App").WithTrayIcon("tray.ico")

    // Notifications
    .EnableSystemNotifications()
    .WithNotificationPosition(NotificationPosition.BottomRight)
    .WithMaxNotifications(3)

    // Settings Persistence
    .WithSettingsAppName("MyApp")
    .AutoSaveSettings()

    // Server
    .UsePort(5001).UseHttps(true)

    // Diagnostics
    .EnableDiagnostics().EnableDevTools(true)

    // UI Framework
    .AddMudBlazor()

    // Advanced options
    .ConfigureOptions(opts => { /* ... */ })

    .RunApp(args);
```

**Complete reference:** See **[Advanced Configuration](./docs/advanced-configuration.md)** for all HostBuilder methods, SignalR configuration, custom pipeline/endpoints, and more.

---

## Project Structure

```
MyDesktopApp/
├── Program.cs                 # Application entry point
├── Components/
│   ├── App.razor             # HTML document root (loads scripts, CSS, renders Routes)
│   ├── Routes.razor          # Blazor router configuration
│   └── MainLayout.razor      # Main application layout
├── Pages/
│   ├── Index.razor           # Home page
│   └── Files.razor           # File management page
├── wwwroot/                  # Static web assets
│   └── css/
└── Services/                 # Your business logic
    └── IMyService.cs
```

---

## Architecture & Integration

### How It Works
```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Avalonia      │    │  Blazor Server   │    │     Photino     │
│   (Desktop      │◄──►│  (Web UI &       │◄──►│   (WebView      │
│    Framework)   │    │   Components)    │    │    Hosting)     │
└─────────────────┘    └──────────────────┘    └─────────────────┘
        ▲                        ▲                        ▲
        │                        │                        │
        ▼                        ▼                        ▼
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Native OS     │    │    MudBlazor     │    │   File System   │
│   Integration   │    │   (Material UI)  │    │     Access      │
└─────────────────┘    └──────────────────┘    └─────────────────┘
```

### Technology Stack
| Component | Technology | Version |
|-----------|-----------|---------|
| UI Layer | Blazor Server + MudBlazor | 8.15.0 |
| Desktop Framework | Avalonia | 11.3.11 |
| WebView Host | Photino.NET | 4.0.16 |
| Backend | ASP.NET Core | .NET 10.0 |
| Interop | Custom desktop services | Built-in |

### Cross-Platform Compatibility

| Feature | Windows | Linux | macOS |
|---------|---------|-------|-------|
| Blazor UI / MudBlazor | Tested | Untested | Untested |
| File Dialogs | Tested | Untested | Untested |
| Window Management | Tested | Untested | Untested |
| Clipboard | Tested | Untested | Untested |
| Desktop Toasts | Tested | Untested | Untested |
| System Tray | Tested | Varies by DE | Untested |
| Minimize to Tray (hide window) | Tested | Fallback to minimize | Fallback to minimize |
| System Notifications (JS) | Tested | Untested | Untested |
| Settings Persistence | Tested | Untested | Untested |
| App Lifecycle Events | Tested | Untested | Untested |
| Theme Detection | Tested | Untested | Untested |
| Global Hotkeys | Tested | Tested (D-Bus/X11) | Not supported |

> **Minimize to Tray** uses Windows `user32.dll` P/Invoke to fully hide the window. On Linux/macOS, the window falls back to a regular minimize (taskbar icon stays visible). System Tray behavior on Linux depends on the desktop environment's support for Avalonia's `TrayIcon` API.

### Services Overview

| Service | Lifetime | Purpose |
|---------|----------|---------|
| `IDesktopInteropService` | Scoped | File dialogs, window control, clipboard, system paths |
| `INotificationService` | Singleton | Desktop toasts + OS system notifications |
| `ISystemTrayService` | Singleton | Tray icon, context menu, minimize/restore to tray |
| `ISettingsService` | Singleton | JSON settings persistence (key-value + typed sections) |
| `IAppLifecycleService` | Singleton | Window lifecycle events (minimize, maximize, focus, close) |
| `IThemeService` | Singleton | OS dark/light mode detection and runtime change tracking |
| `IHotkeyService` | Singleton | System-wide global hotkeys (Windows + Linux, `IsSupported` for detection) |
| `IDiagnosticLoggerFactory` | Singleton | Conditional diagnostic logging |
| `PhotinoMessageHandler` | Singleton | JavaScript ↔ C# bridge communication |

---

## Sample Applications

### MinimalApp
Location: `samples/MinimalApp/` - Absolute minimum code to run a Blazor desktop app.

### DesktopFeatures
Location: `samples/DesktopFeatures/` - Demonstrates all desktop interop features:
- File dialogs (open, save, folder)
- Window controls (minimize, maximize, restore, title)
- Clipboard operations (copy, paste)
- System tray (minimize to tray, custom menu items, tooltip)
- Desktop toast notifications (all severity types)
- System notifications (OS notification center)
- Settings persistence (key-value and typed section APIs)
- App lifecycle events (window state tracking and event log)
- Theme detection (OS dark/light mode with follow-system toggle)
- Global hotkeys (system-wide keyboard shortcuts, Windows + Linux)
- System paths and browser integration

### CheapShotcutRandomizer (External)
A video editing workflow tool built with CheapAvaloniaBlazor that demonstrates the framework's capabilities for building desktop applications with complex features.

**Repository:** [https://github.com/CheapNud/CheapShotcutRandomizer](https://github.com/CheapNud/CheapShotcutRandomizer)

---

## Build & Deployment

### Development
```bash
# Command Line / VS Code Users
dotnet run

# Visual Studio Users
Press F5 or Debug > Start Debugging

# Hot reload enabled automatically in both environments
# Make changes to .razor files and see instant updates
```

### Production Builds
```bash
# Build release version
dotnet publish -c Release -r win-x64 --self-contained

# Create single-file executable
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true

# Cross-platform builds
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true
dotnet publish -c Release -r osx-x64 --self-contained -p:PublishSingleFile=true
```

---

## System Requirements

### Runtime Requirements
- **.NET 10.0** or later
- **Windows 10+** (Tested)
- **Linux with WebKit** (Untested - on roadmap)
- **macOS 10.15+** (Untested - on roadmap)

### Development Requirements
- **Visual Studio 2022** (17.8+) or **VS Code**
- **.NET 10.0 SDK**
- **C# 14** language features

### Package Dependencies
- `Avalonia 11.3.11` - Desktop framework
- `MudBlazor 8.15.0` - Material Design components
- `Photino.NET 4.0.16` - WebView hosting
- `Tmds.DBus.Protocol 0.22.0` - D-Bus protocol for Linux global hotkeys
- `Microsoft.AspNetCore.App` - ASP.NET Core framework reference

---

## Troubleshooting

### Common Issues & Solutions

**Build Errors**
```bash
# Ensure correct .NET version
dotnet --version  # Should be 10.0+

# Clear and restore packages
dotnet clean
dotnet restore
dotnet build
```

**Window Doesn't Appear**
- Check if port 5000/5001 is available
- Verify no firewall blocking local connections
- Look for exceptions in console output
- Try different port: `builder.UsePort(8080)`

**MudBlazor Styles Missing**
- Verify CSS reference in `App.razor`:
  ```html
  <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
  ```
- Check browser dev tools for 404 errors
- Ensure `AddMudBlazor()` is called in HostBuilder

**Platform Compatibility Issues**
- **Linux/macOS**: Currently untested - if you encounter issues, please report them!
- **Windows**: Fully tested and supported
- Dependencies (Avalonia, Photino) should work cross-platform, but integration not verified

**File Dialog Not Working**
- File dialogs use Avalonia StorageProvider (working since v1.0.67)
- Ensure you're using latest version: `dotnet add package CheapAvaloniaBlazor`
- Check `IDesktopInteropService` injection

**Taskbar Icon Stays Visible When Minimized to Tray**
- This happens when `EnableDevTools` is true - the DevTools window keeps the taskbar icon alive
- This is a Photino/WebView2 limitation, not a CheapAvaloniaBlazor bug
- Disable `EnableDevTools` for production or when testing tray behavior

### Debug Mode

```csharp
var builder = new HostBuilder()
    .WithTitle("My App")
    .EnableConsoleLogging()    // Show console window for logging
    .EnableDevTools()          // Enable F12 developer tools
    .EnableContextMenu()       // Enable right-click menu (default: true)
    .EnableDiagnostics()       // Comprehensive diagnostic logging
    .AddMudBlazor();
```

---

## Project Status & Roadmap

### Current Status: v2.3.0
- Core Framework: Avalonia + Blazor + Photino integration
- NuGet Package: Published and functional
- Splash Screen: Enabled by default, fully customizable
- System Tray: Full icon, menu, minimize/close-to-tray support
- Dual Notifications: Desktop toasts (Avalonia) + system notifications (JS Web Notification API)
- Settings Persistence: JSON-based key-value + typed section APIs with auto-save
- App Lifecycle Events: Window state tracking, close cancellation, focus/minimize/maximize events
- Theme Detection: OS dark/light mode detection with runtime change tracking
- Global Hotkeys: System-wide keyboard shortcuts (Windows via Win32, Linux via D-Bus/X11)
- File System Interop: Cross-platform file dialogs via Avalonia StorageProvider
- Window Management: Minimize, maximize, resize, title changes, hide/show
- Clipboard: Read/write text via clipboard API
- JavaScript Bridge: Full bidirectional communication
- MudBlazor Integration: Full component library support
- Diagnostics: Comprehensive logging and troubleshooting system
- Performance: ValueTask for zero-allocation synchronous operations

### Planned Features
- Testing Framework: Unit and integration test support
- Cross-Platform Testing: Full compatibility validation on Linux and macOS
- Alternative UI Frameworks: Support for Tailwind CSS, Radzen, and other Blazor component libraries
- Package Templates: `dotnet new` project templates
- Plugin System: Extensible architecture

### Known Limitations
- Alpha stage project - some breaking changes possible but architecture now stable
- Currently tested on Windows only - Linux and macOS compatibility validation in progress
- MudBlazor-focused currently - other UI framework integrations planned

---

## Contributing & Support

### How to Help
- Report Issues: Found a bug? [Create an issue](https://github.com/CheapNud/CheapAvaloniaBlazor/issues)
- Provide Feedback: Share your experience and suggestions
- Testing: Try it in your projects and report compatibility issues
- Documentation: Suggest improvements to examples and guides

This is a hobby project - support is provided on a best-effort basis.

---

## License & Attribution

MIT License - Use freely in personal and commercial projects.

### Built Using:
- [Avalonia](https://avaloniaui.net/) - Cross-platform .NET desktop framework
- [Blazor](https://blazor.net/) - Build interactive web UIs using C#
- [MudBlazor](https://mudblazor.com/) - Material Design component library
- [Photino](https://www.tryphotino.io/) - Lightweight cross-platform WebView

---

## Getting Started

```bash
# Create new project
dotnet new console -n MyFirstDesktopApp
cd MyFirstDesktopApp

# Add CheapAvaloniaBlazor
dotnet add package CheapAvaloniaBlazor

# Follow the Quick Start guide above
```

Questions or issues? [Open an issue](https://github.com/CheapNud/CheapAvaloniaBlazor/issues) for feedback.

---
