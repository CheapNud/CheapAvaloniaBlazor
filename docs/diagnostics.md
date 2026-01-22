# Diagnostics & Debugging Guide

## Overview

CheapAvaloniaBlazor includes a comprehensive diagnostic system for troubleshooting application startup, configuration, and runtime issues. The diagnostic system provides visibility into service registration, Blazor server initialization, window setup, JavaScript interop operations, and performance metrics.

**Key Features:**
- Conditional verbose logging that respects EnableDiagnostics flag
- Service registration and initialization tracking
- Blazor server startup and health monitoring
- Window lifecycle and property initialization logging
- JavaScript bridge status and communication tracking
- File dialog operation logs
- Performance timing information for critical operations

The diagnostic system is designed to be non-intrusive—diagnostic output is only generated when explicitly enabled, keeping production logs clean while providing detailed troubleshooting information during development.

---

## Enabling Diagnostics

### Via HostBuilder (Recommended)

The simplest way to enable comprehensive diagnostics is using the `EnableDiagnostics()` method on the HostBuilder. This automatically enables both diagnostics and console logging:

```csharp
using CheapAvaloniaBlazor.Hosting;
using MudBlazor.Services;

var builder = new HostBuilder()
    .WithTitle("My Application")
    .EnableDiagnostics()  // Enables both EnableDiagnostics and EnableConsoleLogging
    .AddMudBlazor();

builder.RunApp(args);
```

**Effects of EnableDiagnostics():**
- Sets `EnableDiagnostics = true` in options
- Automatically enables console logging
- Logging minimum level adjusted to Debug
- All diagnostic log methods will output messages

### Alias Method

An alternative alias method is available:

```csharp
var builder = new HostBuilder()
    .WithTitle("My Application")
    .WithDiagnostics()  // Alias for EnableDiagnostics()
    .AddMudBlazor();

builder.RunApp(args);
```

### Independent Console Logging

You can enable console logging independently from diagnostics:

```csharp
var builder = new HostBuilder()
    .WithTitle("My Application")
    .EnableConsoleLogging(true)  // Only enable console output, not diagnostics
    .AddMudBlazor();

builder.RunApp(args);
```

### Via Configuration Options

For more granular control, directly configure the options:

```csharp
var builder = new HostBuilder()
    .WithTitle("My Application")
    .ConfigureOptions(options =>
    {
        options.EnableDiagnostics = true;
        options.EnableConsoleLogging = true;
    })
    .AddMudBlazor();

builder.RunApp(args);
```

---

## Diagnostic Features

### What Gets Logged

When diagnostics are enabled, the system automatically tracks:

#### Service Registration
- Summary of registered services
- Dependency injection container status
- Custom service configuration details

#### Blazor Server Startup
- Server initialization timestamp
- Port and protocol configuration (HTTP/HTTPS)
- Content root and web root paths
- Static web assets loading
- Circuit initialization and configuration

#### Window Initialization
- Window creation with type name
- Title, width, and height settings
- Window startup location (centered vs. manual positioning)
- Icon loading success/failure
- Avalonia platform configuration

#### JavaScript Bridge Status
- Bridge initialization status
- Ready state confirmation
- Interop timeout configuration
- Message channel establishment

#### File Dialog Operations
- Dialog open/save operations initiated
- File selection results
- Dialog cancellation events
- Path information and file counts

#### Performance Timing
- Service initialization duration
- Blazor server startup time
- Window creation latency
- Bridge readiness time

---

## DiagnosticLogger Service

### Overview

The `DiagnosticLogger` is an abstraction layer over `ILogger` that automatically respects the `EnableDiagnostics` flag. It provides specialized methods for different logging scenarios:

- **Diagnostic logs**: Only output when `EnableDiagnostics = true`
- **Verbose logs**: Only output when `EnableDiagnostics = true`
- **Information, Warning, Error logs**: Always output regardless of diagnostic flag

This design keeps production logs clean by filtering diagnostic-only messages while ensuring important warnings and errors are always visible.

### Dependency Injection

Inject `IDiagnosticLoggerFactory` into your services to create loggers:

