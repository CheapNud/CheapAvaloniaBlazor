# CheapAvaloniaBlazor - TODO & Roadmap

## Known Technical Debt

### Environment Hardcoded to "Development" in EmbeddedBlazorHostService.cs

**Status**: WORKING - DO NOT "FIX"

**Location**: `Services/EmbeddedBlazorHostService.cs` (line ~105)

**The Issue**: Environment is hardcoded to "Development" so `UseStaticWebAssets()` resolves
NuGet-provided static assets (`_content/MudBlazor/`, `_content/CheapAvaloniaBlazor/`, etc.)
from the development manifest. Production mode expects these files to be physically published
to wwwroot, which doesn't happen in desktop app `dotnet run` workflows.

Since this is a localhost-only desktop app (not exposed to the internet), Development mode's
relaxed security posture is irrelevant.

### blazor.web.js Serving Strategy

**Location**: `Utilities/BlazorFrameworkExtractor.cs`, `Build/CheapAvaloniaBlazor.targets`

**Background**: In .NET 10, `blazor.web.js` ships in the `Microsoft.AspNetCore.App.Internal.Assets`
NuGet package. Its MSBuild targets only register the file as a static web asset when the consuming
project uses `Microsoft.NET.Sdk.Web`. Desktop apps commonly use `Microsoft.NET.Sdk.Razor`, which
means `blazor.web.js` never enters the static web assets manifest and 404s at runtime.

**Why switching SDK doesn't help**: Even with `Microsoft.NET.Sdk.Web`, the Internal.Assets
MSBuild targets have `Condition="'$(OutputType)' == 'Exe'"` — desktop apps use `WinExe`,
so the targets are skipped entirely. Web SDK is not a solution for desktop apps.

**Runtime solution** (`BlazorFrameworkExtractor`): At startup, `EmbeddedBlazorHostService`
calls `BlazorFrameworkExtractor.ExtractBlazorFrameworkJs()` which finds `blazor.web.js` in
the NuGet global packages folder
(`~/.nuget/packages/microsoft.aspnetcore.app.internal.assets/{version}/_framework/`)
and copies it to `{contentRoot}/wwwroot/_framework/`. `UseStaticFiles()` then serves it
from disk. No consumer action required.

**Build-time belt-and-suspenders**: `Build/CheapAvaloniaBlazor.targets` has a
`CopyBlazorFrameworkFilesFromLibrary` MSBuild target that copies the file at build time
for NuGet package consumers. This only fires for PackageReference (not ProjectReference).

**What Was Tried Before This Solution**:
- Embedded resources approach
- Custom static file providers
- Various combinations of `UseStaticWebAssets()` and `UseStaticFiles()`
- Passing `WebApplicationOptions` to the builder (breaks Blazor script serving)

**Future Consideration**: Only revisit if Microsoft changes how framework static assets work
in a future .NET version. The current approach is robust across both SDK types.

---

## Priority 1 - Essential Desktop Features

### System Tray Support - DONE (v2.1.0)
- [x] Integrate Avalonia's `TrayIcon` / `NativeMenu` APIs
- [x] Add `ISystemTrayService` interface
- [x] Methods: `ShowTrayIcon()`, `HideTrayIcon()`, `SetTrayIcon()`, `SetTrayTooltip()`
- [x] Support for context menu on tray icon (submenus, checkable items, separators, async handlers)
- [x] "Minimize to tray" / "Close to tray" options in `CheapAvaloniaBlazorOptions`
- [x] Tray icon click events (single click, double click)
- [x] Fluent builder: `EnableSystemTray()`, `CloseToTray()`, `WithTrayTooltip()`, `WithTrayIcon()`
- [x] Window hide/show via user32.dll P/Invoke (Windows), minimize fallback (Linux/macOS)
- [x] Fallback icon generation (16x16 WriteableBitmap) when no icon path provided

### Dual-Channel Notifications - DONE (v2.1.0)
- [x] `INotificationService` interface with desktop toasts + system notifications
- [x] Desktop toasts via Avalonia `WindowNotificationManager` on transparent overlay window
- [x] System notifications via JS Web Notification API (opt-in)
- [x] Configurable position (`NotificationPosition` enum) and max visible toasts
- [x] Fluent builder: `EnableSystemNotifications()`, `WithNotificationPosition()`, `WithMaxNotifications()`
- [x] Proper `IDisposable` cleanup of overlay window

### Settings Persistence Helper - DONE (v2.1.0)
- [x] `ISettingsService` interface with key-value and typed section APIs
- [x] JSON-based storage in app data folder (`%LocalAppData%/{appName}/settings.json`)
- [x] Key-value API: `GetAsync<T>()`, `SetAsync<T>()`, `DeleteAsync()`, `ExistsAsync()`
- [x] Typed section API: `GetSectionAsync<T>()`, `SetSectionAsync<T>()`, `UpdateSectionAsync<T>()`
- [x] Auto-save on change (configurable via `AutoSaveSettings`)
- [x] Thread-safe via `SemaphoreSlim`, lazy-loaded on first access
- [x] Fluent builder: `WithSettingsAppName()`, `WithSettingsFolder()`, `WithSettingsFileName()`, `AutoSaveSettings()`
- [x] Proper `IDisposable` cleanup, `SettingsChanged` event

