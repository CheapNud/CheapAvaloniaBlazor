using CheapAvaloniaBlazor.Models;

namespace CheapAvaloniaBlazor.Services;

/// <summary>
/// Service for creating a native menu bar on the application window.
/// Supported on Windows (Win32 native menu). Other platforms return IsSupported=false.
/// Accelerator text (e.g. "Ctrl+S") is display-only — use IHotkeyService for actual keyboard bindings.
/// </summary>
public interface IMenuBarService : IDisposable
{
    /// <summary>
    /// Whether native menu bars are supported on the current platform.
    /// True on Windows, false on Linux/macOS.
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// Replace the entire menu bar with new definitions.
    /// Top-level items should be submenus created via MenuItemDefinition.CreateSubMenu().
    /// </summary>
    /// <param name="menus">Top-level menu definitions</param>
    void SetMenuBar(IEnumerable<MenuItemDefinition> menus);

    /// <summary>
    /// Enable or disable a specific menu item by its string ID.
    /// </summary>
    /// <param name="menuItemId">The MenuItemDefinition.Id value</param>
    /// <param name="enabled">Whether the item should be enabled</param>
    void EnableMenuItem(string menuItemId, bool enabled);

    /// <summary>
    /// Check or uncheck a specific menu item by its string ID.
    /// </summary>
    /// <param name="menuItemId">The MenuItemDefinition.Id value</param>
    /// <param name="isChecked">Whether the item should be checked</param>
    void CheckMenuItem(string menuItemId, bool isChecked);

    /// <summary>
    /// Fired when a menu item with an ID is clicked.
    /// </summary>
    event Action<string>? MenuItemClicked;
}
