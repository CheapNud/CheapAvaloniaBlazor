using System.ComponentModel;
using Microsoft.Extensions.Logging;

namespace CheapAvaloniaBlazor.Services;

/// <summary>
/// Singleton service that tracks Photino window lifecycle state and fires events.
/// Internal On* methods are called by BlazorHostWindow when Photino events fire.
/// </summary>
public class AppLifecycleService : IAppLifecycleService
{
    private readonly ILogger<AppLifecycleService>? _logger;

    public event EventHandler<CancelEventArgs>? Closing;
    public event Action? Minimized;
    public event Action? Maximized;
    public event Action? Restored;
    public event Action? Activated;
    public event Action? Deactivated;

    // volatile: state is written on Photino thread, read on Blazor render thread
    private volatile bool _isMinimized;
    private volatile bool _isMaximized;
    private volatile bool _isFocused;

    public bool IsMinimized => _isMinimized;
    public bool IsMaximized => _isMaximized;
    public bool IsFocused => _isFocused;

    public AppLifecycleService(ILogger<AppLifecycleService>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called by BlazorHostWindow when the Photino window is about to close.
    /// Returns true if any subscriber cancelled the close.
    /// </summary>
    internal bool OnClosing()
    {
        _logger?.LogDebug("AppLifecycleService: Closing event firing");

        var cancelArgs = new CancelEventArgs();
        Closing?.Invoke(this, cancelArgs);

        if (cancelArgs.Cancel)
            _logger?.LogDebug("AppLifecycleService: Close was cancelled by a subscriber");

        return cancelArgs.Cancel;
    }

    /// <summary>
    /// Called by BlazorHostWindow when the Photino window is minimized
    /// </summary>
    internal void OnMinimized()
    {
        _isMinimized = true;
        _logger?.LogDebug("AppLifecycleService: Window minimized");
        Minimized?.Invoke();
    }

    /// <summary>
    /// Called by BlazorHostWindow when the Photino window is maximized
    /// </summary>
    internal void OnMaximized()
    {
        _isMaximized = true;
        _isMinimized = false;
        _logger?.LogDebug("AppLifecycleService: Window maximized");
        Maximized?.Invoke();
    }

    /// <summary>
    /// Called by BlazorHostWindow when the Photino window is restored
    /// </summary>
    internal void OnRestored()
    {
        _isMinimized = false;
        _isMaximized = false;
        _logger?.LogDebug("AppLifecycleService: Window restored");
        Restored?.Invoke();
    }

    /// <summary>
    /// Called by BlazorHostWindow when the Photino window gains focus
    /// </summary>
    internal void OnActivated()
    {
        _isFocused = true;
        _logger?.LogDebug("AppLifecycleService: Window activated");
        Activated?.Invoke();
    }

    /// <summary>
    /// Called by BlazorHostWindow when the Photino window loses focus
    /// </summary>
    internal void OnDeactivated()
    {
        _isFocused = false;
        _logger?.LogDebug("AppLifecycleService: Window deactivated");
        Deactivated?.Invoke();
    }
}
