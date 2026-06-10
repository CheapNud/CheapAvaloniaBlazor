<!--
  TODO.md — CheapAvaloniaBlazor project work tracker
  Last updated: 2026-06-10

  RULES FOR AI AGENTS:
  - Update the "Last updated" date above whenever you modify this file
  - Items use checkbox format: - [ ] incomplete, - [x] complete
  - Never remove completed items — they serve as history. Move them to "## Done" when a category gets cluttered.
  - Each item gets ONE line. Details go in sub-bullets indented with 2 spaces.
  - Prefix each item with the date it was added: - [ ] (2026-03-17) Description
  - When completing, change to: - [x] (2026-03-17 → 2026-03-18) Description
  - Tag the SOURCE of each item at the end in brackets:
      [code-todo] = from // TODO comment in source code
      [plan] = from a plan document or planning session
      [bug] = from a bug encountered during dev/deploy
      [audit] = from a code audit or review
      [user] = explicitly requested by the user
  - For [code-todo] items, ALWAYS include file:line reference so devs can navigate directly
  - Categories: Blocking, Planned, Future, Done
  - New items go at the TOP of their category
  - Do not create separate TODO_*.md files — everything goes here
  - Keep it terse. If it needs more than 3 sub-bullets, link to a plan document.
-->

# TODO

## Blocking

_Nothing blocking._

## Planned

- [ ] (2026-06-10) Template smoke tests in CI — install packed nupkg, scaffold both templates, build against local source [plan]
  - Runs on PRs too (pack inside the job; existing pack job is push-only)
- [ ] (2026-06-10) `samples/TemplateApp` — checked-in `dotnet new cheapblazor` output, CI diffs scaffold against it to prevent drift [plan]
- [ ] (2026-06-10) README slim-down to ~100-line landing page, feature deep-dives move to Docs/ [plan]
- [ ] (2026-06-10) Wiki sync — mirror top-level `Docs/*.md` to GitHub wiki on master push, `Docs/README.md` as Home [plan]
- [ ] (2026-06-10) Add `PackageReadmeFile` to templates csproj — nuget.org warns readme missing [bug]
- [ ] (2026-03-17) Fork Photino.NET under CheapNud → CheapPhotino [plan]
  - Add `GetCookiesAsync` (cookie manager) + native `ExecuteScriptAsync`
  - See `TODO_PHOTINO_FORK.md` for native C++ changes per platform
- [ ] (2026-03-17) Wire `ICookieService` to Photino cookie manager after fork [plan]
  - `Services/CookieService.cs` — currently returns empty, waiting for Photino API
  - See `TODO_COOKIE_SERVICE.md` for architecture
- [ ] (2026-02-08) Linux hotkey testing: verify D-Bus portal on KDE/GNOME/Hyprland [plan]
- [ ] (2026-02-08) Linux hotkey testing: verify X11 backend on X11 sessions [plan]
- [ ] (2026-02-08) Linux hotkey testing: verify D-Bus → X11 fallback chain [plan]
- [ ] (2026-02-08) Linux GTK menu bar integration [plan]
- [ ] (2026-02-08) macOS native menu bar integration [plan]
- [ ] (2026-02-08) Linux/macOS modal behavior (GTK/Cocoa parent disable) [plan]
- [ ] (2026-02-08) Window positioning relative to parent (center-on-parent calculation) [plan]
- [ ] (2026-02-08) File path extraction via native backend (Win32 IDropTarget / DragAcceptFiles) [plan]

## Future

- [ ] (2026-02-08) Auto-updater — GitHub releases check, background download, apply on restart
- [ ] (2026-02-08) Plugin system — interface contract, discovery, lifecycle, sandboxing
- [ ] (2026-02-08) AOT compilation support — test Native AOT, document trimming requirements
- [ ] (2026-02-08) Startup performance profiling — cold start time, lazy service init
- [ ] (2026-02-08) Memory profiling — baseline, benchmarks
- [ ] (2026-02-08) Lazy Blazor component loading patterns
- [ ] (2026-02-08) UI framework integrations — Tailwind, Radzen, Telerik, Fluent UI, Ant Design
- [ ] (2026-02-08) Unit + integration + BUnit tests
- [ ] (2026-02-08) Cross-platform CI/CD for multi-platform builds
- [ ] (2026-02-08) API reference docs + video tutorial + migration guide

## Known Technical Debt

**Environment hardcoded to "Development"** — `Services/EmbeddedBlazorHostService.cs:~105`
Desktop apps need Development mode for `UseStaticWebAssets()` to resolve NuGet static assets. Production mode expects physically published wwwroot files. Since this is localhost-only, irrelevant.

**blazor.web.js serving** — `Utilities/BlazorFrameworkExtractor.cs`, `Build/CheapAvaloniaBlazor.targets`
.NET 10 ships `blazor.web.js` in an Internal.Assets NuGet package. MSBuild targets only register it for `Microsoft.NET.Sdk.Web` with `OutputType=Exe`. Desktop apps use `WinExe` or `Microsoft.NET.Sdk.Razor`. Runtime extraction from NuGet global packages folder is the workaround.

## Done

- [x] (2026-06-10 → 2026-06-10) v3.1.2 release — templates package now published to NuGet for the first time (closes #38) [bug]
- [x] (2026-06-10 → 2026-06-10) CI packs `CheapAvaloniaBlazor.Templates` alongside the library in nuget-publish.yml [bug]
- [x] (2026-06-10 → 2026-06-10) Getting started guide fixes — missing `CheapAvaloniaBlazor.Extensions` using (#40), missing project namespace imports causing CS1662 (#41), stale version pins in README/getting-started/installation (#39) [bug]
- [x] (2026-06-10 → 2026-06-10) Dependency updates — Avalonia 12.0.4, MudBlazor 9.5.0, Tmds.DBus.Protocol 0.94.1 [user]
- [x] (2026-02-08 → 2026-02-08) System tray support (v2.1.0) [plan]
- [x] (2026-02-08 → 2026-02-08) Dual-channel notifications (v2.1.0) [plan]
- [x] (2026-02-08 → 2026-02-08) Settings persistence helper (v2.1.0) [plan]
- [x] (2026-02-08 → 2026-02-08) App lifecycle events (v2.1.0) [plan]
- [x] (2026-02-08 → 2026-02-08) Theme detection (v2.1.0) [plan]
- [x] (2026-02-08 → 2026-02-08) Global hotkeys — Win32 + Linux D-Bus/X11 (v2.1.0) [plan]
- [x] (2026-02-08 → 2026-02-08) Native menu bar — Win32 WndProc subclassing (v2.1.0) [plan]
- [x] (2026-02-08 → 2026-02-08) Multi-window support + modal dialogs (v2.1.0) [plan]
- [x] (2026-02-08 → 2026-02-08) Drag-and-drop files via JS bridge (v2.1.0) [plan]
- [x] (2026-02-08 → 2026-02-08) Project templates — `dotnet new cheapblazor` + `cheapblazor-full` (v2.1.0) [plan]
- [x] (2026-02-08 → 2026-02-08) Cross-platform compatibility matrix in README [plan]
- [x] (2026-02-08 → 2026-02-08) Troubleshooting FAQ expansion [plan]