```csharp
using CheapAvaloniaBlazor.Services;

public class MyService
{
    private readonly DiagnosticLogger _logger;

    public MyService(IDiagnosticLoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<MyService>();
    }
}
```

The factory will automatically create a `DiagnosticLogger` instance bound to your service type.

### Usage Examples

#### Basic Diagnostic Logging

```csharp
public class DataProcessingService
{
    private readonly DiagnosticLogger _logger;

    public DataProcessingService(IDiagnosticLoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<DataProcessingService>();
    }

    public void ProcessData(string[] items)
    {
        // Only logs if EnableDiagnostics = true
        _logger.LogDiagnostic("Starting data processing for {ItemCount} items", items.Length);

        foreach (var item in items)
        {
            _logger.LogDiagnostic("Processing item: {Item}", item);
        }

        _logger.LogDiagnostic("Data processing completed");
    }
}
```

#### Conditional Logging Based on Diagnostics Flag

```csharp
public class AdvancedService
{
    private readonly DiagnosticLogger _logger;

    public AdvancedService(IDiagnosticLoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<AdvancedService>();
    }

    public void PerformOperation()
    {
        if (_logger.DiagnosticsEnabled)
        {
            // Expensive diagnostic work - only do if diagnostics are enabled
            _logger.LogVerbose("Starting detailed diagnostic trace");
            // ... detailed logging ...
        }

        // Always log important operations
        _logger.LogInformation("Operation completed successfully");
    }
}
```

#### Mixed Logging Levels

```csharp
public class RobustService
{
    private readonly DiagnosticLogger _logger;

    public RobustService(IDiagnosticLoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<RobustService>();
    }

    public void ExecuteTask()
    {
        _logger.LogDiagnostic("Task starting");

        try
        {
            // ... perform work ...
            _logger.LogVerbose("Task progressed to checkpoint A");
            // ... more work ...
            _logger.LogInformation("Task completed successfully");
        }
        catch (Exception ex)
        {
            // Errors always logged
            _logger.LogError(ex, "Task failed with exception");
        }
    }
}
```

### Log Levels & Behavior

| Method | Diagnostic-Gated | Log Level | Use Case |
|--------|------------------|-----------|----------|
| `LogDiagnostic(message, args)` | Yes | Debug | Detailed troubleshooting info only needed with diagnostics enabled |
| `LogVerbose(message, args)` | Yes | Information | Verbose operational details; suppressed unless debugging |
| `LogInformation(message, args)` | No | Information | Always-visible operational milestones and status updates |
| `LogWarning(message, args)` | No | Warning | Potential issues that don't prevent operation |
| `LogError(message, args)` | No | Error | Failures that need immediate attention |
| `LogError(exception, message, args)` | No | Error | Exception details always captured |
| `DiagnosticsEnabled` property | — | — | Check if diagnostics are enabled before expensive operations |

---

## Console Logging

### Enabling Console Output

Console logging is automatically enabled when you call `EnableDiagnostics()`:

```csharp
var builder = new HostBuilder()
    .WithTitle("My Application")
    .EnableDiagnostics()  // Enables console logging
    .AddMudBlazor();
```

You can also enable it independently:

```csharp
var builder = new HostBuilder()
    .WithTitle("My Application")
    .EnableConsoleLogging(true)  // Enable console without diagnostics
    .AddMudBlazor();
```

Or disable it explicitly:

```csharp
var builder = new HostBuilder()
    .WithTitle("My Application")
    .EnableConsoleLogging(false)  // Disable console output
    .AddMudBlazor();
```

### Log Level Configuration

When console logging is enabled, the logging framework minimum level is adjusted:

| Setting | Minimum Log Level |
|---------|-------------------|
| Console logging disabled | Information |
| Console logging enabled | Debug |

This ensures diagnostic messages (Debug level) are visible in the console when logging is enabled.

### Output Behavior

**Production (Console logging disabled):**
- Only Information, Warning, and Error messages appear
- Diagnostic and Verbose messages are suppressed
- Clean, concise output

