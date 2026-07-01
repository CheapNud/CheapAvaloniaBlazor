# CheapAvaloniaBlazor

Build cross-platform desktop applications using Blazor Server, Avalonia, and Photino.

Combines Blazor Server + MudBlazor + Avalonia + Photino to create native desktop apps with full file system access across Windows, Linux, and macOS using familiar Razor pages and C# components.

[![NuGet](https://img.shields.io/nuget/v/CheapAvaloniaBlazor.svg)](https://www.nuget.org/packages/CheapAvaloniaBlazor)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)

> **PRE-ALPHA HOBBY PROJECT**
> This is an experimental project developed as a personal hobby. Expect breaking changes, incomplete features, and limited support. Use at your own risk in non-production environments.

## Why CheapAvaloniaBlazor?

Use your existing web development skills (HTML, CSS, Blazor, C#) to build desktop applications with native capabilities — no platform-specific UI frameworks, no complex native interop. Good fits: web developers moving to desktop, rapid prototyping, line-of-business tools needing file system access, and Blazor apps that outgrew the browser sandbox.

## Quick Start

```bash
# Install the project templates
dotnet new install CheapAvaloniaBlazor.Templates

# Create a minimal app (MudBlazor + basic window)
dotnet new cheapblazor -n MyDesktopApp

# Or a full-featured app (tray, notifications, settings, hotkeys, menu bar, multi-window, drag-drop)
dotnet new cheapblazor-full -n MyDesktopApp

cd MyDesktopApp
dotnet run
```

Prefer manual setup? Add the package to a Razor SDK project and call the fluent builder:

```csharp
using CheapAvaloniaBlazor.Extensions;
using CheapAvaloniaBlazor.Hosting;

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

The **[Getting Started guide](./Docs/getting-started.md)** walks through every file of a working app.

## Features

- **System tray** — icon, context menu, minimize/close-to-tray
- **Notifications** — desktop toasts (Avalonia overlay) + OS notification center (Web Notification API)
- **Settings persistence** — JSON key-value and typed-section APIs with auto-save
- **App lifecycle events** — minimize/maximize/restore/focus events, close cancellation
- **Theme detection** — OS dark/light mode with runtime change events
- **Global hotkeys** — system-wide shortcuts on Windows (Win32) and Linux (D-Bus portal / X11)
- **Native menu bar** — Win32 menu with mnemonics, accelerators, checkable items (Windows)
- **Multi-window** — child windows, modal dialogs, inter-window messaging
- **Drag-and-drop files** — HTML5 drag events bridged to C#
- **Desktop interop** — file dialogs, window management, clipboard, system paths
- **Splash screen** — enabled by default, fully customizable
- **Diagnostics** — comprehensive startup and operation logging

Every feature with code samples: **[Features guide](./Docs/features.md)**.

## Documentation

- **[Installation](./Docs/installation.md)** — requirements, installation methods, troubleshooting
- **[Getting Started](./Docs/getting-started.md)** — step-by-step first app tutorial
- **[Features](./Docs/features.md)** — all desktop features with code samples
- **[Desktop Interop API](./Docs/desktop-interop.md)** — file dialogs, window management, clipboard
- **[Advanced Configuration](./Docs/advanced-configuration.md)** — full HostBuilder reference
- **[Splash Screen](./Docs/splash-screen.md)** — customization options
- **[Diagnostics & Debugging](./Docs/diagnostics.md)** — logging and common issues
- **[Architecture](./Docs/architecture.md)** — how the pieces fit together, platform compatibility

Also browsable on the **[project wiki](https://github.com/CheapNud/CheapAvaloniaBlazor/wiki)**.

## Samples

- **[MinimalApp](./samples/MinimalApp)** — the absolute minimum code to run a Blazor desktop app
- **[DesktopFeatures](./samples/DesktopFeatures)** — every desktop feature demonstrated
- **[TemplateApp](./samples/TemplateApp)** — exactly what `dotnet new cheapblazor` scaffolds (CI-verified)
- **[CheapShotcutRandomizer](https://github.com/CheapNud/CheapShotcutRandomizer)** — a real-world app built on the framework

## Contributing & Support

Found a bug? [Open an issue](https://github.com/CheapNud/CheapAvaloniaBlazor/issues). Testing on Linux/macOS and documentation feedback are especially welcome. This is a hobby project — support is best-effort.

## License

MIT — use freely in personal and commercial projects. Built on [Avalonia](https://avaloniaui.net/), [Blazor](https://blazor.net/), [MudBlazor](https://mudblazor.com/), and [Photino](https://www.tryphotino.io/).
