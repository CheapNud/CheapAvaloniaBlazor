using Photino.NET;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CheapAvaloniaBlazor.Configuration;

namespace CheapAvaloniaBlazor.Services;

/// <summary>
/// Manages the Photino window instance and provides thread-safe access
/// </summary>
public class PhotinoWindowManager : IDisposable
{
    private PhotinoWindow? _window;
    private readonly object _lock = new();
    private readonly ILogger<PhotinoWindowManager>? _logger;
    private TaskCompletionSource<bool>? _windowReadyTcs;
    private CancellationTokenSource? _closeCts;
    private readonly Dictionary<string, Func<string, Task<string>>> _messageHandlers = new();

    public bool IsWindowReady => _window != null;
    public bool IsWindowVisible { get; private set; }

    public PhotinoWindowManager(ILogger<PhotinoWindowManager>? logger = null)
    {
        _logger = logger;
        RegisterDefaultHandlers();
    }

    /// <summary>
    /// Initialize the Photino window
    /// </summary>
    public void InitializeWindow(string url, CheapAvaloniaBlazorOptions options)
    {
        lock (_lock)
        {
            if (_window != null)
            {
                _logger?.LogWarning("Window already initialized");
                return;
            }

            _windowReadyTcs = new TaskCompletionSource<bool>();
            _closeCts = new CancellationTokenSource();

            try
            {
                _window = new PhotinoWindow()
                    .SetTitle(options.DefaultWindowTitle)
                    .SetUseOsDefaultSize(false)
                    .SetSize(options.DefaultWindowWidth, options.DefaultWindowHeight)
                    .SetResizable(options.Resizable)
                    .RegisterWebMessageReceivedHandler(OnWebMessageReceived)
                    .SetDevToolsEnabled(options.EnableDevTools)
                    .SetContextMenuEnabled(options.EnableContextMenu)
                    .SetGrantBrowserPermissions(true);

                // Configure window position
                if (options.CenterWindow)
                {
                    _window.Center();
                }
                else if (options.WindowLeft.HasValue && options.WindowTop.HasValue)
                {
                    _window.SetPosition(options.WindowLeft.Value, options.WindowTop.Value);
                }

                // Configure window chrome
                if (options.Chromeless)
                {
                    _window.SetChromeless(true);
                }

                // Configure window icon
                if (!string.IsNullOrEmpty(options.IconPath))
                {
                    _window.SetIconFile(options.IconPath);
                }

                // Configure zoom
                if (options.DefaultZoom != 100)
                {
                    _window.SetZoom(options.DefaultZoom);
                }

                // Register lifecycle handlers
                _window.WindowCreated += OnWindowCreated;
                _window.WindowClosing += OnWindowClosing;
                _window.WindowSizeChanged += OnWindowSizeChanged;
                _window.WindowLocationChanged += OnWindowLocationChanged;
                _window.WindowMaximized += OnWindowMaximized;
                _window.WindowMinimized += OnWindowMinimized;
                _window.WindowRestored += OnWindowRestored;
                _window.WindowFocusIn += OnWindowFocusIn;
                _window.WindowFocusOut += OnWindowFocusOut;

                // Load the URL
                _window.Load(url);

                _logger?.LogInformation("Photino window initialized with URL: {Url}", url);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize Photino window");
                _windowReadyTcs?.SetException(ex);
                throw;
            }
        }
    }

    /// <summary>
    /// Show the window and wait for it to close
    /// </summary>
    public async Task ShowAndWaitForCloseAsync()
    {
        await EnsureWindowReadyAsync();

        IsWindowVisible = true;

        await Task.Run(() =>
        {
            _window?.WaitForClose();
        }, _closeCts?.Token ?? CancellationToken.None);

        IsWindowVisible = false;
    }

    /// <summary>
    /// Show the window without blocking
    /// </summary>
    public async Task ShowAsync()
    {
        await EnsureWindowReadyAsync();

        lock (_lock)
        {
            _window?.Show();
            IsWindowVisible = true;
        }
    }

