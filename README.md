# CheapAvaloniaBlazor

Build **cross-platform desktop apps** with **Blazor Server** + **MudBlazor**. Get native file system access across Windows, Linux, and macOS using familiar web development patterns.

[![NuGet](https://img.shields.io/nuget/v/CheapAvaloniaBlazor.svg)](https://www.nuget.org/packages/CheapAvaloniaBlazor)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

> ⚠️ **Early Development** - Active development, expect rapid changes

---

## 📦 Installation

```bash
dotnet add package CheapAvaloniaBlazor
```

---

## 🏃‍♂️ Quick Start

### Program.cs
```csharp
using CheapAvaloniaBlazor.Hosting;

var builder = new HostBuilder()
    .WithTitle("My Desktop App")
    .WithSize(1200, 800)
    .AddMudBlazor();

builder.Services.AddScoped<IMyService, MyService>();

var window = builder.Build();
window.Run();
```

### _Host.cshtml
```html
@page "/"
@{ Layout = "_Layout"; }

<component type="typeof(App)" render-mode="ServerPrerendered" />
```

### _Layout.cshtml
```html
<!DOCTYPE html>
<html>
<head>
    <title>My Desktop App</title>
    <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
</head>
<body>
    @RenderBody()
    <script src="_framework/blazor.server.js"></script>
</body>
</html>
```

---

## 🔧 Key Features

**HostBuilder Configuration**
```csharp
var builder = new HostBuilder()
    .WithTitle("Advanced App")
    .WithSize(1400, 900)
    .AddMudBlazor()
    .AddHttpClient("MyAPI", client => client.BaseAddress = new Uri("https://api.com"));
```

**File System Access**
```csharp
// Full file system access in Blazor components
var files = Directory.GetFiles(@"C:\MyFolder");
var content = await File.ReadAllTextAsync("document.txt");
```

**MudBlazor Components**
```razor
<MudContainer>
    <MudAppBar>
        <MudText Typo="Typo.h6">Desktop App</MudText>
    </MudAppBar>
    <MudCard>
        <MudCardContent>
            <MudText>Native desktop with web UI!</MudText>
        </MudCardContent>
    </MudCard>
</MudContainer>
```

---

## 📋 Requirements

- **.NET 9.0** or later
- **Windows 10+**, **Linux**, or **macOS**

---

## 🛠️ Build & Deploy

```bash
# Development
dotnet run

# Single-file executable
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

---

## 🐛 Common Issues

**Build errors?** - Ensure .NET 9.0+ and run `dotnet restore`  
**Window doesn't appear?** - Check port 5000 availability  
**No MudBlazor styling?** - Verify CSS reference in _Layout.cshtml  

---

## 📄 Project Status

**Early Development** - This project is actively evolving. Not accepting pull requests at this time. Issues and feedback welcome.

---

## 📄 License

MIT License

---

**Build desktop apps with Blazor!** 🚀
