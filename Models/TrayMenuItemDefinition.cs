namespace CheapAvaloniaBlazor.Models;

/// <summary>
/// Definition for a system tray context menu item
/// </summary>
public class TrayMenuItemDefinition
{
    /// <summary>
    /// Unique identifier for the menu item (used for dynamic updates)
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Display text for the menu item
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Whether this menu item is a separator
    /// </summary>
    public bool IsSeparator { get; set; }

    /// <summary>
    /// Whether the menu item is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Whether the menu item is checked (for checkable items)
    /// </summary>
    public bool IsChecked { get; set; }

    /// <summary>
    /// Whether the menu item can be checked/unchecked
    /// </summary>
    public bool IsCheckable { get; set; }

    /// <summary>
    /// Synchronous click handler
    /// </summary>
    public Action? OnClick { get; set; }

    /// <summary>
    /// Asynchronous click handler
    /// </summary>
    public Func<Task>? OnClickAsync { get; set; }

    /// <summary>
    /// Sub-menu items for nested menus
    /// </summary>
    public List<TrayMenuItemDefinition>? SubMenuItems { get; set; }

    /// <summary>
    /// Create a separator menu item
    /// </summary>
    public static TrayMenuItemDefinition Separator() => new() { IsSeparator = true };

    /// <summary>
    /// Create a menu item with synchronous click handler
    /// </summary>
    /// <param name="text">Display text</param>
    /// <param name="onClick">Click handler</param>
    /// <param name="id">Optional unique identifier</param>
    /// <param name="isEnabled">Whether the item is enabled</param>
    public static TrayMenuItemDefinition Create(
        string text,
        Action onClick,
        string? id = null,
        bool isEnabled = true)
    {
        return new TrayMenuItemDefinition
        {
            Id = id,
            Text = text,
            OnClick = onClick,
            IsEnabled = isEnabled
        };
    }

    /// <summary>
    /// Create a menu item with asynchronous click handler
    /// </summary>
    /// <param name="text">Display text</param>
    /// <param name="onClickAsync">Async click handler</param>
    /// <param name="id">Optional unique identifier</param>
    /// <param name="isEnabled">Whether the item is enabled</param>
    public static TrayMenuItemDefinition CreateAsync(
        string text,
        Func<Task> onClickAsync,
        string? id = null,
        bool isEnabled = true)
    {
        return new TrayMenuItemDefinition
        {
            Id = id,
            Text = text,
            OnClickAsync = onClickAsync,
            IsEnabled = isEnabled
        };
    }

    /// <summary>
    /// Create a checkable menu item
    /// </summary>
    /// <param name="text">Display text</param>
    /// <param name="isChecked">Initial checked state</param>
    /// <param name="onClick">Click handler (receives toggled state)</param>
    /// <param name="id">Optional unique identifier</param>
    public static TrayMenuItemDefinition CreateCheckable(
        string text,
        bool isChecked,
        Action onClick,
        string? id = null)
    {
        return new TrayMenuItemDefinition
        {
            Id = id,
            Text = text,
            IsCheckable = true,
            IsChecked = isChecked,
            OnClick = onClick
        };
    }

    /// <summary>
    /// Create a submenu item
    /// </summary>
    /// <param name="text">Display text</param>
    /// <param name="subItems">Sub-menu items</param>
    /// <param name="id">Optional unique identifier</param>
    public static TrayMenuItemDefinition CreateSubMenu(
        string text,
        IEnumerable<TrayMenuItemDefinition> subItems,
        string? id = null)
    {
        return new TrayMenuItemDefinition
        {
            Id = id,
            Text = text,
            SubMenuItems = subItems.ToList()
        };
    }
}
