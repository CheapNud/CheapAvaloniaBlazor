# 🚀 CheapAvaloniaBlazor

**Build cross-platform desktop applications with the web development stack you already know.**

Combine **Blazor Server** + **Your Choice of UI Framework** (currently **MudBlazor**, more options coming) + **Avalonia** + **Photino** to create native desktop apps with full file system access across Windows, Linux, and macOS - using familiar Razor pages and C# components.

[![NuGet](https://img.shields.io/nuget/v/CheapAvaloniaBlazor.svg)](https://www.nuget.org/packages/CheapAvaloniaBlazor)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/download/dotnet/9.0)

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

## 📦 Installation Options

### Option A: Command Line (VS Code/Terminal Users)
```bash
# Create a new console project (CheapAvaloniaBlazor handles the desktop setup)
dotnet new console -n MyDesktopApp
cd MyDesktopApp

# Add CheapAvaloniaBlazor package
dotnet add package CheapAvaloniaBlazor
```

### Option B: Visual Studio 2022 GUI Users
1. **File** → **New** → **Project**
2. Select **"Console App"** (.NET 9.0)
3. Name your project (e.g., "MyDesktopApp")
4. Right-click project → **"Manage NuGet Packages"**
5. Search for **"CheapAvaloniaBlazor"** → **Install**

### Option C: Start with Avalonia Template (Advanced)
```bash
# Install Avalonia templates first
dotnet new install Avalonia.ProjectTemplates

# Create Avalonia project, then add CheapAvaloniaBlazor
dotnet new avalonia.app -n MyDesktopApp
cd MyDesktopApp
dotnet add package CheapAvaloniaBlazor
# Note: You'll need to integrate with existing Avalonia setup
```

> 💡 **Why start with Console App?** CheapAvaloniaBlazor handles all the desktop framework setup for you! No need for complex Avalonia/Blazor project templates - just add the package and you're ready to build.

---

## 🚀 Quick Start (5 Minutes)

> **Note for Visual Studio Users:** Create folders and files using **Solution Explorer** → **Right-click project** → **Add** → **New Folder/Item**

### 1. Update Project File
Edit your `.csproj` file to use the Web SDK (includes MVC and Blazor support):
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="CheapAvaloniaBlazor" Version="1.0.7" />
  </ItemGroup>
</Project>
```

### 2. Replace Program.cs
```csharp
using CheapAvaloniaBlazor.Hosting;

namespace MyDesktopApp;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var builder = new HostBuilder()
            .WithTitle("My Desktop App")
            .WithSize(1200, 800)
            .AddMudBlazor();

        // Add your services
        builder.Services.AddScoped<IMyService, MyService>();

        // Run the app - all Avalonia complexity handled by the package
        builder.RunApp(args);
    }
}
```

### 3. Create Components/_Host.cshtml
> **Visual Studio:** Right-click project → Add → New Folder → "Components", then right-click Components → Add → New Item → "Razor Page"

```html
@page "/"
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers

<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>My Desktop App</title>
    <base href="~/" />
    
    <!-- MudBlazor CSS -->
    <link href="https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap" rel="stylesheet" />
    <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
    
    <style>
        #blazor-error-ui {
            background: lightyellow;
            bottom: 0;
            box-shadow: 0 -1px 2px rgba(0, 0, 0, 0.2);
            display: none;
            left: 0;
            padding: 0.6rem 1.25rem 0.7rem 1.25rem;
            position: fixed;
            width: 100%;
            z-index: 1000;
        }

            #blazor-error-ui .dismiss {
                cursor: pointer;
                position: absolute;
                right: 0.75rem;
                top: 0.5rem;
            }
    </style>
</head>
<body>
    <component type="typeof(App)" render-mode="ServerPrerendered" />

    <div id="blazor-error-ui">
        An error has occurred. This application may no longer respond until reloaded.
        <a href="" class="reload">Reload</a>
        <a class="dismiss">🗙</a>
    </div>

    <script src="_framework/blazor.server.js"></script>
    <script src="_content/MudBlazor/MudBlazor.min.js"></script>
