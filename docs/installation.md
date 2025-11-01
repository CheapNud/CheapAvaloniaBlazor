# Installation Guide

Complete guide to installing and configuring CheapAvaloniaBlazor for cross-platform desktop development.

---

## System Requirements

Before getting started, ensure your system meets these requirements.

### Runtime Requirements

| Requirement | Minimum | Recommended | Status |
|---|---|---|---|
| **.NET Runtime** | 10.0 | Latest 10.0+ | Required |
| **Windows** | 10 | 11 | ✅ Fully Tested |
| **Linux** | Ubuntu 20.04+ | Ubuntu 22.04+ | ⚠️ Untested |
| **macOS** | 10.15+ | Latest | ⚠️ Untested |

### Development Requirements

| Tool | Minimum | Recommended | Purpose |
|---|---|---|---|
| **.NET SDK** | 10.0 | Latest 10.0+ | Development & Building |
| **Visual Studio** | 2022 (17.8+) | 2022 Latest | GUI Development (optional) |
| **VS Code** | Latest | Latest | Terminal & Code Development |
| **C# Support** | C# 13 | C# 13+ | Language Features |

### Disk Space

- **SDK Installation**: 500 MB
- **Package Installation**: 150-300 MB
- **Project Build Cache**: 200-500 MB
- **Recommended Free Space**: 2 GB

---

## Verification Before Installation

Verify your system is ready before proceeding.

### Check .NET SDK Version

```bash
dotnet --version
```

**Expected Output:**
```
10.0.x (or higher)
```

If you see a lower version or "not found", you need to install .NET 10.0 SDK.

### Verify C# Language Support

```bash
dotnet --info
```

Look for `.NET 10.0` in the SDKs section. Your system is ready if version 10.0+ is listed.

---

## Installation Methods

Choose the installation method that best matches your development environment.

### Option A: Command Line (VS Code, Terminal, or PowerShell)

**Best for:** VS Code users, developers comfortable with terminal commands, cross-platform workflows.

#### Step 1: Create New Console Project

```bash
dotnet new console -n MyDesktopApp
cd MyDesktopApp
```

This creates:
- `MyDesktopApp.csproj` - Project file
- `Program.cs` - Application entry point
- `.gitignore` - Git ignore file

#### Step 2: Add CheapAvaloniaBlazor Package

```bash
dotnet add package CheapAvaloniaBlazor
```

This will:
- Download the NuGet package
- Add package reference to `.csproj`
- Download all dependencies automatically
- Cache packages for future builds

**Expected Output:**
```
Writing C:\Users\...\MyDesktopApp\MyDesktopApp.csproj
info : Adding PackageReference for package 'CheapAvaloniaBlazor' into project '...'
info : Restoring packages for C:\Users\...\MyDesktopApp\MyDesktopApp.csproj
info : Package 'CheapAvaloniaBlazor' is compatible with all the specified frameworks in...
info : RestoreOperationCompleted in XXXms for...
```

#### Step 3: Verify Installation

```bash
dotnet restore
dotnet build
```

Both commands should complete **without errors**. If you see errors, check the troubleshooting section below.

---

### Option B: Visual Studio 2022 GUI

**Best for:** Visual Studio users, developers who prefer graphical interfaces, integrated debugging.

#### Step 1: Create New Project

1. Open **Visual Studio 2022**
2. Click **File** → **New** → **Project**
3. Search for **"Console App"** (.NET)
4. Select **"Console App"** template
5. Click **Next**

#### Step 2: Configure Project

1. **Project name**: Enter `MyDesktopApp`
2. **Location**: Choose your desired folder
3. **Solution name**: Auto-filled as project name
4. Click **Next**

#### Step 3: Select Framework

1. **Framework**: Select **.NET 10.0** from dropdown
2. Click **Create**

Visual Studio will:
- Create the project structure
- Generate default `Program.cs`
- Create `.csproj` file
- Load solution in IDE