**Development with Diagnostics (Console logging enabled):**
- Debug, Information, Warning, and Error messages appear
- Full diagnostic traces visible
- Verbose troubleshooting information available

---

## Development Tools

### Browser Developer Tools

When `EnableDevTools` is true, you can access browser developer tools while the application is running:

```csharp
var builder = new HostBuilder()
    .WithTitle("My Application")
    .EnableDevTools(true)  // Enable developer tools in webview
    .EnableDiagnostics()
    .AddMudBlazor();
```

**Accessing Developer Tools:**
- Right-click in the application window
- Select "Inspect" or similar option (platform-dependent)
- Browser DevTools console shows JavaScript errors and warnings
- Network tab shows SignalR communication and HTTP requests

### Debug Output Window (Visual Studio)

When running under Visual Studio with a debugger attached:

1. Open **Debug > Windows > Output**
2. Select **Debug** from the "Show output from" dropdown
3. Application logs appear in real-time as the app runs

### Enabling DevTools

Enable browser developer tools to debug the WebView:

```csharp
var builder = new HostBuilder()
    .WithTitle("My Application")
    .EnableDevTools()  // Enable browser DevTools (F12)
    .AddMudBlazor();
```

Once enabled, press F12 to open DevTools.

---

## Common Debugging Scenarios

### Scenario 1: Blazor Won't Start

**Symptoms:** Application launches but Blazor server never initializes; window remains blank

**Debugging Steps:**

1. Enable diagnostics to see startup sequence:
```csharp
var builder = new HostBuilder()
    .WithTitle("My Application")
    .EnableDiagnostics()
    .AddMudBlazor();
```

2. Check console output for messages like:
   - "Blazor host started successfully" → Server is running
   - "Failed to start Blazor host" → Check the exception details
   - "Waiting for Blazor server to respond" → Port may be in use

3. Verify port configuration:
```csharp
var builder = new HostBuilder()
    .WithTitle("My Application")
    .UsePort(5000)  // Explicitly set port
    .EnableDiagnostics()
    .AddMudBlazor();
```

4. Check that another application isn't using the port:
```bash
# Windows - find what's using port 5000
netstat -ano | findstr :5000
```

5. If startup is timing out, increase the timeout:
```csharp
var builder = new HostBuilder()
    .WithTitle("My Application")
    .ConfigureOptions(options =>
    {
        options.StartupTimeout = TimeSpan.FromSeconds(30);  // Increase from default 15 seconds
        options.MaxStartupRetries = 5;  // Increase retry attempts
    })
    .EnableDiagnostics()
    .AddMudBlazor();
```

### Scenario 2: File Dialogs Not Working

**Symptoms:** File dialogs open but don't respond to user interaction

**Debugging Steps:**

1. Enable diagnostics to track bridge initialization:
```csharp
var builder = new HostBuilder()
    .WithTitle("My Application")
    .EnableDiagnostics()
    .AddMudBlazor();
```

2. Check for "JavaScript bridge ready" message in console—if missing, the bridge failed to initialize

3. Verify custom JavaScript doesn't interfere:
```csharp
var builder = new HostBuilder()
    .WithTitle("My Application")
    .ConfigureOptions(options =>
    {
        // Temporarily comment out custom JS
        // options.CustomJavaScript = "...";
    })
    .EnableDiagnostics()
    .AddMudBlazor();
```

4. Check browser permissions are enabled:
```csharp
var builder = new HostBuilder()
    .WithTitle("My Application")
    .ConfigureOptions(options =>
    {
        options.GrantBrowserPermissions = true;
    })
    .EnableDiagnostics()
    .AddMudBlazor();
```

5. Monitor the Network tab in Developer Tools—look for failed requests related to file operations

### Scenario 3: JavaScript Interop Failures

**Symptoms:** JS interop calls timeout or return undefined

**Debugging Steps:**

1. Enable diagnostics and dev tools:
```csharp
var builder = new HostBuilder()
    .WithTitle("My Application")
    .EnableDiagnostics()
    .EnableDevTools(true)
    .AddMudBlazor();
```

