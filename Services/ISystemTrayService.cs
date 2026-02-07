using Avalonia.Controls;
using CheapAvaloniaBlazor.Models;

namespace CheapAvaloniaBlazor.Services;

/// <summary>
/// Service interface for system tray icon management
/// </summary>
public interface ISystemTrayService : IDisposable
{
    /// <summary>
    /// Whether the tray icon is currently visible
    /// </summary>
    bool IsVisible { get; }

    /// <summary>
    /// Show the system tray icon
    /// </summary>
    void ShowTrayIcon();

    /// <summary>
    /// Hide the system tray icon
    /// </summary>
    void HideTrayIcon();

    /// <summary>
    /// Set the tray icon from a file path
    /// </summary>
    /// <param name="iconPath">Path to the icon file</param>
    void SetTrayIcon(string iconPath);

    /// <summary>
    /// Set the tray icon from a WindowIcon
    /// </summary>
    /// <param name="icon">The icon to use</param>
    void SetTrayIcon(WindowIcon icon);

    /// <summary>
    /// Set the tooltip text for the tray icon
    /// </summary>
    /// <param name="tooltip">Tooltip text</param>
    void SetTrayTooltip(string tooltip);

    /// <summary>
    /// Set the context menu items for the tray icon
    /// </summary>
    /// <param name="menuItems">Menu item definitions</param>
    void SetTrayMenu(IEnumerable<TrayMenuItemDefinition> menuItems);

    /// <summary>
    /// Add a single menu item to the tray context menu
    /// </summary>
    /// <param name="menuItem">Menu item definition</param>
    void AddTrayMenuItem(TrayMenuItemDefinition menuItem);

    /// <summary>
    /// Clear all custom menu items from the tray context menu
    /// </summary>
    void ClearTrayMenu();

    /// <summary>
    /// Minimize the application to the system tray
    /// </summary>
    void MinimizeToTray();

    /// <summary>
    /// Restore the application from the system tray
    /// </summary>
    void RestoreFromTray();

    /// <summary>
    /// Event fired when the tray icon is clicked
    /// </summary>
    event Action? TrayIconClicked;

    /// <summary>
    /// Event fired when the tray icon is double-clicked
    /// </summary>
    event Action? TrayIconDoubleClicked;
}