#### Step 4: Install CheapAvaloniaBlazor Package

1. Right-click **Project** in Solution Explorer
2. Select **Manage NuGet Packages**
3. Click **Browse** tab (if not already selected)
4. Search for **"CheapAvaloniaBlazor"**
5. Select the package from results
6. Click **Install**

**During Installation:**
- Review license terms
- Click **I Accept** in NuGet License Acceptance dialog
- Wait for "Restoration succeeded" message

#### Step 5: Verify Installation

After installation completes:
1. Right-click **Project** → **Build Project**
2. Check **Output** window for build success
3. Look for message: **"Build succeeded"**

**Common Issues During Installation:**
- If NuGet fails to restore, try: **Tools** → **Options** → **NuGet Package Manager** → **Clear All NuGet Cache(s)**
- If build fails, ensure you selected **.NET 10.0** framework in Step 3

---

### Option C: Avalonia Template (Advanced Users)

**Best for:** Developers wanting full Avalonia control, integrating with existing Avalonia projects, advanced customization.

#### Prerequisites

This approach requires familiarity with Avalonia. Only recommended if:
- You're already using Avalonia
- You need specific Avalonia features beyond CheapAvaloniaBlazor defaults
- You're integrating CheapAvaloniaBlazor into existing Avalonia application

#### Step 1: Install Avalonia Templates

```bash
dotnet new install Avalonia.ProjectTemplates
```

Expected output includes Avalonia templates being added to your system.

#### Step 2: Create Avalonia Project

```bash
dotnet new avalonia.app -n MyDesktopApp
cd MyDesktopApp
```

This creates a complete Avalonia application structure.

#### Step 3: Add CheapAvaloniaBlazor

```bash
dotnet add package CheapAvaloniaBlazor
```

#### Step 4: Update Project File

Edit `MyDesktopApp.csproj` to use Web SDK:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Avalonia" Version="11.3.7" />
    <PackageReference Include="CheapAvaloniaBlazor" Version="1.1.0+" />
  </ItemGroup>
</Project>
```

#### Step 5: Integrate with Program.cs

Replace existing `Program.cs` with CheapAvaloniaBlazor configuration (see Quick Start guide).

---

## Package Dependencies

CheapAvaloniaBlazor automatically installs all required dependencies.

### Primary Dependencies

| Package | Version | Purpose |
|---|---|---|
| **Avalonia** | 11.3.7+ | Cross-platform desktop framework, window management |
| **MudBlazor** | 8.13.0+ | Material Design components for Blazor UI |
| **Photino.NET** | 4.0.16+ | WebView hosting, renders Blazor in native window |

### Secondary Dependencies (Auto-Installed)

These are installed automatically by the above packages:

| Package | Purpose |
|---|---|
| **Microsoft.AspNetCore.Components** | Blazor components framework |
| **Microsoft.AspNetCore.Components.Web** | Web component rendering |
| **Microsoft.JSInterop** | JavaScript-C# interoperability |
| **Avalonia.Desktop** | Desktop-specific Avalonia features |

### What Gets Installed

When you run `dotnet add package CheapAvaloniaBlazor`, you get:

```
C:\Users\...\MyDesktopApp\.nuget\packages
├── avalonia/               (Desktop framework)
├── mudbla zor/             (UI components)
├── photino.net/            (WebView hosting)
└── [dependencies]/         (Supporting packages)
```

**Total Size:** ~200-300 MB (first installation only, cached for future projects)

### Version Compatibility

CheapAvaloniaBlazor targets **.NET 10.0** with **C# 13** language features.

- **Earlier .NET versions**: Not supported
- **Later .NET versions**: Compatible with 10.0+ (forward compatible)
- **C# earlier versions**: Not supported (uses C# 13 syntax)

---

## Post-Installation Verification

After installation, verify everything is working correctly.

### Verification Checklist

- [ ] .NET SDK 10.0+ installed (`dotnet --version`)
- [ ] Project created successfully
- [ ] CheapAvaloniaBlazor package installed
- [ ] `dotnet restore` completes without errors
- [ ] `dotnet build` succeeds with no errors
- [ ] Project structure intact (`.csproj` file present)

### Verify Package Installation

```bash
dotnet list package
```

**Expected output includes:**
```
CheapAvaloniaBlazor        (latest version)
```

### Test Build

```bash
dotnet build
```

**Success indicators:**
- No red error messages
- Output shows: `Build succeeded`
- No warnings about missing dependencies

### Quick Runtime Test

```csharp
// Add to Program.cs
Console.WriteLine("CheapAvaloniaBlazor is ready!");
```

Then run:
```bash
dotnet run
```

Should output the message and exit cleanly.

---

## Troubleshooting Installation

### Error: ".NET 10.0 SDK not found"

**Problem:** Your system doesn't have .NET 10.0 SDK installed.

**Solution:**
1. Go to [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/10.0)
2. Download **.NET 10.0 SDK** (not just Runtime)
3. Run the installer
4. Restart your terminal/Visual Studio
5. Verify: `dotnet --version`

**Expected version:** `10.0.x` or higher

---

### Error: "Package 'CheapAvaloniaBlazor' not found"

**Problem:** NuGet cannot find the CheapAvaloniaBlazor package.

**Causes & Solutions:**

| Cause | Solution |
|---|---|
| NuGet cache corrupted | Run: `dotnet nuget locals all --clear` |
| Network connectivity | Check internet connection, verify firewall |
| Package server down | Try again in a few minutes, check nuget.org status |
| Wrong package name | Verify spelling: `CheapAvaloniaBlazor` (exact case) |

**Manual Fix:**

```bash
# Clear all NuGet cache
dotnet nuget locals all --clear

