# Getting Started with CheapAvaloniaBlazor

Welcome! This guide will walk you through creating your first cross-platform desktop application using CheapAvaloniaBlazor. By the end, you'll have a fully functional desktop app built with the same technologies you use for web development.

## Prerequisites

Before you begin, ensure you have the following installed:

- **.NET 10.0 SDK** or later - [Download from dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Visual Studio 2022** (17.8+) or **Visual Studio Code** with C# extensions
- Basic knowledge of C# and web development concepts

For detailed installation instructions, see [Installation Guide](./installation.md) (coming soon).

---

## Your First Desktop App (5 Minutes)

This quick start will guide you through creating a minimal but complete desktop application. We'll build a simple app that demonstrates file dialogs and notifications - features that prove this is a real desktop application, not just a web app.

### Step 1: Create the Project

#### Using Command Line (VS Code / Terminal Users)
```bash
# Create a new console project
dotnet new console -n MyDesktopApp
cd MyDesktopApp

# Add the CheapAvaloniaBlazor package
dotnet add package CheapAvaloniaBlazor
```

#### Using Visual Studio 2022 (GUI Users)
1. Open Visual Studio 2022
2. Click **Create a new project**
3. Search for and select **"Console App"** (.NET 10.0)
4. Name your project (e.g., "MyDesktopApp")
5. Click **Create**
6. Right-click the project in Solution Explorer → **Manage NuGet Packages**
7. Search for **"CheapAvaloniaBlazor"** → Click **Install**

### Step 2: Update Your Project File

The `.csproj` file tells .NET how to build your project. By default, console apps use the `Microsoft.NET.Sdk`, but Blazor needs the Web SDK which includes MVC and Blazor support.

**Why?** The Razor SDK with ASP.NET Core framework reference provides the necessary infrastructure for Blazor Server. Using `WinExe` output type ensures no console window appears.

Open your `.csproj` file and modify it:

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <PackageReference Include="CheapAvaloniaBlazor" Version="1.2.4" />
  </ItemGroup>
</Project>
```

**Key Changes:**
- `Sdk="Microsoft.NET.Sdk.Razor"` - Razor SDK for Blazor component compilation
- `<OutputType>WinExe</OutputType>` - Windows executable without console window
- `<FrameworkReference Include="Microsoft.AspNetCore.App" />` - ASP.NET Core for Blazor Server

### Step 3: Create Program.cs

The `Program.cs` file is your application's entry point. This is where you configure your desktop window, UI framework, and services.

Replace your `Program.cs` with:

```csharp
using CheapAvaloniaBlazor.Hosting;

namespace MyDesktopApp;

class Program
{
    [STAThread]  // Required for desktop applications
    public static void Main(string[] args)
    {
        // Create a HostBuilder - this is where all configuration happens
        var builder = new HostBuilder()
            // Window Configuration
            .WithTitle("My Desktop App")           // Title shown in window frame
            .WithSize(1200, 800)                   // Initial window size in pixels

            // UI Framework - adds MudBlazor components
            .AddMudBlazor();

        // Add your application services here
        // Example: builder.Services.AddScoped<IMyService, MyService>();

        // Build and run the application
        // This method handles all the Avalonia complexity for you!
        builder.RunApp(args);
    }
}
```

**Why Each Part Matters:**

- `[STAThread]` - Required on Windows for COM interop (file dialogs, notifications)
- `new HostBuilder()` - Creates the application builder using ASP.NET Core's dependency injection
- `.WithTitle()` - Sets the window title bar text
- `.WithSize(1200, 800)` - Sets initial window dimensions in pixels
- `.AddMudBlazor()` - Registers MudBlazor components and services
- `.RunApp(args)` - Builds and runs the application (handles Avalonia setup automatically)

### Step 4: Create Components/App.razor (HTML Document Root)

The `App.razor` file is the HTML document root - the actual HTML page that gets rendered in your desktop window. It loads CSS, sets up the Blazor runtime, and renders the `Routes` component where your Razor components appear.

**Creating the file:**
- **Visual Studio Users:** Right-click project → Add → New Folder → "Components" → right-click Components → Add → New Item → "Razor Component" → Name it "App.razor"
- **VS Code Users:** Create folder `Components` and file `App.razor` inside it

```razor
@using Microsoft.AspNetCore.Components.Web

<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>My Desktop App</title>
    <base href="/" />

    <!-- Roboto font for MudBlazor Material Design -->
    <link href="https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap" rel="stylesheet" />

    <!-- MudBlazor CSS - provides styling for all Material Design components -->
    <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />

    <!-- HeadOutlet renders <PageTitle> and <HeadContent> from components -->
    <HeadOutlet @rendermode="new InteractiveServerRenderMode(prerender: false)" />
</head>
<body>
    <!-- This is where your Blazor app gets rendered via the Routes component -->
    <Routes @rendermode="new InteractiveServerRenderMode(prerender: false)" />

    <!-- Blazor Web App JavaScript runtime - enables SignalR communication between C# and the browser -->
    <script src="_framework/blazor.web.js"></script>

    <!-- MudBlazor JavaScript - provides interactive component functionality -->
    <script src="_content/MudBlazor/MudBlazor.min.js"></script>

    <!-- CheapAvaloniaBlazor JavaScript bridge - provides clipboard and notification support -->
    <script src="_content/CheapAvaloniaBlazor/cheap-blazor-interop.js"></script>
</body>
</html>
```

**What's Happening Here:**

- This is a full HTML document defined as a Razor component (not a Razor Page)
- `<base href="/" />` - Sets the base URL for relative links
- `<HeadOutlet>` - Renders `<PageTitle>` and `<HeadContent>` from child components
- `<Routes>` - Renders the Blazor router component (created in Step 5)
- `@rendermode="new InteractiveServerRenderMode(prerender: false)"` - Enables interactive server-side rendering
- MudBlazor CSS/JS - Provides Material Design styling and component interactivity
- `blazor.web.js` - The Blazor Web App runtime that manages SignalR communication

### Step 5: Create Components/Routes.razor

The `Routes.razor` component handles routing - determining which page/component should display based on the current URL. It is rendered inside the `App.razor` HTML document root.

**Creating the file:**
- **Visual Studio Users:** Right-click Components folder → Add → New Item → "Razor Component" → Name it "Routes.razor"
- **VS Code Users:** Create file `Routes.razor` inside the `Components` folder

```razor
<Router AppAssembly="@typeof(Routes).Assembly">
    <Found Context="routeData">
        <!-- User navigated to a valid page - render it with the MainLayout -->
        <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
        <FocusOnNavigate RouteData="@routeData" Selector="h1" />
    </Found>
    <NotFound>
        <!-- User navigated to a URL that doesn't exist - show 404 message -->
        <PageTitle>Not found</PageTitle>
        <LayoutView Layout="@typeof(MainLayout)">
            <p role="alert">Sorry, there's nothing at this address.</p>
        </LayoutView>
    </NotFound>
</Router>
```

**Understanding Routing:**

- `<Router>` - Blazor's routing component that watches URL changes
- `AppAssembly="@typeof(Routes).Assembly"` - Tells Router to scan this assembly for `@page` directives
- `<Found>` - Rendered when a matching page is found
- `<NotFound>` - Rendered when no matching page exists (404 handling)
- `DefaultLayout="@typeof(MainLayout)"` - Every page gets wrapped in the MainLayout component
- `<FocusOnNavigate>` - Improves accessibility by setting focus after navigation

### Step 6: Create Shared/MainLayout.razor

The layout component wraps every page in your application. It defines the overall structure (AppBar, sidebar, main content area) that persists across pages.

**Creating the file:**
- **Visual Studio Users:** Right-click project → Add → New Folder → "Shared" → right-click Shared → Add → New Item → "Razor Component" → Name it "MainLayout.razor"
- **VS Code Users:** Create folder `Shared` and file `MainLayout.razor` inside it

```razor
@inherits LayoutComponentBase

<!-- MudBlazor theme provider - required for styling -->
<MudThemeProvider />

<!-- Popover support for dropdowns and tooltips -->
<MudPopoverProvider />

<!-- Dialog support for modal windows -->
<MudDialogProvider />

<!-- Snackbar support for notifications -->
<MudSnackbarProvider />

<!-- Main layout using MudBlazor's MudLayout component -->
<MudLayout>
    <!-- Top application bar -->
    <MudAppBar Elevation="1">
        <MudIconButton Icon="Icons.Material.Filled.Menu" Color="Color.Inherit" Edge="Edge.Start" />
        <MudSpacer />  <!-- Pushes content to the right -->
        <MudText Typo="Typo.h6">My Desktop App</MudText>
        <MudSpacer />
        <MudIconButton Icon="Icons.Material.Filled.Settings" Color="Color.Inherit" />
    </MudAppBar>

    <!-- Left sidebar drawer for navigation -->
    <MudDrawer Open="true" Elevation="1">
        <MudDrawerHeader>
            <MudText Typo="Typo.h6">Navigation</MudText>
        </MudDrawerHeader>
        <MudNavMenu>
            <!-- NavLinks automatically highlight based on current URL -->
            <MudNavLink Href="/" Match="NavLinkMatch.All" Icon="Icons.Material.Filled.Home">
                Home
            </MudNavLink>
            <MudNavLink Href="/counter" Icon="Icons.Material.Filled.Calculate">
                Counter
            </MudNavLink>
        </MudNavMenu>
    </MudDrawer>

    <!-- Main content area - this is where pages get rendered -->
    <MudMainContent>
        <!-- Container constrains content width and adds padding -->
        <MudContainer MaxWidth="MaxWidth.Large" Class="my-16 pt-16">
            @Body  <!-- Pages render here -->
        </MudContainer>
    </MudMainContent>
</MudLayout>
```

**Component Breakdown:**

- `@inherits LayoutComponentBase` - Provides the `@Body` property for page content
- `<MudThemeProvider />` - Required for MudBlazor theming to work
- `<MudPopoverProvider />`, `<MudDialogProvider />`, `<MudSnackbarProvider />` - Enable advanced MudBlazor features
- `<MudLayout>` - MudBlazor's main layout container
- `<MudAppBar>` - Top navigation bar
- `<MudDrawer>` - Collapsible sidebar
- `<MudNavLink>` - Navigation links that auto-highlight based on current page
- `<MudMainContent>` - Main content area where pages render
- `@Body` - Placeholder where each page's content appears

### Step 7: Create _Imports.razor

The `_Imports.razor` file provides using statements that are automatically available to all Razor files in your project. Instead of typing `@using System.Collections.Generic` in every component, you add it here once.

**Creating the file:**
- **Visual Studio Users:** Right-click project → Add → New Item → "Razor Component" → Name it "_Imports.razor"
- **VS Code Users:** Create file `_Imports.razor` in the project root

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

**What Each Using Does:**

- `System.Net.Http` - HTTP client for making web requests
- `System.Net.Http.Json` - JSON serialization helpers for HTTP
- `Microsoft.AspNetCore.Components` - Blazor component base classes
- `Microsoft.AspNetCore.Components.Forms` - Form components (EditForm, InputText, etc.)
- `Microsoft.AspNetCore.Components.Routing` - Router component
- `Microsoft.AspNetCore.Components.Web` - Web-specific components
- `Microsoft.AspNetCore.Components.Web.Virtualization` - Performance optimization for large lists
- `Microsoft.JSInterop` - JavaScript interop for calling JS from C#
- `MudBlazor` - Material Design components
- `CheapAvaloniaBlazor` - Core desktop functionality
- `CheapAvaloniaBlazor.Services` - `IDesktopInteropService` for file dialogs, notifications, etc.

### Step 8: Create Your First Page (Pages/Index.razor)

Now we create an actual page that users will see. The Index.razor page is displayed when the app first loads (because it has `@page "/"`).

**Creating the file:**
- **Visual Studio Users:** Right-click project → Add → New Folder → "Pages" → right-click Pages → Add → New Item → "Razor Component" → Name it "Index.razor"
- **VS Code Users:** Create folder `Pages` and file `Index.razor` inside it

```razor
@page "/"
@inject IDesktopInteropService Desktop

<PageTitle>Home</PageTitle>

<MudCard>
    <MudCardContent>
        <MudText Typo="Typo.h4" GutterBottom="true">
            Welcome to Your Desktop App! 🎉
        </MudText>
        <MudText Class="mb-4">
            This is a native desktop application built with Blazor and running with full file system access.
            Try the buttons below to see desktop capabilities in action.
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
    /// <summary>
    /// Opens a file dialog and shows a notification with the selected file
    /// </summary>
    private async Task OpenFileDialog()
    {
        // Call desktop interop to show native file open dialog
        var file = await Desktop.OpenFileDialogAsync(new()
        {
            Title = "Select a file",
            Filters = new()
            {
                new() { Name = "Text Files", Extensions = new[] { "*.txt", "*.md" } },
                new() { Name = "All Files", Extensions = new[] { "*.*" } }
            }
        });

        // If user didn't cancel the dialog
        if (file != null)
        {
            await Desktop.ShowNotificationAsync(
                "File Selected",
                $"You selected: {Path.GetFileName(file)}"
            );
        }
    }

    /// <summary>
    /// Shows a desktop notification
    /// </summary>
    private async Task ShowNotification()
    {
        await Desktop.ShowNotificationAsync(
            "Hello Desktop!",
            "This is a native desktop notification! 🚀"
        );
    }
}
```

**What's Happening Here:**

- `@page "/"` - This page displays at the root URL
- `@inject IDesktopInteropService Desktop` - Injects the service that provides file dialogs and notifications
- `<PageTitle>` - Sets the browser tab title
- `<MudCard>`, `<MudText>`, `<MudButton>` - MudBlazor Material Design components
- `@code { }` - C# code section where you define component logic
- `@onclick="OpenFileDialog"` - Calls the C# method when button is clicked

**Desktop Interop Methods:**

The `IDesktopInteropService` provides access to native desktop features:

```csharp
// File Operations
var file = await Desktop.OpenFileDialogAsync(options);
var folder = await Desktop.OpenFolderDialogAsync(options);
var path = await Desktop.SaveFileDialogAsync(options);