</body>
</html>
```

### 4. Create App.razor
> **Visual Studio:** Right-click project → Add → New Item → "Razor Component"
```razor
<Router AppAssembly="@typeof(App).Assembly">
    <Found Context="routeData">
        <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
    </Found>
    <NotFound>
        <PageTitle>Not found</PageTitle>
        <LayoutView Layout="@typeof(MainLayout)">
            <p role="alert">Sorry, there's nothing at this address.</p>
        </LayoutView>
    </NotFound>
</Router>
```

### 5. Create Shared/MainLayout.razor
> **Visual Studio:** Right-click project → Add → New Folder → "Shared", then Add → New Item → "Razor Component"

```razor
@inherits LayoutComponentBase

<MudThemeProvider />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />

<MudLayout>
    <MudAppBar Elevation="1">
        <MudIconButton Icon="Icons.Material.Filled.Menu" Color="Color.Inherit" Edge="Edge.Start" />
        <MudSpacer />
        <MudText Typo="Typo.h6">My Desktop App</MudText>
        <MudSpacer />
        <MudIconButton Icon="Icons.Material.Filled.Settings" Color="Color.Inherit" />
    </MudAppBar>
    
    <MudDrawer Open="true" Elevation="1">
        <MudDrawerHeader>
            <MudText Typo="Typo.h6">Navigation</MudText>
        </MudDrawerHeader>
        <MudNavMenu>
            <MudNavLink Href="/" Match="NavLinkMatch.All" Icon="Icons.Material.Filled.Home">Home</MudNavLink>
            <MudNavLink Href="/files" Icon="Icons.Material.Filled.Folder">Files</MudNavLink>
        </MudNavMenu>
    </MudDrawer>
    
    <MudMainContent>
        <MudContainer MaxWidth="MaxWidth.Large" Class="my-16 pt-16">
            @Body
        </MudContainer>
    </MudMainContent>
</MudLayout>
```

### 6. Create _Imports.razor
> **Visual Studio:** Right-click project → Add → New Item → "Razor Component" → Name it "_Imports.razor"
```razor
@using System.Net.Http
@using System.Net.Http.Json
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Components.Web.Virtualization
@using Microsoft.JSInterop
@using MudBlazor
@using CheapAvaloniaBlazor
@using CheapAvaloniaBlazor.Services
```

### 7. Create Pages/Index.razor
> **Visual Studio:** Right-click project → Add → New Folder → "Pages", then Add → New Item → "Razor Component"
```razor
@page "/"
@inject IDesktopInteropService Desktop

<PageTitle>Home</PageTitle>

<MudCard>
    <MudCardContent>
        <MudText Typo="Typo.h4" GutterBottom="true">Welcome to Your Desktop App! 🎉</MudText>
        <MudText Class="mb-4">
            This is a native desktop application built with Blazor and running with full file system access.
        </MudText>
        
        <MudButton Variant="Variant.Filled" Color="Color.Primary" @onclick="OpenFileDialog">
            Open File Dialog
        </MudButton>
        
        <MudButton Variant="Variant.Filled" Color="Color.Secondary" @onclick="ShowNotification" Class="ml-2">
            Show Notification
        </MudButton>
    </MudCardContent>
</MudCard>

@code {
    private async Task OpenFileDialog()
    {
        var file = await Desktop.OpenFileDialogAsync(new()
        {
            Title = "Select a file",
            Filters = new()
            {
                new() { Name = "Text Files", Extensions = new[] { "*.txt", "*.md" } },
                new() { Name = "All Files", Extensions = new[] { "*.*" } }
            }
        });
        
        if (file != null)
        {
            await Desktop.ShowNotificationAsync("File Selected", $"You selected: {Path.GetFileName(file)}");
        }
    }
    
    private async Task ShowNotification()
    {
        await Desktop.ShowNotificationAsync("Hello Desktop!", "This is a native desktop notification! 🚀");
    }
}
```

---

## 🔧 Advanced Configuration

### HostBuilder Fluent API
```csharp
var builder = new HostBuilder()
    .WithTitle("Advanced Desktop App")
    .WithSize(1400, 900)
    .WithPosition(100, 100)           // Custom window position
    .UsePort(5001)                    // Custom port
    .UseHttps(true)                   // Enable HTTPS
    .EnableConsoleLogging(true)       // Enable console logging
    .AddMudBlazor(config =>           // Current: MudBlazor support
    {
        config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomLeft;
    })
    // .AddRadzen()                   // Coming: Radzen components
    // .AddTelerik()                  // Coming: Telerik UI
    // .AddBootstrap()                // Coming: Bootstrap components
    .AddHttpClient("API", client =>   // Named HTTP client
    {
        client.BaseAddress = new Uri("https://api.example.com");
    })
    .ConfigureOptions(options =>      // Advanced options
    {
        options.EnableDevTools = true;
        options.EnableContextMenu = false;
        options.Resizable = true;
    });