# Restore packages
dotnet restore

# Try installation again
dotnet add package CheapAvaloniaBlazor
```

---

### Error: "SDK Version 10.0 required, but X.X is installed"

**Problem:** Your `.csproj` file specifies .NET 10.0, but you have a different version installed.

**Solution Option 1: Upgrade .NET SDK**
```bash
# Download and install .NET 10.0 from dotnet.microsoft.com
# Then verify
dotnet --version
```

**Solution Option 2: Use installed version (not recommended)**
1. Edit `.csproj`
2. Find: `<TargetFramework>net10.0</TargetFramework>`
3. Change to your installed version (e.g., `net9.0`)
4. Note: Some features may not work with older .NET versions

---

### Error: "Project file is invalid"

**Problem:** `.csproj` file has syntax errors.

**Common Causes:**
- Incomplete XML tags
- Incorrect SDK declaration
- Invalid characters

**Solution:**

1. Open `MyDesktopApp.csproj` in text editor
2. Verify the structure matches this template:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="CheapAvaloniaBlazor" Version="1.1.0" />
  </ItemGroup>
</Project>
```

3. Save file
4. Run: `dotnet restore`

---

### Error: "Avalonia/MudBlazor/Photino dependency resolution failed"

**Problem:** Package manager cannot resolve transitive dependencies.

**Causes & Solutions:**

| Cause | Solution |
|---|---|
| Conflicting package versions | Delete `obj/` and `bin/` folders, run `dotnet restore` |
| Corrupted package cache | Run: `dotnet nuget locals all --clear` |
| Incomplete .csproj | Ensure Web SDK is specified: `Sdk="Microsoft.NET.Sdk.Web"` |

**Nuclear Option (last resort):**
```bash
# Remove all caches and intermediate files
dotnet clean
del bin -r
del obj -r
del packages.lock.json

# Reinstall
dotnet restore
dotnet build
```

---

### Error: "Terminal/Command not found: dotnet"

**Problem:** Terminal cannot find the `dotnet` command.

**Causes & Solutions:**