// File I/O
var content = await Desktop.ReadFileAsync("path/to/file.txt");
await Desktop.WriteFileAsync("path/to/file.txt", bytes);

// Notifications
await Desktop.ShowNotificationAsync("Title", "Message");

// Window Management
await Desktop.MinimizeWindowAsync();
await Desktop.MaximizeWindowAsync();
await Desktop.SetWindowTitleAsync("New Title");

// URL Opening
await Desktop.OpenUrlInBrowserAsync("https://github.com");

// Clipboard
var text = await Desktop.GetClipboardTextAsync();
await Desktop.SetClipboardTextAsync("Copied text");

// System Paths
var appDataPath = await Desktop.GetAppDataPathAsync();
var documentsPath = await Desktop.GetDocumentsPathAsync();
```

---

## Running Your App

### Visual Studio Users

Press **F5** or select **Debug → Start Debugging**

You should see:
1. A splash screen appears briefly (shows "Loading...")
2. Your desktop window opens with your app
3. You can click buttons and interact with your application

### Command Line / VS Code Users

```bash
# Run in debug mode with hot reload enabled
dotnet run

# Your app will open in a new window
```

### Hot Reload (Automatic Updates)

Make a change to any `.razor` file and save it. The app will automatically reload the component without restarting!

**Try it:**
1. Change "Welcome to Your Desktop App! 🎉" to "Welcome to Your Amazing Desktop App! 🎉"
2. Save the file
3. Look at your running app - the text updates instantly without restarting

### Release Mode

When you're ready to distribute:

```bash
# Build optimized release version
dotnet publish -c Release -r win-x64 --self-contained

