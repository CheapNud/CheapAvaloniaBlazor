# 🚀 CheapAvaloniaBlazor

**Build cross-platform desktop applications with the web development stack you already know.**

Combine **Blazor Server** + **Your Choice of UI Framework** (currently **MudBlazor**, more options coming) + **Avalonia** + **Photino** to create native desktop apps with full file system access across Windows, Linux, and macOS - using familiar Razor pages and C# components.

[![NuGet](https://img.shields.io/nuget/v/CheapAvaloniaBlazor.svg)](https://www.nuget.org/packages/CheapAvaloniaBlazor)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)

> 🚧 **PRE-ALPHA HOBBY PROJECT** 🚧  
> This is an experimental project developed as a personal hobby. Expect breaking changes, incomplete features, and limited support. Use at your own risk in non-production environments.

---

## ✨ Why CheapAvaloniaBlazor?

**The Problem:** Building cross-platform desktop apps traditionally requires learning different UI frameworks for each platform or dealing with complex native interop.

**The Solution:** Use your existing web development skills (HTML, CSS, Blazor, C#) to build real desktop applications with native capabilities.

### 🎯 Perfect For:
- **Web developers** transitioning to desktop development
- **Rapid prototyping** of desktop applications
- **Line-of-business apps** requiring native file system access
- **Cross-platform tools** that need to run on Windows, Linux, and macOS
- **Blazor developers** wanting to break free from browser limitations
- **Teams wanting UI framework flexibility** (MudBlazor now, more options coming)

---

## 📚 Documentation

**Complete documentation available in the [docs](./docs) folder:**

- **[Installation Guide](./docs/installation.md)** - System requirements, installation methods, verification, and troubleshooting
- **[Getting Started](./docs/getting-started.md)** - Step-by-step tutorial for your first desktop app
- **[Splash Screen](./docs/splash-screen.md)** - Professional splash screen configuration and customization
- **[Desktop Interop API](./docs/desktop-interop.md)** - File dialogs, window management, system integration, clipboard operations
- **[Diagnostics & Debugging](./docs/diagnostics.md)** - Diagnostic system, logging, and troubleshooting
- **[Advanced Configuration](./docs/advanced-configuration.md)** - HostBuilder API reference, SignalR, hot reload, custom pipeline

---

## 📦 Quick Installation

```bash
# Create a new console project
dotnet new console -n MyDesktopApp
cd MyDesktopApp

# Add CheapAvaloniaBlazor package
dotnet add package CheapAvaloniaBlazor
```

**More installation options:** See the **[Installation Guide](./docs/installation.md)** for Visual Studio GUI instructions, Avalonia templates, and troubleshooting.

---

## 🚀 Quick Start

### Minimal Example (3 Steps)

**1. Update your `.csproj` to use Web SDK:**
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="CheapAvaloniaBlazor" Version="1.1.0" />
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

**3. Add Blazor components** (App.razor, MainLayout.razor, Index.razor, etc.)

**Full tutorial:** See the **[Getting Started Guide](./docs/getting-started.md)** for complete step-by-step instructions with all files.

---

## 🎨 Features

### Professional Splash Screen (v1.1.0)
**Enabled by default** - Shows a professional loading screen while your app initializes.

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

// File dialogs, window management, clipboard, notifications, and more
var file = await Desktop.OpenFileDialogAsync();
await Desktop.ShowNotificationAsync("Title", "Message");
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

## 🔧 Configuration

### HostBuilder Fluent API
Configure your application with an intuitive fluent interface:

```csharp
new HostBuilder()
    // Window
    .WithTitle("My App").WithSize(1400, 900).Chromeless(false)

    // Splash Screen
    .WithSplashScreen("My App", "Loading...")

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

## 📂 Project Structure

```
MyDesktopApp/
├── Program.cs                 # Application entry point
├── App.razor                  # Blazor router configuration
├── Components/
│   └── _Host.cshtml          # Blazor host page (contains full HTML)
├── Pages/
│   ├── Index.razor           # Home page
│   └── Files.razor           # File management page
├── Shared/
│   └── MainLayout.razor      # Main application layout
├── wwwroot/                  # Static web assets
│   └── css/
└── Services/                 # Your business logic
    └── IMyService.cs
```

---

## 🔌 Architecture & Integration

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
- **UI Layer**: Blazor Server + Razor Pages + MudBlazor components
- **Desktop Framework**: Avalonia (cross-platform window management)
- **WebView Host**: Photino.NET (native webview embedding)
- **Backend**: ASP.NET Core (dependency injection, services, middleware)
- **Interop**: Custom desktop services (file dialogs, notifications, etc.)

---

## 🛠️ Build & Deployment

### Development
```bash
# Command Line / VS Code Users
dotnet run

# Visual Studio Users
Press F5 or Debug → Start Debugging

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

### Distribution
```bash
# Windows
MyDesktopApp.exe

# Linux
./MyDesktopApp

# macOS
./MyDesktopApp
```

---

## 📋 System Requirements

### Runtime Requirements
- **.NET 10.0** or later
- **Windows 10+** ✅ **(Tested)**
- **Linux with WebKit** ⚠️ **(Untested - on roadmap)**
- **macOS 10.15+** ⚠️ **(Untested - on roadmap)**

### Development Requirements
- **Visual Studio 2022** (17.8+) or **VS Code**
- **.NET 10.0 SDK**
- **C# 13** language features

### Package Dependencies
- `Avalonia 11.3.7+` - Desktop framework
- `MudBlazor 8.13.0+` - Material Design components
- `Photino.NET 4.0.16+` - WebView hosting
- `Microsoft.AspNetCore.Components.Web 10.0+` - Blazor components

---

## 🐛 Troubleshooting

### Common Issues & Solutions

**🚫 Build Errors**
```bash
# Ensure correct .NET version
dotnet --version  # Should be 10.0+

# Clear and restore packages
dotnet clean
dotnet restore
dotnet build
```

**🚫 Window Doesn't Appear**
- Check if port 5000/5001 is available
- Verify no firewall blocking local connections
- Look for exceptions in console output
- Try different port: `builder.UsePort(8080)`

**🚫 MudBlazor Styles Missing**
- Verify CSS reference in `_Layout.cshtml`:
  ```html
  <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
  ```
- Check browser dev tools for 404 errors
- Ensure `AddMudBlazor()` is called in HostBuilder

**🚫 Platform Compatibility Issues**
- **Linux/macOS**: Currently untested - if you encounter issues, please report them!
- **Windows**: Fully tested and supported
- Dependencies (Avalonia, Photino) should work cross-platform, but integration not verified

**🚫 Visual Studio Specific Issues**
- **IntelliSense not working**: Rebuild solution (Build → Rebuild Solution)
- **Razor syntax errors**: Install latest "ASP.NET and web development" workload
- **Package restore issues**: Tools → NuGet Package Manager → "Clear All NuGet Cache(s)"
- **Hot reload not working**: Enable "Hot Reload on File Save" in Debug settings

**🚫 File Dialog Not Working**
- ✅ **Fixed in v1.0.67+** - File dialogs now work via Avalonia StorageProvider
- Ensure you're using latest version: `dotnet add package CheapAvaloniaBlazor`
- Check `IDesktopInteropService` injection
- If still having issues, please report - architecture was completely rebuilt for file dialog support

**🚫 Hot Reload Not Working**
- Restart application
- Check VS/VS Code Blazor extensions
- Verify project targets .NET 10.0

### Debug Mode
```csharp
var builder = new HostBuilder()
    .EnableConsoleLogging(true)  // Enable detailed logging
    .ConfigureOptions(options => 
    {
        options.EnableDevTools = true;  // Enable browser dev tools
    });
```

---

## 🎯 Example Applications

### File Manager
```csharp
// Complete file browser with MudBlazor TreeView
@inject IDesktopInteropService Desktop

<MudTreeView Items="FileNodes" @bind-SelectedValue="SelectedFile">
    <ItemTemplate>
        <MudTreeViewItem @bind-Expanded="@context.IsExpanded" 
                         Value="@context" 
                         Icon="@(context.IsDirectory ? Icons.Material.Filled.Folder : Icons.Material.Filled.InsertDriveFile)">
            @context.Name
        </MudTreeViewItem>
    </ItemTemplate>
</MudTreeView>
```

### System Monitor
```csharp
// Real-time system information dashboard
<MudGrid>
    <MudItem xs="12" md="6">
        <MudCard>
            <MudCardContent>
                <MudText Typo="Typo.h6">CPU Usage</MudText>
                <MudProgressLinear Value="@cpuUsage" Color="Color.Primary" />
            </MudCardContent>
        </MudCard>
    </MudItem>
</MudGrid>
```

### Database Browser
```csharp
// SQLite database browser with data grid
<MudDataGrid Items="@DatabaseRecords" Filterable="true" SortMode="SortMode.Multiple">
    <Columns>
        <PropertyColumn Property="x => x.Id" Title="ID" />
        <PropertyColumn Property="x => x.Name" Title="Name" />
        <PropertyColumn Property="x => x.CreatedDate" Title="Created" />
    </Columns>
</MudDataGrid>
```

---

## 🚨 Project Status & Roadmap

### Current Status: **Working Alpha v1.1.0** ✅
- ✅ **Core Framework**: Avalonia + Blazor + Photino integration
- ✅ **NuGet Package**: Published and functional
- ✅ **Professional Splash Screen**: Enabled by default, fully customizable (v1.1.0)
- ✅ **File System Interop**: **WORKING** - Cross-platform file dialogs via Avalonia StorageProvider
- ✅ **Window Management**: Minimize, maximize, resize, title changes
- ✅ **JavaScript ↔ C# Bridge**: Full bidirectional communication with ExecuteScriptAsync
- ✅ **MudBlazor Integration**: Full component library support
- ✅ **Clean Architecture**: Constants extraction (121+ magic strings), DiagnosticLogger abstraction
- ✅ **Performance Optimizations**: ValueTask for zero-allocation synchronous operations

### Upcoming Features 🛣️
- 🔄 **Testing Framework**: Unit and integration test support  
- 🔄 **Cross-Platform Testing**: Full compatibility validation on Linux and macOS
- 🔄 **Alternative WebView Hosts**: Additional options beyond Photino.NET
- 🔄 **Alternative UI Frameworks**: Support for Radzen, Telerik, Bootstrap, and other Blazor component libraries
- 🔄 **Enhanced Documentation**: More examples and tutorials
- 🔄 **Performance Optimization**: Startup time and memory usage
- 🔄 **Plugin System**: Extensible architecture
- 🔄 **Visual Designer**: Drag-and-drop UI builder
- 🔄 **Package Templates**: `dotnet new` project templates

### Known Limitations ⚠️
- **Alpha stage project** - some breaking changes possible but architecture now stable
- **Currently tested on Windows only** - Linux and macOS compatibility validation in progress
- **MudBlazor-focused currently** - other UI framework integrations planned
- **Community support** - best-effort basis with active development
- Testing infrastructure in development

---

## 🤝 Contributing & Support

### Project Status
**This is a personal hobby project in alpha stage.** The core architecture is now stable (v1.0.67+ with working file dialogs), though some features are still evolving. Limited pull requests accepted for bug fixes and documentation improvements.

### How to Help
- 🐛 **Report Issues**: Found a bug? [Create an issue](https://github.com/CheapNud/CheapAvaloniaBlazor/issues)
- 💬 **Provide Feedback**: Share your experience and suggestions  
- 🧪 **Testing**: Try it in your projects and report compatibility issues
- 📖 **Documentation**: Suggest improvements to examples and guides

### Getting Support
- **GitHub Issues**: Technical problems and bug reports
- **Discussions**: Questions and community help  
- **Documentation**: Check this README and inline code comments
- **Expectations**: This is a hobby project - support is provided on a best-effort basis

---

## 📄 License & Attribution

**MIT License** - Use freely in personal and commercial projects.

### Built With ❤️ Using:
- [Avalonia](https://avaloniaui.net/) - Cross-platform .NET desktop framework
- [Blazor](https://blazor.net/) - Build interactive web UIs using C#
- [MudBlazor](https://mudblazor.com/) - Material Design component library (current default)
- [Photino](https://www.tryphotino.io/) - Lightweight cross-platform WebView

*Future integrations planned: Radzen, Telerik, Bootstrap, Blazorise, and more!*

### 🐧 Special Thanks
Documentation analysis and enhancement by **Kowalski** - the analytical penguin who never met a codebase he couldn't optimize! 🤓

---

## 🎉 Get Started Today!

Ready to build your first cross-platform desktop app with web technologies?

```bash
# Create new project
dotnet new console -n MyFirstDesktopApp
cd MyFirstDesktopApp

# Add CheapAvaloniaBlazor
dotnet add package CheapAvaloniaBlazor

# Follow the Quick Start guide above
# Build something amazing! 🚀
```

**Questions? Issues? Ideas?** [Open an issue](https://github.com/CheapNud/CheapAvaloniaBlazor/issues) and share your feedback!

---