| Platform | Solution |
|---|---|
| **Windows (PowerShell)** | Restart PowerShell after SDK installation |
| **Windows (CMD)** | Restart Command Prompt after SDK installation |
| **Windows (Git Bash)** | Add .NET to PATH manually or use PowerShell |
| **macOS/Linux** | Run: `source ~/.bashrc` (or restart terminal) |

**Manual PATH Setup:**
- Find where .NET SDK is installed
- Add SDK `bin` folder to system PATH
- Restart terminal and verify: `dotnet --version`

---

### Error: "Visual Studio won't recognize .NET 10.0"

**Problem:** Visual Studio 2022 project selection doesn't show .NET 10.0.

**Solution:**

1. Ensure **.NET 10.0 SDK is installed** (not just Runtime)
2. Restart Visual Studio completely
3. Go to **Tools** → **Options** → **Projects and Solutions** → **.NET Core**
4. Enable experimental features if disabled
5. Close and reopen Visual Studio

**Alternative:** Use command line to create project:
```bash
dotnet new console -n MyDesktopApp
# Then open folder in Visual Studio
```

---

### Error: "Hot Reload not working"

**Problem:** Code changes in `.razor` files don't auto-reload during debugging.

**Solutions:**

**Visual Studio:**
1. Go to **Debug** → **Edit and Continue**
2. Enable **"Hot Reload on File Save"**
3. Ensure project targets .NET 10.0
4. Restart debugger

**VS Code:**
1. Ensure Blazor extension is installed
2. Run `dotnet watch run`
3. Make changes to `.razor` files
4. Save and check browser for auto-reload

---

### Error: "Port 5000/5001 already in use"

**Problem:** Default development port is occupied by another application.

**Solution:**

**Temporary (one-time):**
```bash
dotnet run -- --urls=http://localhost:8080
```

**Permanent (in Program.cs):**
```csharp
var builder = new HostBuilder()
    .WithTitle("My Desktop App")
    .UsePort(8080)  // Use different port
    .AddMudBlazor();

builder.RunApp(args);
```

---

### Error: "MudBlazor styles not loading (unstyled components)"

**Problem:** Components appear without styling (no colors, poor layout).

**Causes & Solutions:**

| Cause | Solution |
|---|---|
| CSS not referenced | Verify `_Host.cshtml` includes: `<link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />` |
| Wrong CSS link | Check link path is exactly: `_content/MudBlazor/MudBlazor.min.css` |
| MudBlazor not added | Verify `AddMudBlazor()` called in HostBuilder |
| Browser cache | Hard refresh: Ctrl+Shift+Delete (Windows) or Cmd+Shift+Delete (Mac) |

**Quick Fix:**
1. Open `Components/_Host.cshtml`
2. Verify CSS line exists in `<head>`:
   ```html
   <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
   ```
3. Check browser console for 404 errors
4. Clear browser cache and reload

---

### Error: "Could not find a part of the path" (Windows)

**Problem:** File path contains special characters or is too long.

**Solution:**

1. Avoid special characters in project/folder names
2. Keep folder paths shorter (Windows has ~260 character limit)
3. Don't use: `< > : " / \ | ? *`
4. Use simple names: `MyDesktopApp` instead of `My-Desktop@App (v1.0!)`

---

## Platform-Specific Installation Notes

### Windows

**Fully tested and supported.**

#### Windows 10/11 Setup

1. Install .NET 10.0 SDK from [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/10.0)
2. Restart your computer (recommended)
3. Use PowerShell or Command Prompt
4. Follow Option A, B, or C above

#### Visual Studio 2022 Setup

1. Install Visual Studio 2022
2. During installation, select **"ASP.NET and web development"** workload
3. Also select **".NET desktop development"** for full Avalonia support
4. Complete installation
5. Follow Option B above

#### Development Tools

- **PowerShell 7+**: Modern shell recommended
- **Windows Terminal**: Better terminal experience
- **Visual Studio 2022**: Full IDE experience

---

### Linux

**Untested but should work** - Dependencies (Avalonia, Photino) are designed for Linux.