# Single executable that doesn't require .NET installed
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true

# Cross-platform
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true
dotnet publish -c Release -r osx-x64 --self-contained -p:PublishSingleFile=true
```

---

## Common Next Steps

### 1. Add More Pages

Create new Razor components in the Pages folder with different `@page` routes:

```razor
@page "/counter"

<MudCard>
    <MudCardContent>
        <MudText>Counter: @count</MudText>
        <MudButton @onclick="Increment">Increment</MudButton>
    </MudCardContent>
</MudCard>

@code {
    private int count = 0;

    private void Increment() => count++;
}
```

Then add a navigation link to `MainLayout.razor`:
```razor
<MudNavLink Href="/counter" Icon="Icons.Material.Filled.Calculate">Counter</MudNavLink>
```

### 2. Add Services

For business logic, create services and register them in `Program.cs`:

```csharp
// Program.cs
builder.Services.AddScoped<ICounterService, CounterService>();
```

Then inject in your components:
```razor
@inject ICounterService CounterService
```

### 3. Access App Data Paths

```csharp
var appDataPath = await Desktop.GetAppDataPathAsync();
var dbPath = Path.Combine(appDataPath, "MyApp", "database.sqlite");
```

### 4. Use Entity Framework Core for Local Database

```bash
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
```

Then create a DbContext and query data directly from your components.

### 5. Call External APIs

```csharp
@inject HttpClient Http

