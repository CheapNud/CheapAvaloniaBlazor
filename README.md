# Blazor Desktop App Template Setup Guide

This guide will help you set up your Blazor Desktop App template for distribution via NuGet packages as a dotnet template.

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

## Setup Steps

### 1. Prepare Your Repository

1. Copy your existing project files to a new directory
2. Add the template configuration files
3. Update placeholder values:
   - Replace `$author$`, `$username$` in .nuspec file
   - Update project GUID in template.json

### 2. Create Template Directory

```bash
mkdir .template.config
```

### 3. For NuGet Package Distribution

1. Install NuGet CLI: `dotnet tool install -g nuget`
2. Run build script: `.\build-template.bat`
3. Push to NuGet.org: `nuget push BlazorDesktopApp.Template.1.0.0.nupkg -Source https://api.nuget.org/v3/index.json -ApiKey YOUR_API_KEY`

### 4. Usage Instructions

#### Via dotnet CLI:
```bash
# Install template
dotnet new install BlazorDesktopApp.Template

# Use template
dotnet new blazordesktop -n MyNewApp
```

#### Via Visual Studio:
1. Open Package Manager Console
2. Run: `dotnet new install BlazorDesktopApp.Template`
3. Use File → New → Project → search for ".NET Core" templates
4. Or use the CLI from VS terminal: `dotnet new blazordesktop -n MyNewApp`

## Testing Your Template

Before publishing, test your template locally:

```bash
# Install template locally
dotnet new install ./BlazorDesktopApp.Template.1.0.0.nupkg

# Test template
dotnet new blazordesktop -n TestApp
cd TestApp
dotnet build
dotnet run

# Uninstall template after testing
dotnet new uninstall BlazorDesktopApp.Template
```

## Updating the Template

1. Make changes to your source template
2. Update version numbers in:
   - template.json
   - .nuspec file
3. Rebuild and republish

## Alternative: Simple Template Pack

For even simpler setup, you can create a template pack without NuGet:

1. Create a folder structure like:
   ```
   MyTemplates/
   └── blazordesktop/
       ├── .template.config/
       │   └── template.json
       └── [your template files]
   ```

2. Install directly from folder:
   ```bash
   dotnet new install ./MyTemplates/blazordesktop
   ```

This approach is perfect for personal use or team-internal templates.

## Integration with Visual Studio

Since Visual Studio 2022 has excellent dotnet CLI integration:

- **Solution Explorer**: Right-click solution → Add → New Project → search for your template
- **Terminal Integration**: Use `dotnet new` commands directly in VS terminal
- **Package Manager Console**: Install/manage templates via PowerShell

## Debug Configuration

Add this to your template's project file for better debugging:

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
  <DefineConstants>DEBUG;TRACE</DefineConstants>
  <IncludeSourceRevisionInInformationalVersion>false</IncludeSourceRevisionInInformationalVersion>
</PropertyGroup>
```

Remember to use `Debug.WriteLine()` for debugging output in your template!

## Benefits of This Approach

- **Single target framework**: Everything stays on .NET 8.0
- **Simple maintenance**: No complex VS extension projects
- **Modern tooling**: Leverages dotnet CLI which VS integrates with perfectly
- **Cross-platform**: Works on any platform that supports .NET
- **Easy distribution**: NuGet handles everything