2. Check the browser console (Developer Tools) for JavaScript errors

3. Verify the JavaScript function is defined:
```javascript
// In browser console, check if your function exists
typeof window.myFunction  // Should return "function"
```

4. Increase the JSInterop timeout:
```csharp
var builder = new HostBuilder()
    .WithTitle("My Application")
    .ConfigureOptions(options =>
    {
        options.CustomStaticFileOptions = new StaticFileOptions
        {
            // Configure as needed
        };
    })
    .AddMudBlazor();

// Or through Blazor Host Configuration
builder.Services.Configure<CircuitOptions>(options =>
{
    options.JSInteropDefaultCallTimeout = TimeSpan.FromSeconds(60);  // Default is 1 minute
});
```

5. Check SignalR circuit connection in Network tab—verify WebSocket is established and messages flow

### Scenario 4: Performance Problems

**Symptoms:** Application is sluggish or takes a long time to load

**Debugging Steps:**

1. Enable diagnostics to see timing information:
```csharp
var builder = new HostBuilder()
    .WithTitle("My Application")
    .EnableDiagnostics()
    .AddMudBlazor();
```

2. Check startup logs for which phase is slow:
   - "Blazor host started successfully" → Server initialization
   - "JavaScript bridge ready" → Bridge establishment
   - Render events in Developer Tools Network tab → Component rendering

3. Profile rendering in browser Developer Tools:
   - Open Performance tab
   - Record during app interaction
   - Look for long tasks or rendering bottlenecks
   - Check for excessive network requests

4. Monitor Blazor circuit health:
```csharp
var builder = new HostBuilder()
    .WithTitle("My Application")
    .ConfigureOptions(options =>
    {
        // Increase circuit retention to prevent disconnections
        options.MaximumReceiveMessageSize = 100 * 1024 * 1024;  // 100MB for large transfers
    })
    .EnableDiagnostics()
    .AddMudBlazor();
```

5. Check for excessive logging—verbose diagnostics can impact performance:
   - Only enable `LogDiagnostic` and `LogVerbose` during active debugging
   - Use conditional checks with `DiagnosticsEnabled` property

6. Review component rendering—check for unnecessary re-renders in Developer Tools

---

## Log Output Examples

### Successful Startup Sequence

```
info: CheapAvaloniaBlazor.Hosting.HostBuilder[0]
      Created BlazorHostWindow with title 'My Application' at 1024x768
dbug: CheapAvaloniaBlazor.Services.EmbeddedBlazorHostService[0]
      Configuring Blazor server on port 5000 (HTTP)
dbug: CheapAvaloniaBlazor.Services.EmbeddedBlazorHostService[0]
      Service registration summary:
      - IBlazorHostService registered
      - IDiagnosticLoggerFactory registered
      - Mudblazor services configured
info: CheapAvaloniaBlazor.Hosting.HostBuilder[0]
      Blazor host started successfully
dbug: CheapAvaloniaBlazor.Services.JavaScriptBridgeExtractor[0]
      JavaScript bridge ready for interop
```

### Diagnostic Trace During Operation

```
dbug: CheapAvaloniaBlazor.Services.DiagnosticLogger[0]
      User action: Opening file dialog
dbug: CheapAvaloniaBlazor.Services.DiagnosticLogger[0]
      File dialog showing: Filter for *.txt files
dbug: CheapAvaloniaBlazor.Services.DiagnosticLogger[0]
      Selected file: C:\Users\User\Documents\sample.txt
info: CheapAvaloniaBlazor.Services.DiagnosticLogger[0]
      File operation completed successfully
```

### Error with Diagnostics

```
dbug: CheapAvaloniaBlazor.Services.EmbeddedBlazorHostService[0]
      Attempting to start Blazor server (attempt 1 of 3)
dbug: CheapAvaloniaBlazor.Services.EmbeddedBlazorHostService[0]
      Server startup in progress, waiting for readiness...
fail: CheapAvaloniaBlazor.Hosting.HostBuilder[0]
      Failed to start Blazor host
      System.Net.HttpRequestException: Port 5000 is already in use
```

---