    /// <summary>
    /// Hide the window
    /// </summary>
    public async Task HideAsync()
    {
        await EnsureWindowReadyAsync();

        lock (_lock)
        {
            _window?.Hide();
            IsWindowVisible = false;
        }
    }

    /// <summary>
    /// Invoke an action on the Photino window thread-safely
    /// </summary>
    public async Task InvokeAsync(Action<PhotinoWindow> action)
    {
        await EnsureWindowReadyAsync();

        lock (_lock)
        {
            if (_window != null)
            {
                action(_window);
            }
        }
    }

    /// <summary>
    /// Invoke a function on the Photino window thread-safely
    /// </summary>
    public async Task<T> InvokeAsync<T>(Func<PhotinoWindow, T> func)
    {
        await EnsureWindowReadyAsync();

        lock (_lock)
        {
            if (_window != null)
            {
                return func(_window);
            }

            throw new InvalidOperationException("Photino window is not initialized");
        }
    }

    /// <summary>
    /// Send a message to the web view
    /// </summary>
    public async Task SendWebMessageAsync(string message)
    {
        await InvokeAsync(window => window.SendWebMessage(message));
        _logger?.LogDebug("Sent web message: {Message}", message);
    }

    /// <summary>
    /// Register a message handler for a specific message type
    /// </summary>
    public void RegisterMessageHandler(string messageType, Func<string, Task<string>> handler)
    {
        lock (_lock)
        {
            _messageHandlers[messageType] = handler;
        }
    }

    /// <summary>
    /// Execute JavaScript in the web view
    /// </summary>
    public async Task<string> ExecuteScriptAsync(string script)
    {
        var tcs = new TaskCompletionSource<string>();
        var resultId = Guid.NewGuid().ToString();

        // Wrap the script to return result via message
        var wrappedScript = $@"
            (async function() {{
                try {{
                    const result = await (async function() {{ {script} }})();
                    window.external.sendMessage(JSON.stringify({{
                        type: 'scriptResult',
                        id: '{resultId}',
                        success: true,
                        result: result
                    }}));
                }} catch (error) {{
                    window.external.sendMessage(JSON.stringify({{
                        type: 'scriptResult',
                        id: '{resultId}',
                        success: false,
                        error: error.toString()
                    }}));
                }}
            }})();
        ";

        // Register temporary handler for result
        RegisterMessageHandler($"scriptResult_{resultId}", async (payload) =>
        {
            tcs.SetResult(payload);
            return "";
        });

        await InvokeAsync(window => window.SendWebMessage(wrappedScript));

        // Wait for result with timeout
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        cts.Token.Register(() => tcs.TrySetCanceled());

        return await tcs.Task;
    }

    /// <summary>
    /// Close the window
    /// </summary>
    public void Close()
    {
        lock (_lock)
        {
            if (_window != null)
            {
                _closeCts?.Cancel();
                _window.Close();
                _logger?.LogInformation("Photino window closed");
            }
        }
    }

    /// <summary>
    /// Ensure the window is ready
    /// </summary>
    private async Task EnsureWindowReadyAsync()
    {
        if (_windowReadyTcs != null)
        {
            await _windowReadyTcs.Task;
        }
        else
        {
            throw new InvalidOperationException("Window has not been initialized");
        }
    }

    /// <summary>
    /// Register default message handlers
    /// </summary>
    private void RegisterDefaultHandlers()
    {
        RegisterMessageHandler("minimize", async (payload) =>
        {
            await InvokeAsync(w => w.SetMinimized(true));
            return "ok";
        });

        RegisterMessageHandler("maximize", async (payload) =>
        {
            await InvokeAsync(w => w.SetMaximized(true));
            return "ok";
        });

        RegisterMessageHandler("restore", async (payload) =>
        {
            await InvokeAsync(w => w.SetMaximized(false));
            await InvokeAsync(w => w.SetMinimized(false));
            return "ok";
        });

        RegisterMessageHandler("toggleMaximize", async (payload) =>
        {
            var isMaximized = await InvokeAsync(w => w.Maximized);
            await InvokeAsync(w => w.SetMaximized(!isMaximized));
            return "ok";
        });

        RegisterMessageHandler("close", async (payload) =>
        {
            Close();
            return "ok";
        });

        RegisterMessageHandler("setTitle", async (payload) =>
        {
            if (!string.IsNullOrEmpty(payload))
            {
                await InvokeAsync(w => w.SetTitle(payload));
            }
            return "ok";
        });

        RegisterMessageHandler("getWindowState", async (payload) =>
        {
            var state = await InvokeAsync(w => new
            {
                maximized = w.Maximized,
                minimized = w.Minimized,
                width = w.Width,
                height = w.Height,
                left = w.Left,
                top = w.Top
            });
            return System.Text.Json.JsonSerializer.Serialize(state);
        });
    }

