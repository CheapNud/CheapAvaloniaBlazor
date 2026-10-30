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

Since .NET 10, `blazor.web.js` ships in the `Microsoft.AspNetCore.App.Internal.Assets` NuGet package. Its MSBuild targets register it as a static web asset — but **only for projects using `Microsoft.NET.Sdk.Web`** (.NET 10 also required `OutputType=Exe`; .NET 11 accepts `WinExe` but keeps the Web SDK gate). Desktop apps use `Microsoft.NET.Sdk.Razor`, so the framework skips them.

CheapAvaloniaBlazor supplies the file itself, in three layers. Each one covers a case the layer above it cannot reach, and the app only needs one of them to succeed.

**1. Build and publish (PackageReference consumers).** `Build/CheapAvaloniaBlazor.targets` adds the script as `Content` targeting `wwwroot/_framework/blazor.web.js`, sourced from the restored `Microsoft.AspNetCore.App.Internal.Assets` package. Because it is `Content` and not a copy into the output folder, it survives `dotnet publish` and travels to machines with no SDK and no package cache. This is the normal path.

**2. NuGet cache extraction (developer machines, ProjectReference).** `BlazorFrameworkExtractor` runs at startup, picks the cached version matching the running runtime, and copies it into `wwwroot/_framework/`. MSBuild props and targets from a package are not imported through a `ProjectReference`, so the samples in this repo rely on this layer.

**3. Embedded copy (everything else).** A copy of the script is baked into `CheapAvaloniaBlazor.dll` at build time. When layers 1 and 2 both come up empty the extractor writes it out to `wwwroot/_framework/`, and if the install directory is read-only a middleware serves it straight from the assembly at `/_framework/blazor.web.js`. The DLL always ships with the app, so this layer cannot be missing.

`UseStaticFiles()` serves whatever landed on disk; the embedded middleware sits directly behind it and only answers when nothing else did.

The embedded copy is frozen at the version CheapAvaloniaBlazor was packed against, so on layer 3 an app running a newer 11.0.x runtime gets a slightly older bootstrapper. That is fine: `blazor.web.js` negotiates over a protocol that is stable across a major version. Layers 1 and 2 always match the app's own package graph, and layer 3 only runs when neither could supply anything at all.

Before 3.4.1 only layer 2 existed. A published app on a clean machine has no NuGet cache, so the script 404'd and the window rendered blank white with no error — the app was running and serving its HTML the whole time.

## How `cheap-blazor-interop.js` Is Served

The JS bridge that carries every desktop interop call is a static web asset of this library, served at `_content/CheapAvaloniaBlazor/cheap-blazor-interop.js`. `dotnet publish` copies it into the app's `wwwroot/_content/`, which is the normal path and works.

It is **also** embedded in `CheapAvaloniaBlazor.dll` and served from there when the file is not on disk, for the same reason `blazor.web.js` is. The static web asset chain has more failure modes than it appears: a manifest recording absolute paths into a machine that no longer exists, a consumer excluding assets, an install where `wwwroot` did not survive. When it breaks, `MapStaticAssets` throws `FileNotFoundException` and returns **500**, or answers **200 with a zero-length body** — never a 404 anything downstream could recover from.

Third-party `_content` assets such as MudBlazor's CSS and JS have no equivalent safety net. This library has no copy of them to serve, so they must be on disk.

## When Assets Do Not Resolve

Every deployment failure in this area looks identical from the outside: a blank white window, no controls, no error dialog, nothing in a network trace that reads as broken. To stop that being a guessing game, the host fetches its own root document once at startup, follows every local `_framework/` and `_content/` reference in it, and logs an error naming any that come back failing or empty. External URLs (Google Fonts and the like) are skipped — an offline machine is not a broken app.

If the log names an asset, the deployment is the thing to look at:

| Deployment | Result |
|---|---|
| `dotnet publish` (any configuration, framework-dependent or self-contained, single-file included) | All assets resolve. This is the supported way to ship. |
| Copying `bin/<config>/net11.0/` to another machine | **Third-party `_content` assets fail.** Build output resolves them through `{App}.staticwebassets.runtime.json`, which records absolute paths into the build machine's NuGet cache. This library's own two scripts still work, from the embedded copies. |

A manifest pointing at paths that do not exist used to kill the process at startup with no window and no message. It is now caught and logged, and the app runs on whatever is reachable.

## Technology Stack
| Component | Technology | Version |
|-----------|-----------|---------|
| UI Layer | Blazor Server + MudBlazor | 9.5.0 |
| Desktop Framework | Avalonia | 12.0.4 |
| WebView Host | Photino.NET | 4.0.16 |
| Backend | ASP.NET Core | .NET 11.0 |
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
