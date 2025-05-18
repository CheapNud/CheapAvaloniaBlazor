# CheapAvaloniaBlazor

An experimental project exploring integration between **Blazor Server**, **Avalonia**, **MudBlazor**, and **Photino** to build cross-platform desktop and web UIs using a shared .NET codebase.

> ⚠️ **Early Stage Development:** This project is in the very early stages. Expect rapid changes, incomplete features, and evolving structure. Contributions and ideas are welcome!

---

## 🧩 Technologies Used

- **[Blazor Server](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)** – Web UI framework running on the server.
- **[Avalonia](https://avaloniaui.net/)** – Cross-platform UI framework for desktop applications.
- **[MudBlazor](https://mudblazor.com/)** – Material Design component library for Blazor.
- **[Photino](https://www.photino.dev/)** – Lightweight, cross-platform desktop host for web-based apps.

---

## 🚧 Project Goals

- Create a **unified UI architecture** for desktop and web platforms.
- Reuse Blazor components in both **Photino** and **Avalonia** frontends.
- Showcase the flexibility of **.NET 8** and **Blazor** for cross-platform development.

---

## Directory Structure

```
BlazorDesktopApp.Template/
├── .template.config/
│   └── template.json                   # Dotnet template configuration
├── Your existing project files...
├── BlazorDesktopApp.Template.nuspec    # NuGet package spec
├── build-template.bat                  # Build script (Windows)
├── build-template.ps1                  # Build script (PowerShell)
├── exclude.txt                         # Files to exclude from template
└── README.md
```
