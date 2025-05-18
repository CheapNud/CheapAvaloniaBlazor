## 🚧 Project Goals

- Create a **unified UI architecture** for desktop and web platforms.
- Reuse Blazor components in both **Photino** and **Avalonia** frontends.
- Showcase the flexibility of **.NET 8** and **Blazor** for cross-platform development.

---

# CheapAvaloniaBlazor Template

A .NET project template for creating cross-platform desktop applications using **Blazor Server**, **MudBlazor**, **Avalonia**, and **Photino**.

> ✅ **Template is ready to use!** This template allows you to quickly scaffold new projects with the complete Blazor desktop stack.

> ⚠️ **Early Stage Development:** This project is in the very early stages. Expect rapid changes, incomplete features, and evolving structure. Contributions and ideas are welcome!

---

## 🧩 Technologies Included

- **[Blazor Server](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)** – Web UI framework running on the server
- **[Avalonia](https://avaloniaui.net/)** – Cross-platform UI framework for desktop applications
- **[MudBlazor](https://mudblazor.com/)** – Material Design component library for Blazor
- **[Photino](https://www.photino.dev/)** – Lightweight, cross-platform desktop host for web-based apps

---

## 🚀 Quick Start

### Install the Template

```bash
# Install from local package
dotnet new install CheapAvaloniaBlazor.Template.1.0.0.nupkg

# Or install from NuGet (if published)
dotnet new install CheapAvaloniaBlazor.Template
```

### Create a New Project

```bash
# Create new project from template
dotnet new cheapavaloniablazor -n MyAwesomeApp

# Navigate to project and run
cd MyAwesomeApp
dotnet build
dotnet run
```

### Verify Installation

```bash
# List all installed templates
dotnet new list

# Should show:
# Template Name              Short Name              Language    Tags
# CheapAvaloniaBlazor...     cheapavaloniablazor     [C#]        Desktop/Blazor/MudBlazor/Avalonia/Photino
```

---

## 🔨 Building the Template (For Developers)

If you want to modify or rebuild the template:

### Prerequisites

- .NET 8.0 SDK
- Windows (for Windows build script) or Linux/macOS (for Unix build script)

### Build Steps

#### Windows
```bash
# 1. Clone/modify the template source
git clone https://github.com/yourusername/CheapAvaloniaBlazor.git
cd CheapAvaloniaBlazor

# 2. Update template configuration
# - Edit .template.config/template.json if needed
# - Update version in both template.json and the build script

# 3. Build the template package
.\build-template.bat

# 4. Test locally
dotnet new install CheapAvaloniaBlazor.Template.1.0.0.nupkg
dotnet new cheapavaloniablazor -n TestApp
```

#### Linux / macOS
```bash
# 1. Clone/modify the template source
git clone https://github.com/yourusername/CheapAvaloniaBlazor.git
cd CheapAvaloniaBlazor

# 2. Make the script executable
chmod +x build-template.sh

# 3. Build the template package
./build-template.sh

# 4. Test locally
dotnet new install CheapAvaloniaBlazor.Template.1.0.0.nupkg
dotnet new cheapavaloniablazor -n TestApp
```

> ⚠️ **Note:** The Linux/macOS build script (`build-template.sh`) has not been thoroughly tested yet. If you encounter issues, please report them or use the Windows script with WSL.

---

## 📁 Project Structure

When you create a new project, you'll get:

```
MyAwesomeApp/
├── Components/
│   ├── Layout/
│   │   ├── MainLayout.razor      # Main application layout
│   │   └── NavMenu.razor         # Navigation menu
│   ├── Pages/
│   │   └── Home.razor            # Homepage component
│   ├── _Host.cshtml              # Blazor Server host page
│   ├── _Imports.razor            # Global imports for components
│   └── Routes.razor              # Application routing configuration
├── wwwroot/
│   └── css/app.css               # Custom styles
├── App.axaml                     # Avalonia application
├── App.axaml.cs                  # Avalonia application logic
├── MainWindow.axaml              # Main window XAML
├── MainWindow.axaml.cs           # Main window logic (Photino integration)
├── Program.cs                    # Application entry point
└── MyAwesomeApp.csproj           # Project file
```

---

## 🔧 Template Management

### Uninstall Template

```bash
dotnet new uninstall CheapAvaloniaBlazor.Template
```

### Update Template

1. Uninstall current version
2. Install new version
3. Or simply install over existing (will update automatically)

---

## 💡 Usage in Visual Studio

The template integrates seamlessly with Visual Studio:

1. **File → New → Project**
2. Search for **"CheapAvaloniaBlazor"**
3. Create your project through the wizard

Or use the **Package Manager Console**:
```powershell
dotnet new cheapavaloniablazor -n MyProject
```

---

## 🎯 What You Get

- **Complete desktop application** ready to run
- **MudBlazor components** pre-configured
- **Responsive layout** with navigation
- **Hot reload** support for development
- **Cross-platform** deployment capability
- **Modern .NET 8** target framework

---

## 🐛 Troubleshooting

**Template not found after installation?**
- Run `dotnet new list` to verify installation
- Check if you're using the correct short name: `cheapavaloniablazor`

**Build errors in generated project?**
- Ensure .NET 8.0 SDK is installed
- Run `dotnet restore` in the project directory

**Photino window doesn't open?**
- Check if port 5000 is available
- Look for debug output in Visual Studio Output window

---

## 📝 License

MIT License - see [LICENSE](LICENSE) file for details.

---

## 🙏 Contributing

Feel free to submit issues, fork the repository, and create pull requests for any improvements.

---

**Happy coding with Blazor Desktop Apps!** 🚀