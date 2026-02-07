using System.ComponentModel;

namespace CheapAvaloniaBlazor.Services;

/// <summary>
/// Service for subscribing to native window lifecycle events from Photino.
/// Provides events for window state changes and read-only state properties.
/// </summary>
public interface IAppLifecycleService
{
    /// <summary>
    /// Fired when the window is about to close. Set Cancel = true to prevent closing.
    /// This fires before close-to-tray logic - if cancelled, the window stays open.
    /// </summary>
    event EventHandler<CancelEventArgs>? Closing;

    /// <summary>
    /// Fired when the window is minimized
    /// </summary>
    event Action? Minimized;

    /// <summary>
    /// Fired when the window is maximized
    /// </summary>
    event Action? Maximized;

    /// <summary>
    /// Fired when the window is restored from minimized or maximized state
    /// </summary>
    event Action? Restored;

    /// <summary>
    /// Fired when the window gains focus
    /// </summary>
    event Action? Activated;

    /// <summary>
    /// Fired when the window loses focus
    /// </summary>
    event Action? Deactivated;

    /// <summary>
    /// Whether the window is currently minimized
    /// </summary>
    bool IsMinimized { get; }

    /// <summary>
    /// Whether the window is currently maximized
    /// </summary>
    bool IsMaximized { get; }

    /// <summary>
    /// Whether the window currently has focus
    /// </summary>
    bool IsFocused { get; }
}
