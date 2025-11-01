# Splash Screen

## Overview

The splash screen is a professional loading window that displays during application startup while the Blazor server initializes in the background. It provides immediate visual feedback to users that the application is launching, creating a more polished experience compared to a blank window.

### Why It Exists

Modern desktop applications benefit from splash screens because:
- **User Confidence**: Users see the application is responsive during startup
- **Professional Appearance**: Matches expectations of enterprise applications
- **Customization**: Opportunity to brand your application during launch
- **Zero Performance Overhead**: Automatically transitions to hidden mode after startup

### Key Benefits

- **Enabled by Default**: Works out of the box with sensible defaults
- **Fully Customizable**: Colors, text, size, fonts, and custom content
- **Zero Configuration Needed**: Intelligent defaults require no setup
- **Zero Performance Overhead**: Window becomes hidden provider after Blazor loads
- **Professional Theme**: Visual Studio-inspired dark theme by default

---

## Quick Start

### Use Default Splash (No Configuration)

The splash screen is enabled by default with zero configuration needed:

```csharp
var builder = new HostBuilder()
    .WithTitle("My Desktop App")
    .WithSize(1200, 800)
    .AddMudBlazor();

builder.RunApp(args);  // Splash screen shows automatically!
```

The default splash will display with:
- 400x250 pixel window centered on screen
- Application title (from `WithTitle()`)
- "Loading..." message
- Dark theme (#2D2D30 background, white text)
- Animated loading indicator (dots)

### Customize Splash Text

Change the title and loading message in one call:

```csharp
var builder = new HostBuilder()
    .WithTitle("My Desktop App")
    .WithSize(1200, 800)
    .WithSplashScreen("My Desktop App", "Initializing workspace...")
    .AddMudBlazor();

builder.RunApp(args);
```

### Advanced Splash Customization

Configure multiple splash properties at once:

```csharp
var builder = new HostBuilder()
    .WithTitle("My Desktop App")
    .WithSize(1200, 800)
    .ConfigureSplashScreen(splash =>
    {
        splash.Width = 500;
        splash.Height = 300;
        splash.Title = "Enterprise Suite";
        splash.LoadingMessage = "Loading modules...";
        splash.BackgroundColor = "#1E1E1E";     // Darker background
        splash.ForegroundColor = "#00D9FF";     // Cyan accent
        splash.TitleFontSize = 28;
        splash.MessageFontSize = 16;
        splash.ShowLoadingIndicator = true;
    })
    .AddMudBlazor();

builder.RunApp(args);
```

### Custom Splash Content

Replace the default splash with custom Avalonia controls:

```csharp
var builder = new HostBuilder()
    .WithTitle("My Desktop App")
    .WithSize(1200, 800)
    .WithCustomSplashScreen(() =>
    {
        var panel = new StackPanel
        {
            Background = Brushes.DarkSlateGray,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 20
        };

        panel.Children.Add(new TextBlock
        {
            Text = "🚀 My Amazing App",
            FontSize = 32,
            Foreground = Brushes.White,
            TextAlignment = TextAlignment.Center
        });

        panel.Children.Add(new ProgressBar
        {
            IsIndeterminate = true,
            Width = 300
        });

        return new Border
        {
            Background = Brushes.DarkSlateGray,
            Child = panel
        };
    })
    .AddMudBlazor();

builder.RunApp(args);
```

### Disable Splash Screen

Hide the splash screen entirely:

```csharp
var builder = new HostBuilder()
    .WithTitle("My Desktop App")
    .WithSize(1200, 800)
    .WithSplashScreen(false)  // Disable splash screen
    .AddMudBlazor();

builder.RunApp(args);
```

---

## Configuration Reference

All splash screen properties are contained in the `SplashScreenConfig` class. Configure them using `ConfigureSplashScreen()`:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Enabled` | `bool` | `true` | Whether to show splash screen during startup |
| `Title` | `string` | `"Blazor Desktop App"` | Main title text displayed on splash |
| `LoadingMessage` | `string` | `"Loading..."` | Status message below title |
| `Width` | `int` | `400` | Splash window width in pixels |
| `Height` | `int` | `250` | Splash window height in pixels |
| `BackgroundColor` | `string` | `"#2D2D30"` | Background color (hex format) |
| `ForegroundColor` | `string` | `"#FFFFFF"` | Text color (hex format) |
| `TitleFontSize` | `double` | `24.0` | Title text font size in points |
| `MessageFontSize` | `double` | `14.0` | Message text font size in points |
| `ShowLoadingIndicator` | `bool` | `true` | Whether to show animated dots indicator |
| `CustomContentFactory` | `Func<Control>?` | `null` | Custom control factory (overrides default UI) |

### Default Values Reference

The default constants are defined in `Constants.Defaults`:

```csharp
// Splash Screen Defaults
SplashLoadingMessage = "Loading..."
SplashWindowWidth = 400
SplashWindowHeight = 250
SplashBackgroundColor = "#2D2D30"      // Dark theme
SplashForegroundColor = "#FFFFFF"      // White text
SplashTitleFontSize = 24.0
SplashMessageFontSize = 14.0
```

---

## API Reference

### HostBuilder Methods

#### `WithSplashScreen(bool enabled = true)`

Enable or disable the splash screen entirely.

```csharp
// Disable splash screen
.WithSplashScreen(false)

// Enable splash screen (default)
.WithSplashScreen(true)
```

**Returns**: `HostBuilder` for method chaining

---

#### `WithSplashScreen(string title, string loadingMessage = "Loading...")`

Set the splash title and loading message text.

```csharp
.WithSplashScreen("My App", "Initializing...")
```

This method automatically enables the splash screen.

**Parameters**:
- `title` - Splash screen title text
- `loadingMessage` - Loading status message (optional, defaults to "Loading...")

**Returns**: `HostBuilder` for method chaining

---

#### `ConfigureSplashScreen(Action<SplashScreenConfig> configure)`

Configure multiple splash properties at once.

```csharp
.ConfigureSplashScreen(splash =>
{
    splash.BackgroundColor = "#000000";
    splash.ForegroundColor = "#00FF00";
    splash.Width = 600;
    splash.Height = 400;
})
```

**Parameters**:
- `configure` - Action to configure the `SplashScreenConfig` instance

**Returns**: `HostBuilder` for method chaining

---

#### `WithCustomSplashScreen(Func<Control> contentFactory)`

Provide a custom Avalonia control factory to replace the default splash UI.

```csharp
.WithCustomSplashScreen(() =>
{
    var control = new MyCustomSplashControl();
    return control;
})
```

This method automatically enables the splash screen and ignores all standard configuration properties.

**Parameters**:
- `contentFactory` - Function that creates and returns the splash content control

**Returns**: `HostBuilder` for method chaining

**Note**: Custom content completely overrides the default splash UI. Standard properties like `Title`, `BackgroundColor`, etc. are ignored.

---

## Examples

### Example 1: Production-Ready Branded Splash

```csharp
var builder = new HostBuilder()
    .WithTitle("Enterprise Application")
    .WithSize(1400, 900)
    .ConfigureSplashScreen(splash =>
    {
        splash.Title = "Enterprise Application Suite";
        splash.LoadingMessage = "Loading your workspace...";
        splash.Width = 600;
        splash.Height = 350;
        splash.BackgroundColor = "#1A1A1A";      // Very dark background
        splash.ForegroundColor = "#FFFFFF";       // White text
        splash.TitleFontSize = 32;
        splash.MessageFontSize = 16;
        splash.ShowLoadingIndicator = true;
    })
    .AddMudBlazor()
    .ConfigureServices(services =>
    {
        // Add application services
    });

builder.RunApp(args);
```

### Example 2: Dark Theme with Accent Color

```csharp
var builder = new HostBuilder()
    .WithTitle("Developer Tools")
    .WithSize(1200, 800)
    .ConfigureSplashScreen(splash =>
    {
        splash.Title = "Developer Tools";
        splash.LoadingMessage = "Initializing toolchain...";
        splash.BackgroundColor = "#2D2D30";      // VS dark theme
        splash.ForegroundColor = "#007ACC";      // VS blue accent
        splash.TitleFontSize = 28;
        splash.MessageFontSize = 14;
    })
    .AddMudBlazor();

builder.RunApp(args);
```

### Example 3: Animated Progress Example

Custom splash with progress bar (note: this won't actually track progress, just show animated indicator):

```csharp
var builder = new HostBuilder()
    .WithTitle("My App")
    .WithSize(1200, 800)
    .WithCustomSplashScreen(() =>
    {
        var container = new StackPanel
        {
            Background = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 20,
            Width = 400,
            Height = 300
        };

        // Logo/Title
        container.Children.Add(new TextBlock
        {
            Text = "Loading Application",
            FontSize = 24,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.Black,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        // Subtitle
        container.Children.Add(new TextBlock
        {
            Text = "Please wait...",
            FontSize = 14,
            Foreground = new SolidColorBrush(Colors.Gray),
            HorizontalAlignment = HorizontalAlignment.Center
        });

        // Indeterminate progress bar
        container.Children.Add(new ProgressBar
        {
            IsIndeterminate = true,
            Height = 4,
            Width = 300
        });

        return new Border
        {
            Background = Brushes.White,
            Child = container
        };
    })
    .AddMudBlazor();

builder.RunApp(args);
```

### Example 4: Minimal Splash

```csharp
var builder = new HostBuilder()
    .WithTitle("Simple App")
    .WithSize(1200, 800)
    .WithSplashScreen("Simple App", "Starting...")
    .AddMudBlazor();

builder.RunApp(args);
```

### Example 5: Disable Splash

```csharp
var builder = new HostBuilder()
    .WithTitle("No Splash App")
    .WithSize(1200, 800)
    .WithSplashScreen(false)  // No splash screen
    .AddMudBlazor();

builder.RunApp(args);
```

---

## Implementation Details

### Window Lifecycle

The splash screen integrates with the Avalonia window lifecycle:

1. **Initialization Phase**: When `InitializeWindow()` runs, it checks if the splash is enabled
2. **Splash Display Phase**: If enabled, the Avalonia window displays as the splash screen
3. **Blazor Startup Phase**: While splash is visible, the Blazor server starts in the background
4. **Server Ready Phase**: HTTP requests are made to verify the Blazor server is ready
5. **Transition Phase**: Once the server responds successfully, the splash window transitions to hidden mode
6. **Hidden Provider Phase**: The Avalonia window becomes a hidden provider for Avalonia's `StorageProvider` API
7. **Photino Display Phase**: The actual application UI displays in a Photino window

### Technical Architecture

**Avalonia Window (BlazorHostWindow)**:
- Initially displays as the splash screen
- Hides itself after Blazor server is ready
- Remains in memory as a hidden window (off-screen, transparent)
- Provides the `StorageProvider` implementation for file dialogs

**Photino Window**:
- The actual application window
- Created and displayed after splash transitions to hidden mode
- Hosts the Blazor content
- Handles actual user interaction

### How Splash Content is Rendered

The splash content is generated by `SplashScreenConfig.CreateDefaultContent()`:

```csharp
internal Control CreateDefaultContent()
{
    // Creates a centered StackPanel with:
    // 1. Bold title text
    // 2. Loading message (slightly transparent)
    // 3. Animated dots indicator (if enabled)

    var mainPanel = new StackPanel
    {
        Background = backgroundColor,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        Spacing = 20
    };

    // Add children based on configuration...

    return new Border { Background = backgroundColor, Child = mainPanel };
}
```

### Storage Provider Continuity

By keeping the Avalonia window alive in hidden mode, the framework maintains a single `StorageProvider` instance throughout the application lifecycle. This ensures:

- File dialogs work consistently
- No provider state is lost during transitions
- Single source of truth for platform interactions

---

## Best Practices

### 1. Keep Splash Text Brief

Long text may not fit or look professional:

```csharp
// Good - concise
splash.LoadingMessage = "Loading...";
splash.LoadingMessage = "Initializing workspace...";

// Avoid - too long
splash.LoadingMessage = "Loading all resources and configurations for your workspace";
```

### 2. Use Color Contrast

Ensure text is readable against the background:

```csharp
// Good - high contrast
splash.BackgroundColor = "#2D2D30";
splash.ForegroundColor = "#FFFFFF";

// Avoid - low contrast
splash.BackgroundColor = "#F0F0F0";
splash.ForegroundColor = "#ECECEC";
```

### 3. Match Your Application Theme

The splash should feel like part of your application:

```csharp
// If your app uses a professional dark theme
.ConfigureSplashScreen(splash =>
{
    splash.BackgroundColor = "#1E1E1E";
    splash.ForegroundColor = "#FFFFFF";
    splash.TitleFontSize = 28;
})
```

### 4. Font Size Considerations

Balance readability with splash size:

```csharp
// For 400x250 splash
splash.TitleFontSize = 24;
splash.MessageFontSize = 14;

// For 600x400 splash (larger)
splash.TitleFontSize = 32;
splash.MessageFontSize = 18;
```

### 5. Keep Splash Window Reasonable Size

Too large or too small looks unprofessional:

```csharp
// Good sizes
400 x 250    // Default, balanced
500 x 300    // Slightly larger
600 x 350    // For detailed graphics

// Avoid
200 x 150    // Too small, cramped
1200 x 800   // Same as main window, confusing
```

### 6. Use Hex Colors Correctly

Colors must be valid hex format:

```csharp
// Good
splash.BackgroundColor = "#2D2D30";
splash.BackgroundColor = "#000000";
splash.BackgroundColor = "#FFFFFF";

// Avoid
splash.BackgroundColor = "2D2D30";         // Missing #
splash.BackgroundColor = "#2D2D";          // Too short
splash.BackgroundColor = "DarkGray";       // Not hex format
```

### 7. Disable When Not Needed

If your application starts very quickly, consider disabling the splash:

```csharp
#if DEBUG
    .WithSplashScreen(false)
#else
    .WithSplashScreen(true)
#endif
```

### 8. Custom Content Best Practices

When using custom content:

```csharp
.WithCustomSplashScreen(() =>
{
    // ✓ Return a complete Control hierarchy
    var container = new Border { ... };
    return container;

    // ✗ Don't return null
    return null;  // This will cause issues

    // ✗ Don't rely on external state that might change
    var config = GetCurrentConfig();  // Don't do this
    return BuildSplash(config);
})
```

---

## Troubleshooting

### Splash Not Appearing

**Problem**: The splash screen doesn't show during startup.

**Causes & Solutions**:

1. Check if splash is enabled:
```csharp
// Verify splash is enabled
.ConfigureSplashScreen(splash =>
{
    Console.WriteLine($"Splash enabled: {splash.Enabled}");  // Should be true
})
```

2. Verify `WithSplashScreen(false)` wasn't called:
```csharp
// This disables it
.WithSplashScreen(false)
```

3. Check window size - if size is 0 or negative, window won't display:
```csharp
.ConfigureSplashScreen(splash =>
{
    splash.Width = 400;   // Ensure > 0
    splash.Height = 250;  // Ensure > 0
})
```

### Splash Text Not Visible

**Problem**: Text appears on splash but isn't readable.

**Solutions**:

1. Check color contrast:
```csharp
// If background and foreground are too similar
.ConfigureSplashScreen(splash =>
{
    splash.BackgroundColor = "#FFFFFF";  // White background
    splash.ForegroundColor = "#000000";  // Black text
})
```

2. Verify hex color format:
```csharp
// Invalid format
splash.BackgroundColor = "2D2D30";   // Missing #

// Valid format
splash.BackgroundColor = "#2D2D30";
```

3. Check font sizes aren't too small:
```csharp
.ConfigureSplashScreen(splash =>
{
    splash.TitleFontSize = 24;     // Minimum 20
    splash.MessageFontSize = 14;   // Minimum 12
})
```

### Custom Content Not Displaying

**Problem**: Custom splash content doesn't appear or throws errors.

**Solutions**:

1. Ensure factory returns a valid Control:
```csharp
.WithCustomSplashScreen(() =>
{
    if (someCondition)
    {
        // ✓ Always return a Control
        return new TextBlock { Text = "Loading..." };
    }

    // ✗ Don't return null
    return null;  // Will cause issues
})
```

2. Don't use stateful external data:
```csharp
// ✗ Bad - relies on external state
var globalConfig = GetConfig();
.WithCustomSplashScreen(() => CreateSplash(globalConfig))

// ✓ Good - self-contained
.WithCustomSplashScreen(() =>
{
    return new Border { Background = Brushes.DarkGray };
})
```

3. Keep content creation simple:
```csharp
// ✗ Avoid complex logic
.WithCustomSplashScreen(() =>
{
    try
    {
        var service = GetService();
        var data = service.GetDataAsync().Result;  // Blocking
        return CreateSplashFromData(data);
    }
    catch { return null; }
})

// ✓ Keep it simple
.WithCustomSplashScreen(() =>
{
    return new Border
    {
        Background = Brushes.White,
        Child = new TextBlock { Text = "Loading..." }
    };
})
```

### Splash Appears Then Disappears Quickly

**Problem**: Splash shows briefly then closes immediately.

**Cause**: Blazor server is starting very fast.

**Solutions**:

1. This is normal behavior - the splash is working correctly
2. If you want to see it longer, consider adding a minimum display time (not recommended in production)
3. Verify with diagnostics:
```csharp
.EnableDiagnostics()  // Shows timing information
```

### Window Size/Position Issues

**Problem**: Splash appears in wrong location or has wrong size.

**Solutions**:

1. Verify dimensions in configuration:
```csharp
.ConfigureSplashScreen(splash =>
{
    Debug.WriteLine($"Size: {splash.Width}x{splash.Height}");
    // Should show something like: Size: 400x250
})
```

2. Check for negative or zero values:
```csharp
splash.Width = 400;   // Must be > 0
splash.Height = 250;  // Must be > 0
```

3. Window is centered automatically, no manual positioning needed

### Splash Configuration Not Applied

**Problem**: Configuration changes don't appear to take effect.

**Solutions**:

1. Ensure configuration is called before `RunApp()`:
```csharp
var builder = new HostBuilder()
    // ✓ Configure splash here
    .ConfigureSplashScreen(...)
    // ✗ Not here
    ;

// Must call before RunApp()
builder.RunApp(args);
```

2. Verify you're configuring the right builder:
```csharp
var builder = new HostBuilder();
builder.ConfigureSplashScreen(...);  // ✓ Correct
builder.RunApp(args);
```

3. Check property names are correct:
```csharp
// ✓ Correct property names
splash.BackgroundColor
splash.ForegroundColor
splash.TitleFontSize
splash.MessageFontSize
splash.ShowLoadingIndicator

// ✗ Won't work
splash.BgColor        // Wrong name
splash.TextColor      // Wrong name
splash.FontSize       // Wrong name
```

### Splash Stays Visible After Startup

**Problem**: Splash never transitions to hidden mode.

**Causes**:

1. Blazor server failed to start - check logs:
```csharp
.EnableDiagnostics()  // Shows detailed startup information
```

2. HTTP connectivity issue - server started but not responding:
   - Check `Constants.Defaults.ServerReadinessMaxAttempts`
   - Verify Blazor server is actually listening on the configured port

3. Network issues preventing localhost connection:
   - Verify firewall isn't blocking localhost
   - Check port isn't already in use

**Solution**:

1. Check console output for error messages
2. Enable diagnostics for detailed information
3. Verify Blazor server is actually starting by checking port

---

## See Also

- [Main README](../README.md)
- [Configuration Guide](../docs/configuration.md)
- [HostBuilder Documentation](../docs/hostbuilder.md)
- [Avalonia Documentation](https://docs.avaloniaui.net/)
