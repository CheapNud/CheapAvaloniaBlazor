using Avalonia.Threading;
using CheapAvaloniaBlazor.Models;
using CheapAvaloniaBlazor.Services.Backends;
using Microsoft.Extensions.Logging;

namespace CheapAvaloniaBlazor.Services;

/// <summary>
/// Cross-platform orchestrator for native menu bars.
/// Selects the appropriate platform backend (Windows or Null) and relays events.
/// </summary>
public sealed class MenuBarService : IMenuBarService, IDisposable
{
    private readonly ILogger<MenuBarService> _logger;
    private readonly IMenuBarBackend _backend;
    private readonly object _initLock = new();

    private List<MenuItemDefinition>? _pendingMenuItems;
    private volatile bool _disposed;
    private bool _initialized;

    public bool IsSupported => _backend.IsSupported;

    public event Action<string>? MenuItemClicked;

    public MenuBarService(ILogger<MenuBarService> logger)
    {
        _logger = logger;
        _backend = CreateBackend(logger);
        _backend.MenuItemClicked += OnBackendMenuItemClicked;

        if (_backend is WindowsMenuBarBackend windowsBackend)
            windowsBackend.AsyncExceptionOccurred += OnAsyncException;

        _logger.LogDebug("MenuBarService initialized with backend {Backend} (supported={Supported})",
            _backend.GetType().Name, _backend.IsSupported);
    }

    /// <summary>
    /// Called by BlazorHostWindow after the Photino window is created.
    /// Attaches the native menu bar to the window handle.
    /// </summary>
    internal void Initialize(IntPtr windowHandle, IEnumerable<MenuItemDefinition>? menuItems)
    {
        if (_disposed) return;
        if (windowHandle == IntPtr.Zero) return;
        if (!_backend.IsSupported) return;

        lock (_initLock)
        {
            if (_initialized) return;

            var menus = menuItems ?? _pendingMenuItems;
            if (menus is null) return;

            _backend.Initialize(windowHandle, menus);
            _pendingMenuItems = null;
            _initialized = true;
        }

        _logger.LogInformation("Native menu bar initialized on window handle {Handle}", windowHandle);
    }

    /// <summary>
    /// Called from the builder to store menu items before the window exists.
    /// </summary>
    internal void SetPendingMenuItems(IEnumerable<MenuItemDefinition> menus)
    {
        lock (_initLock)
        {
            _pendingMenuItems = menus.ToList();
        }
    }

    public void SetMenuBar(IEnumerable<MenuItemDefinition> menus)
    {
        if (_disposed) return;
        _backend.SetMenuBar(menus);
    }

    public void EnableMenuItem(string menuItemId, bool enabled)
    {
        if (_disposed) return;
        _backend.EnableMenuItem(menuItemId, enabled);
    }

    public void CheckMenuItem(string menuItemId, bool isChecked)
    {
        if (_disposed) return;
        _backend.CheckMenuItem(menuItemId, isChecked);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _backend.MenuItemClicked -= OnBackendMenuItemClicked;

        if (_backend is WindowsMenuBarBackend windowsBackend)
            windowsBackend.AsyncExceptionOccurred -= OnAsyncException;

        _backend.Dispose();
    }

    private void OnBackendMenuItemClicked(string menuItemId)
    {
        // Backend fires from Win32 WndProc thread — marshal to Avalonia dispatcher
        // so Blazor UI subscribers can safely interact with the UI.
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                MenuItemClicked?.Invoke(menuItemId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MenuItemClicked event handler threw for '{Id}'", menuItemId);
            }
        });
    }

    private void OnAsyncException(Exception ex)
    {
        _logger.LogError(ex, "Async menu item callback threw an unhandled exception");
    }

    private static IMenuBarBackend CreateBackend(ILogger logger)
    {
        if (OperatingSystem.IsWindows())
            return new WindowsMenuBarBackend(logger);

        return new NullMenuBarBackend();
    }
}
