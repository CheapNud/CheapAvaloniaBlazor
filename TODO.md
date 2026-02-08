# CheapAvaloniaBlazor - TODO & Roadmap

## Known Technical Debt

### DO NOT TOUCH - Environment Hack in EmbeddedBlazorHostService.cs

**Status**: WORKING - DO NOT "FIX"

**Location**: `Services/EmbeddedBlazorHostService.cs`

**The Issue**: Environment is hardcoded to "Development" for `UseStaticWebAssets()` to work properly.

**Why It's Like This**: Multiple sessions were spent attempting to properly configure static assets for Production/Release builds. Every "proper" solution resulted in `blazor.web.js` 404 errors and days of debugging. The framework files simply would not resolve correctly outside of Development environment.

**What Was Tried**:
- Embedded resources approach
- Custom static file providers
- Manual file copying in build targets
- Various combinations of `UseStaticWebAssets()` and `UseStaticFiles()`

**Result**: All attempts failed. The current hack works reliably across all scenarios.

**Future Consideration**: Only revisit this when:
1. Microsoft changes how Blazor static assets work
2. A proven solution exists in the wild
3. You have infinite time and patience (you don't)

---

## Priority 1 - Essential Desktop Features

### System Tray Support - DONE (v2.0.0)
- [x] Integrate Avalonia's `TrayIcon` / `NativeMenu` APIs
- [x] Add `ISystemTrayService` interface
- [x] Methods: `ShowTrayIcon()`, `HideTrayIcon()`, `SetTrayIcon()`, `SetTrayTooltip()`
- [x] Support for context menu on tray icon (submenus, checkable items, separators, async handlers)
- [x] "Minimize to tray" / "Close to tray" options in `CheapAvaloniaBlazorOptions`
- [x] Tray icon click events (single click, double click)
- [x] Fluent builder: `EnableSystemTray()`, `CloseToTray()`, `WithTrayTooltip()`, `WithTrayIcon()`
- [x] Window hide/show via user32.dll P/Invoke (Windows), minimize fallback (Linux/macOS)
- [x] Fallback icon generation (16x16 WriteableBitmap) when no icon path provided

### Dual-Channel Notifications - DONE (v2.0.2)
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

### App Lifecycle Events - DONE (v2.2.0)
- [x] `OnClosing` event with cancellation support (prevent close, confirm dialogs)
- [x] `OnMinimized` event
- [x] `OnMaximized` event
- [x] `OnRestored` event
- [x] `OnActivated` / `OnDeactivated` (window focus)
- [x] Expose via dedicated `IAppLifecycleService` singleton
- [x] Read-only state properties: `IsMinimized`, `IsMaximized`, `IsFocused`
- [x] Wired to Photino native window events in `BlazorHostWindow`
- [x] Demo panel in DesktopFeatures sample with event log

### Theme Detection - DONE (v2.2.0)
- [x] Detect OS dark/light mode preference via Avalonia's `ActualThemeVariant`
- [x] `IThemeService` interface with `CurrentTheme`, `IsDarkMode`, `ThemeChanged` event
- [x] `SystemTheme` enum (`Light`, `Dark`)
- [x] Runtime theme change tracking via `ActualThemeVariantChanged`
- [x] Auto-apply to MudBlazor: "Follow System Theme" toggle in DesktopFeatures sample
- [x] Demo panel with theme state display and change log

---

## Priority 2 - Enhanced Desktop Experience

### Global Hotkeys - DONE (v2.3.0)
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

### Native Menu Bar
- [ ] File, Edit, View, Help standard menus
- [ ] Custom menu items via fluent API
- [ ] Keyboard accelerators (Ctrl+S, Ctrl+O, etc.)
- [ ] Menu item enable/disable states
- [ ] Separator support
- [ ] Submenu support

### Multi-Window Support
- [ ] `IWindowService` for creating additional windows
- [ ] Methods: `CreateWindowAsync()`, `CloseWindowAsync()`, `GetWindows()`
- [ ] Window-to-window communication
- [ ] Modal dialog support
- [ ] Window positioning relative to parent

### Drag-and-Drop Files (Blazor Exposed)
- [ ] Expose existing JS drag-and-drop to Blazor components
- [ ] `IDropZoneService` or component
- [ ] File path extraction from drop events
- [ ] Multiple file support
- [ ] Drag-over visual feedback helpers

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

### Project Templates
- [ ] `dotnet new cheapblazor` template
- [ ] Minimal template (bare bones)
- [ ] Full template (with sample pages)
- [ ] Template with MudBlazor pre-configured
- [ ] NuGet template package

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

**Last Updated**: 2026-02-07

**Versioning**: This TODO applies to v2.x and beyond. Core architecture is stable.

**Contributing**: If picking up any of these items, create an issue first to discuss approach.