@code {
    private async Task FetchData()
    {
        var data = await Http.GetFromJsonAsync<MyData>("https://api.example.com/data");
    }
}
```

---

## Troubleshooting

### Port Already in Use

If you see "Port 5000 is already in use":

```csharp
// Program.cs
var builder = new HostBuilder()
    .WithTitle("My App")
    .UsePort(5001)  // Use different port
    .AddMudBlazor();
```

### Blazor Not Connecting

Check the browser console (F12) for SignalR errors. Ensure:
1. No firewall is blocking localhost connections
2. The port specified in Program.cs is available
3. You're running in debug mode

### MudBlazor Styles Missing

Ensure your `App.razor` includes:
```html
<link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
<script src="_content/MudBlazor/MudBlazor.min.js"></script>
```

And `AddMudBlazor()` is called in `Program.cs`.

### File Dialogs Not Working

Update to the latest CheapAvaloniaBlazor package:
```bash
dotnet add package CheapAvaloniaBlazor
```

### Hot Reload Not Working

1. Restart the application
2. Check that your project targets .NET 10.0
3. Verify you have the latest C# extensions in VS Code

---

## Learning Resources

- **MudBlazor Documentation**: [mudblazor.com](https://mudblazor.com/)
- **Blazor Documentation**: [docs.microsoft.com/blazor](https://docs.microsoft.com/aspnet/core/blazor/)
- **C# Documentation**: [docs.microsoft.com/dotnet/csharp](https://docs.microsoft.com/dotnet/csharp/)

---

## What's Next?

- Explore [Advanced Configuration](./advanced-configuration.md) (coming soon)
- Learn about [Desktop Interop Features](./desktop-interop.md) (coming soon)
- Build real-world examples with databases and APIs
- Share your creations with the community!

Happy coding! 🚀
