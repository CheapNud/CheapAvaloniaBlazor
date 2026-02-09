using System.Collections.Concurrent;
using System.Net;
using Avalonia.Threading;
using CheapAvaloniaBlazor.Models;
using CheapAvaloniaBlazor.Services.Backends;
using Microsoft.Extensions.Logging;
using Photino.NET;

namespace CheapAvaloniaBlazor.Services;

/// <summary>
/// Cross-platform orchestrator for child windows and modal dialogs.
/// Child windows are created on the main Photino thread via Invoke() so they participate
/// in the existing Win32 message pump. Each window connects to the shared Blazor server
/// as an independent SignalR circuit.
/// </summary>
/// <remarks>
/// Photino enforces ONE global message pump per process (static _messageLoopIsStarted flag).
/// Calling WaitForClose() on subsequent PhotinoWindows creates the native window and returns
/// immediately because the pump is already running — the new window is serviced by the
/// existing pump. This is the correct multi-window pattern for Photino.
/// </remarks>
public sealed class WindowService : IWindowService
{
    private readonly IBlazorHostService _blazorHost;
    private readonly ILogger<WindowService> _logger;
    private readonly IModalBackend _modalBackend;
    private readonly ConcurrentDictionary<string, WindowInfo> _windows = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<ModalResult>> _modalCompletions = new();

    private PhotinoWindow? _mainPhotinoWindow;
    private IntPtr _mainWindowHandle;
    private volatile bool _disposed;

    public bool IsModalSupported => _modalBackend.IsSupported;

    public event Action<string>? WindowCreated;
    public event Action<string>? WindowClosed;
    public event Action<string, string, object?>? MessageReceived;

    public WindowService(IBlazorHostService blazorHost, ILogger<WindowService> logger)
    {
        _blazorHost = blazorHost;
        _logger = logger;
        _modalBackend = CreateModalBackend(logger);

        _logger.LogDebug("WindowService initialized with modal backend {Backend} (supported={Supported})",
            _modalBackend.GetType().Name, _modalBackend.IsSupported);
    }

    /// <summary>
    /// Called by BlazorHostWindow after the main Photino window's native handle is created.
    /// Stores the PhotinoWindow reference for Invoke()-based child window creation.
    /// </summary>
    internal void RegisterMainWindow(PhotinoWindow photinoWindow)
    {
        _mainPhotinoWindow = photinoWindow;
        _mainWindowHandle = photinoWindow.WindowHandle;
        _logger.LogInformation("Main window registered with handle {Handle}", _mainWindowHandle);
    }