// Add custom services
builder.Services.AddSingleton<IDataService, DataService>();
builder.Services.AddScoped<IFileManager, FileManager>();

var window = builder.Build();
window.Run();
```

### Desktop Interop Features
```csharp
@inject IDesktopInteropService Desktop

// File System Operations
var selectedFile = await Desktop.OpenFileDialogAsync();
var saveLocation = await Desktop.SaveFileDialogAsync();
var folder = await Desktop.OpenFolderDialogAsync();

// File I/O
var content = await Desktop.ReadFileAsync("document.txt");
await Desktop.WriteFileAsync("output.txt", Encoding.UTF8.GetBytes("Hello Desktop!"));
var exists = await Desktop.FileExistsAsync("somefile.txt");

// Window Management
await Desktop.MinimizeWindowAsync();
await Desktop.MaximizeWindowAsync();
await Desktop.SetWindowTitleAsync("New Title");
var state = await Desktop.GetWindowStateAsync();

// System Integration
await Desktop.OpenUrlInBrowserAsync("https://github.com");
await Desktop.ShowNotificationAsync("Title", "Message");

// Clipboard Operations
var clipboardText = await Desktop.GetClipboardTextAsync();
await Desktop.SetClipboardTextAsync("Copied from desktop app!");

// Get system paths
var appDataPath = await Desktop.GetAppDataPathAsync();
var documentsPath = await Desktop.GetDocumentsPathAsync();
```

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
- **.NET 9.0** or later
- **Windows 10+** ✅ **(Tested)**
- **Linux with WebKit** ⚠️ **(Untested - on roadmap)**
- **macOS 10.15+** ⚠️ **(Untested - on roadmap)**

### Development Requirements
- **Visual Studio 2022** (17.8+) or **VS Code**
- **.NET 9.0 SDK**
- **C# 13** language features

### Package Dependencies
- `Avalonia 11.3.2+` - Desktop framework
- `MudBlazor 8.10.0+` - Material Design components  
- `Photino.NET 4.0.16+` - WebView hosting
- `Microsoft.AspNetCore.Components.Web 9.0.7+` - Blazor components

---

## 🐛 Troubleshooting

### Common Issues & Solutions

**🚫 Build Errors**
```bash
# Ensure correct .NET version
dotnet --version  # Should be 9.0+

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
- Verify Photino permissions
- Check `IDesktopInteropService` injection
- Test with simple file operations first

**🚫 Hot Reload Not Working**
- Restart application
- Check VS/VS Code Blazor extensions
- Verify project targets .NET 9.0

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

### Current Status: **Pre-Alpha Hobby Project** 🚧
- ✅ **Core Framework**: Avalonia + Blazor + Photino integration
- ✅ **NuGet Package**: Published and functional
- ✅ **File System Interop**: File dialogs, I/O operations
- ✅ **Window Management**: Minimize, maximize, resize
- ✅ **MudBlazor Integration**: Full component library support

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
- **Pre-alpha hobby project** - breaking changes expected
- **Currently tested on Windows only** - Linux and macOS compatibility validation in progress
- **MudBlazor-focused currently** - other UI framework integrations planned
- **Limited support** - best-effort basis only
- Some Photino features not yet exposed (alternatives planned)
- Testing infrastructure in development

---

## 🤝 Contributing & Support

### Project Status
**This is a personal hobby project in pre-alpha stage.** I'm not currently accepting pull requests as the core architecture is still evolving and I'm working through fundamental design decisions.

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