### App Lifecycle Events - DONE (v2.1.0)
- [x] `OnClosing` event with cancellation support (prevent close, confirm dialogs)
- [x] `OnMinimized` event
- [x] `OnMaximized` event
- [x] `OnRestored` event
- [x] `OnActivated` / `OnDeactivated` (window focus)
- [x] Expose via dedicated `IAppLifecycleService` singleton
- [x] Read-only state properties: `IsMinimized`, `IsMaximized`, `IsFocused`
- [x] Wired to Photino native window events in `BlazorHostWindow`
- [x] Demo panel in DesktopFeatures sample with event log

### Theme Detection - DONE (v2.1.0)
- [x] Detect OS dark/light mode preference via Avalonia's `ActualThemeVariant`
- [x] `IThemeService` interface with `CurrentTheme`, `IsDarkMode`, `ThemeChanged` event
- [x] `SystemTheme` enum (`Light`, `Dark`)
- [x] Runtime theme change tracking via `ActualThemeVariantChanged`
- [x] Auto-apply to MudBlazor: "Follow System Theme" toggle in DesktopFeatures sample
- [x] Demo panel with theme state display and change log

---

## Priority 2 - Enhanced Desktop Experience

### Global Hotkeys - DONE (v2.1.0)
- [x] Register system-wide keyboard shortcuts via Win32 `RegisterHotKey` API
- [x] `IHotkeyService` interface with `IsSupported` for cross-platform detection
- [x] Methods: `RegisterHotkey()`, `UnregisterHotkey()`, `UnregisterAll()`
- [x] Modifier key support (Ctrl, Alt, Shift, Win) via `HotkeyModifiers` flags enum
- [x] Conflict detection with existing system hotkeys (Win32 error propagation)
- [x] `HotkeyPressed` global event for any hotkey press
- [x] Dedicated background thread with Win32 `GetMessage` loop
- [x] `Avalonia.Input.Key` → Win32 VK code mapping via `KeyMapper`
- [x] Proper `IDisposable` cleanup (unregister all, stop message pump)
- [x] Demo panel in DesktopFeatures sample with register/unregister and event log
- [x] Platform backend architecture (`IHotkeyBackend` → Windows/D-Bus/X11/Null)
- [x] Linux D-Bus GlobalShortcuts portal backend (Wayland: KDE 5.27+, GNOME 48+, Hyprland)
- [x] Linux X11 XGrabKey fallback backend (X11 sessions + XWayland)
- [x] NumLock/CapsLock-aware key grab (4 modifier variants per hotkey)
- [x] Automatic backend selection with D-Bus → X11 → Null fallback chain
- [x] `KeyMapper.ToX11KeySym()` and `KeyMapper.ToPortalTrigger()` mapping methods
- [ ] Linux testing: verify D-Bus portal on KDE/GNOME/Hyprland
- [ ] Linux testing: verify X11 backend on X11 sessions
- [ ] Linux testing: verify D-Bus → X11 fallback chain on X11-only systems

### Native Menu Bar - DONE (v2.1.0)
- [x] File, Edit, View, Help standard menus
- [x] Custom menu items via fluent API (`.WithMenuBar()` builder + `MenuItemDefinition` factory methods)
- [x] Keyboard accelerators display text (e.g. "Ctrl+S" — display-only, actual binding via `IHotkeyService`)
- [x] Menu item enable/disable states (`EnableMenuItem()`)
- [x] Checkable menu items with toggle (`CheckMenuItem()`, `CreateCheckable()`)
- [x] Separator support
- [x] Submenu support (nested `CreateSubMenu()`)
- [x] Win32 mnemonic characters (`&` prefix, Alt+F opens File menu)
- [x] `IMenuBarService` interface with `IsSupported` for cross-platform detection
- [x] `MenuItemClicked` event with string ID
- [x] Platform backend architecture (`IMenuBarBackend` → Windows/Null)
- [x] WndProc subclassing on Photino HWND for WM_COMMAND handling
- [x] Proper dispose order (restore WndProc first, then destroy menu)
- [x] Demo panel in DesktopFeatures sample with toggle/enable controls and event log
- [ ] Linux GTK menu bar integration (requires Photino widget hierarchy access)
- [ ] macOS native menu bar integration

