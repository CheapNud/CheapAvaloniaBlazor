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

### System Tray Support
- [ ] Integrate Avalonia's `TrayIcon` / `NativeMenu` APIs
- [ ] Add `ISystemTrayService` interface
- [ ] Methods: `ShowTrayIcon()`, `HideTrayIcon()`, `SetTrayIcon()`, `SetTrayTooltip()`
- [ ] Support for context menu on tray icon
- [ ] "Minimize to tray" option in `CheapAvaloniaBlazorOptions`
- [ ] Tray icon click events (single click, double click)

### Settings Persistence Helper
- [ ] Add `ISettingsService` interface
- [ ] JSON-based storage in app data folder
- [ ] Methods: `GetAsync<T>()`, `SetAsync<T>()`, `DeleteAsync()`, `ExistsAsync()`
- [ ] Auto-save on change option
- [ ] Type-safe settings with generics
- [ ] Default values support

### App Lifecycle Events
- [ ] `OnClosing` event with cancellation support (prevent close, confirm dialogs)
- [ ] `OnMinimized` event
- [ ] `OnMaximized` event
- [ ] `OnRestored` event
- [ ] `OnActivated` / `OnDeactivated` (window focus)
- [ ] Expose via `IDesktopInteropService` or dedicated `IAppLifecycleService`

### Theme Detection
- [ ] Detect OS dark/light mode preference
- [ ] `IThemeService` interface
- [ ] `GetSystemTheme()` method returning `Light`, `Dark`, or `System`
- [ ] `OnThemeChanged` event for runtime theme switches
- [ ] Auto-apply to MudBlazor theme provider

---

## Priority 2 - Enhanced Desktop Experience

### Global Hotkeys
- [ ] Register system-wide keyboard shortcuts
- [ ] `IHotkeyService` interface
- [ ] Methods: `RegisterHotkey()`, `UnregisterHotkey()`, `UnregisterAll()`
- [ ] Modifier key support (Ctrl, Alt, Shift, Win)
- [ ] Conflict detection with existing system hotkeys

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
- [ ] Document platform-specific quirks
- [ ] CI/CD for multi-platform builds

---

## Documentation

- [ ] API reference documentation
- [ ] More code examples
- [ ] Video tutorial
- [ ] Migration guide for version upgrades
- [ ] Troubleshooting FAQ expansion

---

## Notes

**Last Updated**: 2026-01-30

**Versioning**: This TODO applies to v2.x and beyond. Core architecture is stable.

**Contributing**: If picking up any of these items, create an issue first to discuss approach.