    /// <summary>
    /// Handle web messages
    /// </summary>
    private void OnWebMessageReceived(object? sender, string message)
    {
        Task.Run(async () =>
        {
            try
            {
                var messageData = System.Text.Json.JsonSerializer.Deserialize<WebMessage>(message);

                if (messageData?.Type != null)
                {
                    _logger?.LogDebug("Received web message: {Type}", messageData.Type);

                    // Check for script result
                    if (messageData.Type == "scriptResult" && !string.IsNullOrEmpty(messageData.Id))
                    {
                        var handlerKey = $"scriptResult_{messageData.Id}";
                        if (_messageHandlers.TryGetValue(handlerKey, out var handler))
                        {
                            await handler(message);
                            _messageHandlers.Remove(handlerKey);
                            return;
                        }
                    }

                    // Check for registered handler
                    if (_messageHandlers.TryGetValue(messageData.Type, out var messageHandler))
                    {
                        var result = await messageHandler(messageData.Payload ?? "");

                        // Send response if requested
                        if (!string.IsNullOrEmpty(messageData.Id))
                        {
                            var response = new
                            {
                                type = "response",
                                id = messageData.Id,
                                result = result
                            };
                            await SendWebMessageAsync(System.Text.Json.JsonSerializer.Serialize(response));
                        }
                    }
                    else
                    {
                        _logger?.LogWarning("No handler registered for message type: {Type}", messageData.Type);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error processing web message");
            }
        });
    }

    // Window lifecycle event handlers
    private void OnWindowCreated(object? sender, EventArgs e)
    {
        _logger?.LogInformation("Photino window created");
        _windowReadyTcs?.SetResult(true);
    }

    private bool OnWindowClosing(object? sender, EventArgs e)
    {
        _logger?.LogInformation("Photino window closing");
        _closeCts?.Cancel();
        return false; // Allow close
    }

    private void OnWindowSizeChanged(object? sender, EventArgs e)
    {
        if (_window != null)
        {
            _logger?.LogDebug("Window size changed: {Width}x{Height}", _window.Width, _window.Height);
        }
    }

    private void OnWindowLocationChanged(object? sender, EventArgs e)
    {
        if (_window != null)
        {
            _logger?.LogDebug("Window location changed: {Left},{Top}", _window.Left, _window.Top);
        }
    }

    private void OnWindowMaximized(object? sender, EventArgs e)
    {
        _logger?.LogDebug("Window maximized");
    }

    private void OnWindowMinimized(object? sender, EventArgs e)
    {
        _logger?.LogDebug("Window minimized");
    }

    private void OnWindowRestored(object? sender, EventArgs e)
    {
        _logger?.LogDebug("Window restored");
    }

    private void OnWindowFocusIn(object? sender, EventArgs e)
    {
        _logger?.LogDebug("Window focused");
    }

    private void OnWindowFocusOut(object? sender, EventArgs e)
    {
        _logger?.LogDebug("Window lost focus");
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_window != null)
            {
                _closeCts?.Cancel();
                _closeCts?.Dispose();

                try
                {
                    _window.Close();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error closing Photino window during disposal");
                }

                _window = null;
            }
        }
    }
}

internal class WebMessage
{
    public string? Type { get; set; }
    public string? Id { get; set; }
    public string? Payload { get; set; }
    public bool Success { get; set; }
    public string? Result { get; set; }
    public string? Error { get; set; }
}