    public Task<string> CreateWindowAsync(WindowOptions options)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(WindowService));
        ValidateOptions(options);

        return CreateWindowCoreAsync(options, isModal: false);
    }

    public async Task<ModalResult> CreateModalAsync(WindowOptions options)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(WindowService));
        ValidateOptions(options);

        var tcs = new TaskCompletionSource<ModalResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var windowId = await CreateWindowCoreAsync(options, isModal: true, modalTcs: tcs);

        // Disable the parent window to create modal behavior
        var parentHandle = ResolveParentHandle(options.ParentWindowId);
        if (parentHandle != IntPtr.Zero)
        {
            _modalBackend.DisableParentWindow(parentHandle);
        }

        return await tcs.Task;
    }

    public Task CloseWindowAsync(string windowId)
    {
        if (_disposed) return Task.CompletedTask;

        if (_windows.TryGetValue(windowId, out var windowInfo))
        {
            if (windowInfo.WindowHandle != IntPtr.Zero)
            {
                _modalBackend.PostCloseMessage(windowInfo.WindowHandle);
            }
        }
        else
        {
            _logger.LogWarning("CloseWindowAsync: window '{WindowId}' not found", windowId);
        }

        return Task.CompletedTask;
    }

    public void CompleteModal(string windowId, ModalResult result)
    {
        if (_disposed) return;

        if (!_modalCompletions.TryGetValue(windowId, out var tcs))
        {
            _logger.LogWarning("CompleteModal: no modal completion found for window '{WindowId}'", windowId);
            return;
        }

        // Set the result (TrySetResult handles race with X-close)
        tcs.TrySetResult(result);

        // Re-enable the parent window
        if (_windows.TryGetValue(windowId, out var windowInfo))
        {
            var parentHandle = ResolveParentHandle(windowInfo.ParentWindowId);
            if (parentHandle != IntPtr.Zero)
            {
                _modalBackend.EnableParentWindow(parentHandle);
            }

            // Close the modal window
            if (windowInfo.WindowHandle != IntPtr.Zero)
            {
                _modalBackend.PostCloseMessage(windowInfo.WindowHandle);
            }
        }

        _logger.LogDebug("Modal '{WindowId}' completed (Confirmed={Confirmed})", windowId, result.Confirmed);
    }

    public IReadOnlyList<string> GetWindows()
    {
        return _windows.Keys.ToList().AsReadOnly();
    }

    public void SendMessage(string windowId, string messageType, object? payload = null)
    {
        if (_disposed) return;

        Dispatcher.UIThread.Post(() =>
        {
            InvokeHandlersSafely(MessageReceived, handler =>
            {
                ((Action<string, string, object?>)handler)(windowId, messageType, payload);
            });
        });
    }

    public void BroadcastMessage(string messageType, object? payload = null)
    {
        SendMessage("*", messageType, payload);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _logger.LogDebug("WindowService disposing — closing {Count} child window(s)", _windows.Count);

        // PostMessage is non-blocking (unlike SendMessage) so this is safe from any thread,
        // including the Photino message pump thread. The WM_CLOSE messages are queued and
        // processed asynchronously by the pump — no deadlock risk.
        foreach (var kvp in _windows)
        {
            var windowInfo = kvp.Value;
            if (windowInfo.WindowHandle != IntPtr.Zero)
            {
                _modalBackend.PostCloseMessage(windowInfo.WindowHandle);
            }
        }

        // Complete any outstanding modal TCS with Cancel
        foreach (var kvp in _modalCompletions)
        {
            kvp.Value.TrySetResult(ModalResult.Cancel());
        }

        _windows.Clear();
        _modalCompletions.Clear();
        _modalBackend.Dispose();

        _logger.LogDebug("WindowService disposed");
    }

    // ── Internal ─────────────────────────────────────────────────────────────

    private async Task<string> CreateWindowCoreAsync(WindowOptions options, bool isModal, TaskCompletionSource<ModalResult>? modalTcs = null)
    {
        var windowId = Guid.NewGuid().ToString("N")[..12];
        var windowUrl = BuildWindowUrl(options, windowId);

        var windowInfo = new WindowInfo
        {
            WindowId = windowId,
            IsModal = isModal,
            ParentWindowId = options.ParentWindowId,
        };

        _windows[windowId] = windowInfo;

        if (modalTcs is not null)
        {
            _modalCompletions[windowId] = modalTcs;
        }

        if (_mainPhotinoWindow is null)
        {
            _logger.LogError("Cannot create child window — main window not registered");
            _windows.TryRemove(windowId, out _);
            _modalCompletions.TryRemove(windowId, out _);
            throw new InvalidOperationException("Main window has not been registered. Cannot create child windows before the main window is ready.");
        }

        // Synchronization gate: WindowCreatedHandler fires during WaitForClose() on the Photino
        // thread before WaitForClose returns. This is synchronous in current Photino, but we use
        // an explicit signal to make the contract clear and defend against future Photino changes.
        var handleReady = new ManualResetEventSlim(false);

        // Marshal child window creation to the main Photino thread via Invoke().
        // Photino's message pump is global (one per process). WaitForClose() on subsequent windows
        // creates the native window and returns immediately because the pump is already running.
        // The child window is then serviced by the existing pump — this is the correct pattern.
        // Task.Run avoids blocking the calling (Blazor) thread while Invoke marshals.
        await Task.Run(() =>
        {
            _mainPhotinoWindow.Invoke(() =>
            {
                try
                {
                    // Child windows share the main window's WebView2 user data directory.
                    // This is safe because all windows run on the same thread (via Invoke).
                    // Sharing also reuses the browser process and its cached static assets
                    // (including blazor.web.js which the server may not serve on fresh requests).
                    var childWindow = new PhotinoWindow(_mainPhotinoWindow)
                        .SetTitle(options.Title ?? "Child Window")
                        .SetSize(options.Width, options.Height)
                        .SetMinSize(Constants.Defaults.MinimumResizableWidth, Constants.Defaults.MinimumResizableHeight)
                        .SetResizable(options.Resizable)
                        .SetTopMost(false)
                        .SetUseOsDefaultSize(false)
                        .SetUseOsDefaultLocation(!options.CenterOnParent)
                        .SetLogVerbosity(1);

                    if (options.CenterOnParent)
                    {
                        childWindow.Center();
                    }

                    childWindow.Load(windowUrl);

                    // Capture handle when native window is created (fires during WaitForClose → Photino_ctor)
                    childWindow.RegisterWindowCreatedHandler((s, e) =>
                    {
                        windowInfo.WindowHandle = childWindow.WindowHandle;
                        windowInfo.PhotinoWindow = childWindow;
                        handleReady.Set();
                    });

                    // Detect when user closes the child window
                    childWindow.WindowClosing += (s, e) =>
                    {
                        OnChildWindowClosed(windowId);
                        return false; // Allow close
                    };

                    // WaitForClose() creates the native window and returns immediately
                    // because the main message pump is already running (_messageLoopIsStarted = true).
                    // The window lives on in the existing pump, serviced like any other HWND on this thread.
                    childWindow.WaitForClose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create child window '{WindowId}' on Photino thread", windowId);
                    handleReady.Set(); // Unblock the caller even on failure
                }
            });
        });

        // Wait for the handle signal with timeout — defends against Photino changes
        // where WaitForClose behavior might differ
        if (!handleReady.IsSet && !handleReady.Wait(Constants.Window.HandleReadyTimeoutMs))
        {
            _logger.LogError("Child window '{WindowId}' handle was not ready within {Timeout}ms",
                windowId, Constants.Window.HandleReadyTimeoutMs);
            _windows.TryRemove(windowId, out _);
            _modalCompletions.TryRemove(windowId, out _);
            handleReady.Dispose();
            throw new TimeoutException($"Child window '{windowId}' native handle was not ready within {Constants.Window.HandleReadyTimeoutMs}ms");
        }

        handleReady.Dispose();

        if (windowInfo.WindowHandle == IntPtr.Zero)
        {
            _logger.LogError("Child window '{WindowId}' was created but handle is not available", windowId);
            _windows.TryRemove(windowId, out _);
            _modalCompletions.TryRemove(windowId, out _);
            throw new InvalidOperationException($"Child window '{windowId}' native handle was not captured.");
        }

        // Fire event on Avalonia UI thread — isolate per-handler exceptions
        Dispatcher.UIThread.Post(() =>
        {
            InvokeHandlersSafely(WindowCreated, handler =>
            {
                ((Action<string>)handler)(windowId);
            });
        });

        _logger.LogInformation("Child window '{WindowId}' created (modal={IsModal}, url={Url})", windowId, isModal, windowUrl);
        return windowId;
    }

    private void OnChildWindowClosed(string windowId)
    {
        // If this was a modal and the TCS hasn't been completed yet (user clicked X),
        // complete it with Cancel
        if (_modalCompletions.TryRemove(windowId, out var tcs))
        {
            tcs.TrySetResult(ModalResult.Cancel());

            // Re-enable the parent window
            if (_windows.TryGetValue(windowId, out var windowInfo))
            {
                var parentHandle = ResolveParentHandle(windowInfo.ParentWindowId);
                if (parentHandle != IntPtr.Zero)
                {
                    _modalBackend.EnableParentWindow(parentHandle);
                }
            }
        }

        _windows.TryRemove(windowId, out _);

        if (!_disposed)
        {
            Dispatcher.UIThread.Post(() =>
            {
                InvokeHandlersSafely(WindowClosed, handler =>
                {
                    ((Action<string>)handler)(windowId);
                });
            });
        }

        _logger.LogInformation("Child window '{WindowId}' closed", windowId);
    }

    private string BuildWindowUrl(WindowOptions options, string windowId)
    {
        var baseUrl = _blazorHost.BaseUrl;

        if (options.ComponentType is not null)
        {
            // Component type mode: use the library's WindowHost page
            var typeName = Uri.EscapeDataString(options.ComponentType.AssemblyQualifiedName ?? options.ComponentType.FullName!);
            return $"{baseUrl}{Constants.Window.WindowHostRoute}" +
                   $"?{Constants.Window.ComponentTypeQueryParam}={typeName}" +
                   $"&{Constants.Window.WindowIdQueryParam}={Uri.EscapeDataString(windowId)}";
        }

        if (!string.IsNullOrEmpty(options.UrlPath))
        {
            // URL path mode: extract any existing query string and merge with windowId
            var path = options.UrlPath.StartsWith('/') ? options.UrlPath : "/" + options.UrlPath;
            var separator = path.Contains('?') ? "&" : "?";
            return $"{baseUrl}{path}{separator}{Constants.Window.WindowIdQueryParam}={Uri.EscapeDataString(windowId)}";
        }

        // Fallback to root
        return $"{baseUrl}/?{Constants.Window.WindowIdQueryParam}={Uri.EscapeDataString(windowId)}";
    }

    private IntPtr ResolveParentHandle(string? parentWindowId)
    {
        if (string.IsNullOrEmpty(parentWindowId) || parentWindowId == Constants.Window.MainWindowId)
        {
            return _mainWindowHandle;
        }

        if (_windows.TryGetValue(parentWindowId, out var parentInfo))
        {
            return parentInfo.WindowHandle;
        }

        _logger.LogWarning("Parent window '{ParentWindowId}' not found, falling back to main window", parentWindowId);
        return _mainWindowHandle;
    }

    /// <summary>
    /// Invokes each subscriber of a multicast delegate independently so one bad handler
    /// cannot crash the invocation chain for remaining subscribers.
    /// </summary>
    private void InvokeHandlersSafely(Delegate? multicastDelegate, Action<Delegate> invoker)
    {
        if (multicastDelegate is null) return;

        foreach (var handler in multicastDelegate.GetInvocationList())
        {
            try
            {
                invoker(handler);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Event handler {Handler} threw an exception", handler.Method.Name);
            }
        }
    }

    private static void ValidateOptions(WindowOptions options)
    {
        if (options.UrlPath is null && options.ComponentType is null)
        {
            throw new ArgumentException("Either UrlPath or ComponentType must be set on WindowOptions.", nameof(options));
        }

        if (options.UrlPath is not null && options.ComponentType is not null)
        {
            throw new ArgumentException("UrlPath and ComponentType are mutually exclusive on WindowOptions.", nameof(options));
        }
    }

    private static IModalBackend CreateModalBackend(ILogger logger)
    {
        if (OperatingSystem.IsWindows())
            return new WindowsModalBackend(logger);

        return new NullModalBackend();
    }

    // ── Internal types ───────────────────────────────────────────────────────

    internal class WindowInfo
    {
        public required string WindowId { get; init; }
        public IntPtr WindowHandle { get; set; }
        public PhotinoWindow? PhotinoWindow { get; set; }
        public bool IsModal { get; set; }
        public string? ParentWindowId { get; set; }
    }
}
