using CheapAvaloniaBlazor.Models;
using Photino.NET;

namespace CheapAvaloniaBlazor.Services.Backends;

/// <summary>
/// Platform-specific backend for native menu bar rendering.
/// Windows: Win32 CreateMenu + WndProc subclassing.
/// Linux: GTK 3 menu bar packed above the webview.
/// Other platforms: NullMenuBarBackend (no-op).
/// </summary>
internal interface IMenuBarBackend : IDisposable
{
    /// <summary>
    /// Whether the native menu bar is supported on the current platform
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// Attach the menu bar to the window and build the initial menus.
    /// Backends extract what they need from the PhotinoWindow — the HWND on Windows,
    /// the GTK toplevel on Linux (Photino exposes no native handles there).
    /// </summary>
    /// <param name="window">The Photino window to attach to (native window already created)</param>
    /// <param name="menus">Top-level menu definitions (typically submenus like File, Edit, Help)</param>
    void Initialize(PhotinoWindow window, IEnumerable<MenuItemDefinition> menus);

    /// <summary>
    /// Replace the entire menu bar with new definitions
    /// </summary>
    /// <param name="menus">New top-level menu definitions</param>
    void SetMenuBar(IEnumerable<MenuItemDefinition> menus);

    /// <summary>
    /// Enable or disable a menu item by its string ID
    /// </summary>
    /// <param name="menuItemId">The MenuItemDefinition.Id</param>
    /// <param name="enabled">Whether the item should be enabled</param>
    void EnableMenuItem(string menuItemId, bool enabled);

    /// <summary>
    /// Check or uncheck a menu item by its string ID
    /// </summary>
    /// <param name="menuItemId">The MenuItemDefinition.Id</param>
    /// <param name="isChecked">Whether the item should be checked</param>
    void CheckMenuItem(string menuItemId, bool isChecked);

    /// <summary>
    /// Fired when a menu item is clicked, with the string ID of the item
    /// </summary>
    event Action<string>? MenuItemClicked;
}
