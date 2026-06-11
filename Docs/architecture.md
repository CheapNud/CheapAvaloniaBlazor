# Architecture

## How It Works
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

## How `blazor.web.js` Is Served

In .NET 10, `blazor.web.js` ships in the `Microsoft.AspNetCore.App.Internal.Assets` NuGet package. Its MSBuild targets register it as a static web asset — but **only for `OutputType=Exe` projects using `Microsoft.NET.Sdk.Web`**. Desktop apps use `OutputType=WinExe`, so the framework skips them entirely regardless of SDK choice.

CheapAvaloniaBlazor handles this automatically via `BlazorFrameworkExtractor`:
1. At startup, the extractor checks if `wwwroot/_framework/blazor.web.js` already exists
2. If not, it locates the file in the NuGet global packages cache (`~/.nuget/packages/microsoft.aspnetcore.app.internal.assets/{version}/_framework/`)
3. Copies it to `{contentRoot}/wwwroot/_framework/blazor.web.js`
4. `UseStaticFiles()` middleware then serves it at `/_framework/blazor.web.js`

## Technology Stack
| Component | Technology | Version |
|-----------|-----------|---------|
| UI Layer | Blazor Server + MudBlazor | 9.5.0 |
| Desktop Framework | Avalonia | 12.0.4 |
| WebView Host | Photino.NET | 4.0.16 |
| Backend | ASP.NET Core | .NET 10.0 |
| Interop | Custom desktop services | Built-in |

## Cross-Platform Compatibility

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
| Native Menu Bar | Tested | Not supported | Not supported |
| Multi-Window / Child Windows | Tested | Untested | Untested |
| Modal Dialogs (parent disable) | Tested | Not supported | Not supported |

> **Minimize to Tray** uses Windows `user32.dll` P/Invoke to fully hide the window. On Linux/macOS, the window falls back to a regular minimize (taskbar icon stays visible). System Tray behavior on Linux depends on the desktop environment's support for Avalonia's `TrayIcon` API.

## Services Overview

| Service | Lifetime | Purpose |
|---------|----------|---------|
| `IDesktopInteropService` | Scoped | File dialogs, window control, clipboard, system paths |
| `INotificationService` | Singleton | Desktop toasts + OS system notifications |
| `ISystemTrayService` | Singleton | Tray icon, context menu, minimize/restore to tray |
| `ISettingsService` | Singleton | JSON settings persistence (key-value + typed sections) |
| `IAppLifecycleService` | Singleton | Window lifecycle events (minimize, maximize, focus, close) |
| `IThemeService` | Singleton | OS dark/light mode detection and runtime change tracking |
| `IHotkeyService` | Singleton | System-wide global hotkeys (Windows + Linux, `IsSupported` for detection) |
| `IMenuBarService` | Singleton | Native Win32 menu bar (Windows only, `IsSupported` for detection) |
| `IWindowService` | Singleton | Child windows, modal dialogs, inter-window messaging |
| `IDiagnosticLoggerFactory` | Singleton | Conditional diagnostic logging |
| `PhotinoMessageHandler` | Singleton | JavaScript ↔ C# bridge communication |

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