#### Linux Setup

1. **Install .NET SDK:**
   ```bash
   # Ubuntu/Debian
   wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
   chmod +x dotnet-install.sh
   ./dotnet-install.sh --channel 10.0
   ```

2. **Install WebKit (required for Photino):**
   ```bash
   # Ubuntu/Debian
   sudo apt-get install libwebkit2gtk-4.1-dev
   ```

3. **Create project:**
   ```bash
   dotnet new console -n MyDesktopApp
   cd MyDesktopApp
   dotnet add package CheapAvaloniaBlazor
   ```

#### Known Issues

- File dialogs may not work (Avalonia StorageProvider integration)
- Window decorations may differ from Windows
- Please report issues on GitHub

---

### macOS

**Untested but should work** - Avalonia and Photino have macOS support.

#### macOS Setup

1. **Install .NET SDK:**
   ```bash
   # Using Homebrew
   brew install dotnet

   # Or manually from dotnet.microsoft.com
   ```

2. **Create project:**
   ```bash
   dotnet new console -n MyDesktopApp
   cd MyDesktopApp
   dotnet add package CheapAvaloniaBlazor
   ```

3. **Grant permissions (may be needed):**
   ```bash
   xcode-select --install  # Install Xcode command line tools
   ```

#### Known Issues

- Requires macOS 10.15+
- Retina display support untested
- Please report issues on GitHub

---

## Next Steps After Installation

Once installation is verified, proceed with:

1. **Read Quick Start Guide**: Follow the setup steps in main README.md
2. **Create Project Structure**: Set up Blazor components and pages
3. **Add Desktop Features**: Use `IDesktopInteropService` for file dialogs, notifications
4. **Configure UI**: Set up MudBlazor layout and components
5. **Build & Deploy**: Package your application for distribution

See the main README.md for detailed guides on each step.

---

## Getting Help

### Troubleshooting Resources

- **Installation Issues**: Check the Troubleshooting section above
- **Build Errors**: See the "Common Issues & Solutions" in main README.md
- **API Questions**: Review inline code comments and API documentation
- **General Help**: Check project discussions or create an issue

### Reporting Issues

Found a problem? Help improve CheapAvaloniaBlazor:

1. **Check existing issues** on GitHub
2. **Provide details**: OS, .NET version, error message, steps to reproduce
3. **Share environment**: `dotnet --info` output helpful
4. **Include logs**: Enable diagnostics and share relevant output

---

## Uninstallation

To remove CheapAvaloniaBlazor:

### Option 1: Remove Package Only

```bash
dotnet remove package CheapAvaloniaBlazor
```

Project folder structure remains; dependencies auto-clean on next `dotnet restore`.

### Option 2: Remove Entire Project

```bash
# Delete the project folder
rm -r MyDesktopApp  # Linux/macOS
rmdir /s MyDesktopApp  # Windows CMD
Remove-Item -Recurse MyDesktopApp  # PowerShell
```

### Clear NuGet Cache (Optional)

```bash
dotnet nuget locals all --clear
```

This removes all cached packages from your system.

---

## Advanced Configuration

After basic installation, explore advanced features:

- **Custom Ports**: `builder.UsePort(8080)`
- **HTTPS Support**: `builder.UseHttps(true)`
- **Diagnostics**: `builder.EnableDiagnostics()`
- **Splash Screen**: `builder.WithSplashScreen(...)`
- **Window Configuration**: `builder.WithSize()`, `builder.WithPosition()`

See main README.md **Advanced Configuration** section for details.

---

## Installation Summary Table

| Method | Best For | Difficulty | Time |
|---|---|---|---|
| **Command Line** | VS Code, Terminal | Easy | 2-3 min |
| **Visual Studio** | GUI users | Very Easy | 2-3 min |
| **Avalonia Template** | Advanced users | Advanced | 5-10 min |

---

**Questions? Issues? Feedback?** Open an issue on GitHub or check project discussions!
