using CheapAvaloniaBlazor.Models;
using Microsoft.Extensions.Logging;
using Photino.NET;
using System.Text.Json;

namespace CheapAvaloniaBlazor.Services;

/// <summary>
/// Lightweight message handler for Photino ↔ JavaScript communication
/// Does NOT manage window creation/lifecycle - only handles messages
/// </summary>
public class PhotinoMessageHandler : IDisposable
{
    private PhotinoWindow? _window;
    private readonly Dictionary<string, Func<string, Task<string>>> _messageHandlers = new();
    private readonly ILogger<PhotinoMessageHandler>? _logger;

    public PhotinoMessageHandler(ILogger<PhotinoMessageHandler>? logger = null)
    {
        _logger = logger;
        RegisterDefaultHandlers();
    }

    /// <summary>
    /// Attach to an existing Photino window for message handling
    /// </summary>
    public void AttachToWindow(PhotinoWindow window)
    {
        _window = window;
        window.RegisterWebMessageReceivedHandler(OnWebMessageReceived);
        _logger?.LogInformation("PhotinoMessageHandler attached to window");
    }

    /// <summary>
    /// Register a custom message handler
    /// </summary>
    public void RegisterMessageHandler(string messageType, Func<string, Task<string>> handler)
    {
        _messageHandlers[messageType] = handler;
        _logger?.LogDebug($"Registered message handler for: {messageType}");
    }

    /// <summary>
    /// Send a message to JavaScript
    /// </summary>
    public void SendMessage(string messageType, object? payload = null)
    {
        if (_window == null) return;

        var message = JsonSerializer.Serialize(new { type = messageType, payload });
        _window.SendWebMessage(message);
    }

    private void RegisterDefaultHandlers()
    {
        // Window control handlers - these work with our direct Photino approach
        RegisterMessageHandler("minimize", async (payload) =>
        {
            _window?.SetMinimized(true);
            return "ok";
        });

        RegisterMessageHandler("maximize", async (payload) =>
        {
            _window?.SetMaximized(true);
            return "ok";
        });

        RegisterMessageHandler("restore", async (payload) =>
        {
            if (_window != null)
            {
                _window.SetMaximized(false);
                _window.SetMinimized(false);
            }
            return "ok";
        });

        RegisterMessageHandler("toggleMaximize", async (payload) =>
        {
            if (_window != null)
            {
                var isMaximized = _window.Maximized;
                _window.SetMaximized(!isMaximized);
            }
            return "ok";
        });

        RegisterMessageHandler("close", async (payload) =>
        {
            // Don't close directly - let the window closing handler manage this
            _window?.Close();
            return "ok";
        });

        RegisterMessageHandler("setTitle", async (payload) =>
        {
            if (!string.IsNullOrEmpty(payload) && _window != null)
            {
                _window.SetTitle(payload);
            }
            return "ok";
        });

        RegisterMessageHandler("getWindowState", async (payload) =>
        {
            if (_window == null) return "normal";
            
            if (_window.Maximized) return "maximized";
            if (_window.Minimized) return "minimized";
            return "normal";
        });
    }

    private void OnWebMessageReceived(object? sender, string message)
    {
        try
        {
            _logger?.LogDebug($"Received web message: {message}");
            
            var messageData = JsonSerializer.Deserialize<MessageData>(message);
            if (messageData?.Type == null) return;

            // Handle one-time result handlers
            var resultKey = $"result_{messageData.Type}";
            if (_messageHandlers.ContainsKey(resultKey))
            {
                Task.Run(async () =>
                {
                    if (_messageHandlers.TryGetValue(resultKey, out var handler))
                    {
                        await handler(messageData.Payload ?? "");
                        _messageHandlers.Remove(resultKey);
                    }
                });
                return;
            }

            // Handle regular message handlers
            if (_messageHandlers.TryGetValue(messageData.Type, out var messageHandler))
            {
                Task.Run(async () =>
                {
                    try
                    {
                        var result = await messageHandler(messageData.Payload ?? "");
                        if (!string.IsNullOrEmpty(result))
                        {
                            SendMessage($"response_{messageData.Type}", result);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, $"Error handling message: {messageData.Type}");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Error processing web message: {message}");
        }
    }

    public void Dispose()
    {
        _messageHandlers.Clear();
        _window = null;
    }

    private class MessageData
    {
        public string? Type { get; set; }
        public string? Payload { get; set; }
    }
}