## Best Practices

### Development Guidelines

1. **Enable Diagnostics During Development**
   ```csharp
   var builder = new HostBuilder()
       .WithTitle("My Application")
       .EnableDiagnostics()  // Always enable during development
       .AddMudBlazor();
   ```

2. **Use Conditional Diagnostics for Expensive Operations**
   ```csharp
   if (_logger.DiagnosticsEnabled)
   {
       // Only perform expensive diagnostic work when enabled
       var diagnosticData = GatherDetailedDiagnostics();
       _logger.LogVerbose("Diagnostic data: {Data}", diagnosticData);
   }
   ```

3. **Preserve Error Context**
   ```csharp
   try
   {
       // risky operation
   }
   catch (Exception ex)
   {
       // Always log with exception details
       _logger.LogError(ex, "Operation failed: {Operation}", operationName);
   }
   ```

4. **Use Appropriate Log Levels**
   - `LogDiagnostic()`: Step-by-step traces for debugging
   - `LogVerbose()`: Operational progress only visible with diagnostics
   - `LogInformation()`: Normal operational events always visible
   - `LogWarning()`: Potential issues that don't prevent operation
   - `LogError()`: Failures requiring attention

### Production Guidelines

1. **Disable Diagnostics in Production**
   ```csharp
   var builder = new HostBuilder()
       .WithTitle("My Application")
       // Do NOT call .EnableDiagnostics()
       .EnableConsoleLogging(false)  // Disable console output
       .AddMudBlazor();
   ```

2. **Keep Console Logging Disabled**
   - Reduces memory overhead
   - Cleaner log output in monitoring systems
   - Slightly improved performance

3. **Enable Developer Tools Selectively**
   ```csharp
   var builder = new HostBuilder()
       .WithTitle("My Application")
       #if DEBUG
           .EnableDevTools(true)
       #else
           .EnableDevTools(false)
       #endif
       .AddMudBlazor();
   ```

4. **Monitor Real Issues**
   - Configure external logging (Application Insights, Serilog, etc.)
   - Only log warnings and errors in production
   - Use Information level for important milestones only

5. **Testing Before Deployment**
   ```csharp
   // Test with diagnostics disabled
   var builder = new HostBuilder()
       .WithTitle("My Application")
       .EnableConsoleLogging(false)
       .ConfigureOptions(options =>
       {
           options.EnableDiagnostics = false;
           options.EnableDevTools = false;
       })
       .AddMudBlazor();
   ```

### Troubleshooting Workflow

1. **Reproduce the Issue**
   - Document exact steps to reproduce
   - Note error messages and timestamps

2. **Enable Maximum Diagnostics**
   ```csharp
   var builder = new HostBuilder()
       .WithTitle("My Application")
       .EnableDiagnostics()
       .EnableDevTools(true)
       .AddMudBlazor();
   ```

3. **Gather Logs**
   - Console output with timestamps
   - Browser DevTools console
   - Network tab (especially SignalR)
   - Application Insights or external monitoring

4. **Isolate the Component**
   - Disable features one by one
   - Narrow down which service/component fails
   - Reproduce in minimal test case

5. **Check Common Causes**
   - Port conflicts (netstat -ano | findstr :PORT)
   - Network connectivity (especially for HTTPS)
   - File/folder permissions
   - Missing dependencies or services
   - JavaScript interop mismatches

6. **Clean and Rebuild**
   - Clear bin/obj folders
   - Rebuild entire solution
   - Restart Visual Studio/IDE
   - Clear browser cache (Ctrl+Shift+Delete)

---

## Related Resources

- [CheapAvaloniaBlazor README](../README.md#diagnostics--logging)
- [Configuration Options](../Configuration/CheapAvaloniaBlazorOptions.cs)
- [DiagnosticLogger Source](../Services/DiagnosticLogger.cs)
- [HostBuilder Source](../Hosting/HostBuilder.cs)
- [Microsoft Logging Documentation](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging)
- [Blazor Server Troubleshooting](https://learn.microsoft.com/en-us/aspnet/core/blazor/troubleshoot)