### Multi-Window Support - DONE (v2.1.0)
- [x] `IWindowService` singleton for creating additional windows
- [x] Methods: `CreateWindowAsync()`, `CreateModalAsync()`, `CloseWindowAsync()`, `GetWindows()`
- [x] Window content via URL path (`WindowOptions.FromUrl("/settings")`) or component type (`WindowOptions.FromComponent<T>()`)
- [x] `ModalResult` with `Confirmed`, `Data`, `GetData<T>()`, factory methods `Ok()` / `Cancel()`
- [x] `CompleteModal()` for modal components to return data to the caller
- [x] Window-to-window communication via `SendMessage()`, `BroadcastMessage()`, `MessageReceived` event
- [x] Modal dialog support with `TaskCompletionSource<ModalResult>` (awaitable)
- [x] Platform backend architecture (`IModalBackend` → Windows/Null)
- [x] Win32 `EnableWindow` for true modal behavior (parent disabled while modal open)
- [x] `SetForegroundWindow` to restore parent focus after modal closes
- [x] Each child window on STA background thread with independent Blazor circuit
- [x] `WindowHost.razor` (`/_cheapblazor/window`) for DynamicComponent rendering
- [x] `WindowCreated` / `WindowClosed` events
- [x] Demo panel in DesktopFeatures sample with child window, modal dialog, and messaging
- [ ] Linux/macOS modal behavior (GTK/Cocoa parent disable)
- [ ] Window positioning relative to parent (center-on-parent calculation)

### Drag-and-Drop Files (Blazor Exposed)
- [x] Expose JS drag-and-drop to Blazor components via `IDragDropService`
- [x] `IDragDropService` singleton with `FilesDropped`, `DragEnter`, `DragLeave` events
- [x] Multiple file support (file metadata: name, size, type, lastModified)
- [x] Drag-over visual feedback via `IsDragOver` property and `DragEnter`/`DragLeave` events
- [x] Auto-initialized JS bridge via Photino message channel
- [x] Demo panel in DesktopFeatures sample
- [ ] File path extraction via native backend (Win32 `IDropTarget` / `DragAcceptFiles`) — V2

---

## Priority 3 - Advanced Features

### Auto-Updater
- [ ] Check for updates from GitHub releases or custom endpoint
- [ ] Download update in background
- [ ] Apply update on restart
- [ ] Version comparison logic
- [ ] Update notification UI helper
- [ ] Consider Squirrel.Windows / Velopack integration

### Plugin System
- [ ] Define plugin interface contract
- [ ] Plugin discovery from designated folder
- [ ] Plugin lifecycle (load, unload, reload)
- [ ] Sandboxed execution considerations
- [ ] Plugin settings integration

### AOT Compilation Support
- [ ] Test and validate Native AOT builds
- [ ] Document trimming requirements
- [ ] Identify reflection-heavy code paths
- [ ] Create AOT-compatible build profile

### Project Templates - DONE (v2.1.0)
- [x] `dotnet new cheapblazor` template (minimal — MudBlazor + basic window)
- [x] `dotnet new cheapblazor-full` template (all features enabled)
- [x] NuGet template package (`CheapAvaloniaBlazor.Templates`)
- [x] `sourceName` substitution (project name, namespaces, titles)
- [x] Both templates build with 0 warnings, 0 errors

---

## Optimizations

### Startup Performance
- [ ] Profile cold start time
- [ ] Identify slow initialization paths
- [ ] Lazy service initialization where possible
- [ ] Reduce 30-second timeout if startup is consistently fast

### Memory Profiling
- [ ] Establish baseline memory usage
- [ ] Add benchmarks for common operations
- [ ] Document memory characteristics in README

### Lazy Component Loading
- [ ] Investigate Blazor lazy loading patterns
- [ ] Document how users can implement for their apps

---

## UI Framework Integrations

### Planned Support
- [ ] **Tailwind CSS** - Utility-first CSS framework
- [ ] **Radzen Blazor** - Rich component library
- [ ] **Telerik UI for Blazor** - Enterprise components
- [ ] **Bootstrap Blazor** - Bootstrap-based components
- [ ] **Fluent UI Blazor** - Microsoft Fluent design system
- [ ] **Ant Design Blazor** - Ant Design components

### Integration Pattern
Each UI framework integration should:
- [ ] Have a dedicated `.AddXxx()` extension method
- [ ] Auto-configure required services
- [ ] Include CSS/JS in proper order
- [ ] Document any conflicts with other frameworks

---

## Testing & Quality

### Testing Infrastructure
- [ ] Unit test project setup
- [ ] Integration test patterns
- [ ] BUnit tests for any Blazor components
- [ ] Mock services for `IDesktopInteropService`

### Cross-Platform Validation
- [ ] Linux testing (Ubuntu, Fedora)
- [ ] macOS testing (Intel, Apple Silicon)
- [x] Document platform-specific quirks (cross-platform compatibility matrix in README)
- [ ] CI/CD for multi-platform builds

---

## Documentation

- [ ] API reference documentation
- [x] Code examples (README updated with all features, usage snippets, and sample app descriptions)
- [ ] Video tutorial
- [ ] Migration guide for version upgrades
- [x] Troubleshooting FAQ expansion (DevTools/taskbar issue, platform compatibility, common issues)

---

## Notes

**Last Updated**: 2026-02-08

**Versioning**: This TODO applies to v2.x and beyond. Core architecture is stable.

**Contributing**: If picking up any of these items, create an issue first to discuss approach.
