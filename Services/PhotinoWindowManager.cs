using Photino.NET;
using System;
using System.Threading.Tasks;

namespace CheapAvaloniaBlazor.Services;

/// <summary>
/// Manages the Photino window instance and provides thread-safe access
/// </summary>
public class PhotinoWindowManager : IDisposable
{
    private PhotinoWindow? _window;
    private readonly object _lock = new();
    private TaskCompletionSource<bool>? _windowReadyTcs;

    public bool IsWindowReady => _window != null;

    /// <summary>
    /// Initialize the Photino window
    /// </summary>
    public void InitializeWindow(string url, CheapAvaloniaBlazorOptions options)
    {
        lock (_lock)
        {
            if (_window != null)
                return;

            _windowReadyTcs = new TaskCompletionSource<bool>();

            _window = new PhotinoWindow()
                .SetTitle(options.DefaultWindowTitle)
                .SetUseOsDefaultSize(false)
                .SetSize(options.DefaultWindowWidth, options.DefaultWindowHeight)
                .SetResizable(options.Resizable)
                .RegisterWebMessageReceivedHandler(OnWebMessageReceived)
                .Load(url);

            if (options.CenterWindow)
            {
                _window.Center();
            }

            // Configure window closing behavior
            _window.WindowClosing += OnWindowClosing;

            // Mark window as ready
            _windowReadyTcs.SetResult(true);
        }
    }

    /// <summary>
    /// Show the window and wait for it to close
    /// </summary>
    public void ShowAndWaitForClose()
    {
        _window?.WaitForClose();
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
    }

    /// <summary>
    /// Close the window
    /// </summary>
    public void Close()
    {
        lock (_lock)
        {
            _window?.Close();
        }
    }

    private async Task EnsureWindowReadyAsync()
    {
        if (_windowReadyTcs != null)
        {
            await _windowReadyTcs.Task;
        }
    }

    private void OnWebMessageReceived(object? sender, string message)
    {
        // Handle messages from JavaScript
        // This can be extended to handle various desktop operations
        try
        {
            var messageData = System.Text.Json.JsonSerializer.Deserialize<WebMessage>(message);

            switch (messageData?.Type)
            {
                case "minimize":
                    _window?.SetMinimized(true);
                    break;
                case "maximize":
                    _window?.SetMaximized(true);
                    break;
                case "close":
                    _window?.Close();
                    break;
                case "setTitle":
                    if (!string.IsNullOrEmpty(messageData.Payload))
                    {
                        _window?.SetTitle(messageData.Payload);
                    }
                    break;
            }
        }
        catch
        {
            // Log error in production
        }
    }

    private bool OnWindowClosing(object? sender, EventArgs e)
    {
        // Allow window to close
        return false;
    }

    public void Dispose()
    {
        Close();
        _window = null;
    }
}

internal class WebMessage
{
    public string? Type { get; set; }
    public string? Payload { get; set; }
}