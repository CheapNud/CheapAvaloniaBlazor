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

    /// <summary>
    /// Whitelist of component types allowed in WindowHost.razor.
    /// Only types explicitly passed via <see cref="WindowOptions.ComponentType"/> are registered.
    /// Prevents arbitrary type instantiation from URL query parameters.
    /// Capped at <see cref="Constants.Window.MaxRegisteredComponentTypes"/> distinct types —
    /// re-registering the same type (by FullName) is idempotent and does not count against the limit.
    /// </summary>
    private readonly ConcurrentDictionary<string, Type> _registeredComponents = new();

    /// <summary>
    /// Signaled by <see cref="RegisterMainWindow"/> when the main Photino window is ready.
    /// Early <see cref="CreateWindowAsync"/> callers await this instead of hitting a null-check exception.
    /// </summary>
    private readonly TaskCompletionSource _mainWindowReady = new(TaskCreationOptions.RunContinuationsAsynchronously);

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
        _mainWindowReady.TrySetResult();
        _logger.LogInformation("Main window registered with handle {Handle}", _mainWindowHandle);
    }

    public Type? ResolveWindowComponent(string fullName)
    {
        _registeredComponents.TryGetValue(fullName, out var componentType);
        return componentType;
    }

    public Task<string> CreateWindowAsync(WindowOptions options, CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(WindowService));
        ValidateOptions(options);

        return CreateWindowCoreAsync(options, isModal: false, cancellationToken: cancellationToken);
    }

    public async Task<ModalResult> CreateModalAsync(WindowOptions options, CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(WindowService));
        ValidateOptions(options);

        var tcs = new TaskCompletionSource<ModalResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var windowId = await CreateWindowCoreAsync(options, isModal: true, modalTcs: tcs, cancellationToken: cancellationToken);

        // Disable the parent window to create modal behavior
        var parentHandle = ResolveParentHandle(options.ParentWindowId);
        if (parentHandle != IntPtr.Zero)
        {
            _modalBackend.DisableParentWindow(parentHandle);
        }

        // Register cancellation to prevent indefinite hang if the child window crashes or
        // the caller decides to bail. On cancel: complete TCS, re-enable parent, close child.
        CancellationTokenRegistration? ctsRegistration = null;
        if (cancellationToken.CanBeCanceled)
        {
            ctsRegistration = cancellationToken.Register(() =>
            {
                if (_modalCompletions.TryRemove(windowId, out var cancelledTcs))
                {
                    cancelledTcs.TrySetResult(ModalResult.Cancel());

                    if (_windows.TryGetValue(windowId, out var cancelledWindowInfo))
                    {
                        var cancelParentHandle = ResolveParentHandle(cancelledWindowInfo.ParentWindowId);
                        if (cancelParentHandle != IntPtr.Zero)
                            _modalBackend.EnableParentWindow(cancelParentHandle);

                        if (cancelledWindowInfo.WindowHandle != IntPtr.Zero)
                            _modalBackend.PostCloseMessage(cancelledWindowInfo.WindowHandle);
                    }
                }
            });
        }

        try
        {
            return await tcs.Task;
        }
        finally
        {
            ctsRegistration?.Dispose();
        }
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

    /// <remarks>
    /// Concurrency contract: Three paths compete for modal TCS ownership via TryRemove on
    /// _modalCompletions — CompleteModal, OnChildWindowClosed, and the CancellationToken callback.
    /// ConcurrentDictionary.TryRemove is atomic: exactly ONE path wins and executes the
    /// EnableParentWindow + PostCloseMessage cleanup. Losers get false and no-op.
    /// Post-removal actions (EnableParent, PostClose) use value-type IntPtr copies and
    /// WindowsModalBackend guards each call with IsWindow, so stale handles are safe.
    /// </remarks>
    public void CompleteModal(string windowId, ModalResult result)
    {
        if (_disposed) return;

        // TryRemove ensures exactly one code path (CompleteModal vs OnChildWindowClosed vs CancellationToken)
        // takes ownership of the TCS and parent re-enable. Prevents double EnableParentWindow
        // when user clicks X at the same time as CompleteModal is called.
        if (!_modalCompletions.TryRemove(windowId, out var tcs))
        {
            _logger.LogWarning("CompleteModal: no modal completion found for window '{WindowId}'", windowId);
            return;
        }

        tcs.TrySetResult(result);

        // We own the cleanup since we successfully removed the TCS
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

    /// <remarks>
    /// PhotinoWindow does not implement IDisposable. Native HWND resources are freed by the
    /// Win32 message pump when it processes the WM_CLOSE → WM_DESTROY sequence. We post
    /// WM_CLOSE to every tracked child window and null the managed reference for GC.
    ///
    /// If the message pump itself has crashed (catastrophic failure), native handles leak — but
    /// the OS reclaims all handles when the process exits. This is the standard Win32 contract
    /// and not something we can guard against at the managed layer.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Unblock any callers waiting for main window registration.
        // They'll proceed, see _mainPhotinoWindow is still null, and get a clear exception.
        _mainWindowReady.TrySetResult();

        var windowCount = _windows.Count;
        _logger.LogDebug("WindowService disposing — closing {Count} child window(s)", windowCount);

        // PostMessage is non-blocking (unlike SendMessage) so this is safe from any thread,
        // including the Photino message pump thread. The WM_CLOSE messages are queued and
        // processed asynchronously by the pump — no deadlock risk.
        var closedCount = 0;
        foreach (var kvp in _windows)
        {
            var windowInfo = kvp.Value;
            if (windowInfo.WindowHandle != IntPtr.Zero)
            {
                _modalBackend.PostCloseMessage(windowInfo.WindowHandle);
                closedCount++;
            }
            else
            {
                _logger.LogWarning("WindowService Dispose: window '{WindowId}' has no handle — cannot send WM_CLOSE",
                    windowInfo.WindowId);
            }
            windowInfo.PhotinoWindow = null;
        }

        if (closedCount != windowCount)
        {
            _logger.LogWarning("WindowService Dispose: sent WM_CLOSE to {Closed}/{Total} windows — " +
                "{Remaining} window(s) had no handle", closedCount, windowCount, windowCount - closedCount);
        }

        // Complete any outstanding modal TCS with Cancel
        foreach (var kvp in _modalCompletions)
        {
            kvp.Value.TrySetResult(ModalResult.Cancel());
        }

        _windows.Clear();
        _modalCompletions.Clear();
        _registeredComponents.Clear();
        _modalBackend.Dispose();

        _logger.LogDebug("WindowService disposed");
    }

    // ── Internal ─────────────────────────────────────────────────────────────

    private async Task<string> CreateWindowCoreAsync(
        WindowOptions options,
        bool isModal,
        TaskCompletionSource<ModalResult>? modalTcs = null,
        CancellationToken cancellationToken = default)
    {
        var windowId = Guid.NewGuid().ToString("N")[..12];

        // Register component type in whitelist before building the URL.
        // WindowHost.razor will only instantiate types that appear here.
        if (options.ComponentType is not null)
        {
            var fullName = options.ComponentType.FullName!;

            // TryAdd is idempotent — re-registering the same type is a no-op.
            // The cap only guards against pathological scenarios with hundreds of distinct types.
            if (!_registeredComponents.ContainsKey(fullName)
                && _registeredComponents.Count >= Constants.Window.MaxRegisteredComponentTypes)
            {
                _logger.LogWarning("Component type whitelist is full ({Max} types). " +
                    "Refusing to register '{TypeName}'. Increase MaxRegisteredComponentTypes if this is intentional.",
                    Constants.Window.MaxRegisteredComponentTypes, fullName);
                CleanupFailedWindow(windowId);
                throw new InvalidOperationException(
                    $"Component type whitelist is full ({Constants.Window.MaxRegisteredComponentTypes} types). " +
                    $"Cannot register '{fullName}'.");
            }

            _registeredComponents.TryAdd(fullName, options.ComponentType);
            _logger.LogDebug("Registered component type '{TypeName}' for window hosting", fullName);
        }

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

        // Wait for main window registration if not yet available — handles early callers
        // (e.g., splash screen scenarios) where CreateWindowAsync is called before the main
        // window's RegisterWindowCreatedHandler fires during WaitForClose().
        if (_mainPhotinoWindow is null)
        {
            _logger.LogDebug("Main window not yet registered — waiting up to {Timeout}ms", Constants.Window.HandleReadyTimeoutMs);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(Constants.Window.HandleReadyTimeoutMs);

            try
            {
                await _mainWindowReady.Task.WaitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Timeout expired, not caller cancellation
                CleanupFailedWindow(windowId);
                throw new TimeoutException(
                    $"Main window was not registered within {Constants.Window.HandleReadyTimeoutMs}ms. " +
                    "Cannot create child windows before the main window is ready.");
            }
        }

        // Post-wait null check: handles edge case where Dispose() signaled the TCS
        // but main window was never actually registered.
        if (_mainPhotinoWindow is null)
        {
            _logger.LogError("Cannot create child window — main window not registered");
            CleanupFailedWindow(windowId);
            throw new InvalidOperationException("Main window has not been registered. Cannot create child windows.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Synchronization gate: WindowCreatedHandler fires during WaitForClose() on the Photino
        // thread before WaitForClose returns. This is synchronous in current Photino, but we use
        // an explicit signal to make the contract clear and defend against future Photino changes.
        var handleReady = new ManualResetEventSlim(false);

        try
        {
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
                        // PhotinoWindow does not implement IDisposable. Native window resources are
                        // freed by the Win32 message pump when WM_CLOSE → WM_DESTROY is processed.
                        // The managed PhotinoWindow reference is nulled in OnChildWindowClosed() to
                        // allow GC of the managed wrapper — the native HWND is already gone by then.
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
            }, cancellationToken);

            // Wait for the handle signal with timeout — defends against Photino changes
            // where WaitForClose behavior might differ
            if (!handleReady.IsSet && !handleReady.Wait(Constants.Window.HandleReadyTimeoutMs, cancellationToken))
            {
                _logger.LogError("Child window '{WindowId}' handle was not ready within {Timeout}ms",
                    windowId, Constants.Window.HandleReadyTimeoutMs);

                CleanupPartialWindow(windowInfo);
                CleanupFailedWindow(windowId);
                throw new TimeoutException($"Child window '{windowId}' native handle was not ready within {Constants.Window.HandleReadyTimeoutMs}ms");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("Child window '{WindowId}' creation was cancelled", windowId);
            CleanupPartialWindow(windowInfo);
            CleanupFailedWindow(windowId);
            throw;
        }
        finally
        {
            handleReady.Dispose();
        }

        if (windowInfo.WindowHandle == IntPtr.Zero)
        {
            _logger.LogError("Child window '{WindowId}' was created but handle is not available", windowId);
            CleanupPartialWindow(windowInfo);
            CleanupFailedWindow(windowId);
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
        // TryRemove ensures exactly one code path takes ownership (see CompleteModal).
        if (_modalCompletions.TryRemove(windowId, out var tcs))
        {
            tcs.TrySetResult(ModalResult.Cancel());

            // We own the cleanup since we successfully removed the TCS
            if (_windows.TryGetValue(windowId, out var modalWindowInfo))
            {
                var parentHandle = ResolveParentHandle(modalWindowInfo.ParentWindowId);
                if (parentHandle != IntPtr.Zero)
                {
                    _modalBackend.EnableParentWindow(parentHandle);
                }
            }
        }

        // Release managed reference to PhotinoWindow (native resources freed by message pump)
        if (_windows.TryRemove(windowId, out var removedInfo))
        {
            removedInfo.PhotinoWindow = null;
        }

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

    /// <summary>
    /// Attempt to close a partially-created window that may have a handle but failed setup.
    /// </summary>
    private void CleanupPartialWindow(WindowInfo windowInfo)
    {
        if (windowInfo.WindowHandle != IntPtr.Zero)
        {
            _logger.LogDebug("Cleaning up partially-created window '{WindowId}' (handle={Handle})",
                windowInfo.WindowId, windowInfo.WindowHandle);
            _modalBackend.PostCloseMessage(windowInfo.WindowHandle);
        }
        windowInfo.PhotinoWindow = null;
    }

    /// <summary>
    /// Remove tracking state for a window that failed to fully initialize.
    /// </summary>
    private void CleanupFailedWindow(string windowId)
    {
        _windows.TryRemove(windowId, out _);
        _modalCompletions.TryRemove(windowId, out _);
    }

    private string BuildWindowUrl(WindowOptions options, string windowId)
    {
        var baseUrl = _blazorHost.BaseUrl;

        if (options.ComponentType is not null)
        {
            // Component type mode: use FullName as the lookup key for the whitelist
            var typeName = Uri.EscapeDataString(options.ComponentType.FullName!);
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
