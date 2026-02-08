namespace CheapAvaloniaBlazor.Models;

/// <summary>
/// Definition for a native menu bar item (top-level menus, sub-items, separators, checkable items).
/// Top-level items are typically submenus: CreateSubMenu("&amp;File", [...]).
/// The &amp; character marks Win32 mnemonic/accelerator keys (Alt+F opens File menu).
/// </summary>
public class MenuItemDefinition
{
    /// <summary>
    /// Unique identifier for the menu item (used for dynamic updates via EnableMenuItem/CheckMenuItem)
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Display text for the menu item. Use &amp; before a character for Win32 mnemonics (e.g. "&amp;File").
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Whether this menu item is a separator line
    /// </summary>
    public bool IsSeparator { get; set; }

    /// <summary>
    /// Whether the menu item is enabled (grayed out when false)
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Whether the menu item is currently checked
    /// </summary>
    public bool IsChecked { get; set; }

    /// <summary>
    /// Whether the menu item can be checked/unchecked on click
    /// </summary>
    public bool IsCheckable { get; set; }

    /// <summary>
    /// Display-only accelerator text shown right-aligned in the menu item (e.g. "Ctrl+S").
    /// This is cosmetic only — actual keyboard binding should be done via IHotkeyService.
    /// </summary>
    public string? Accelerator { get; set; }

    /// <summary>
    /// Synchronous click handler
    /// </summary>
    public Action? OnClick { get; set; }

    /// <summary>
    /// Asynchronous click handler
    /// </summary>
    public Func<Task>? OnClickAsync { get; set; }

    /// <summary>
    /// Child items for submenus (dropdown menus under this item)
    /// </summary>
    public List<MenuItemDefinition>? SubItems { get; set; }

    /// <summary>
    /// Create a separator menu item
    /// </summary>
    public static MenuItemDefinition Separator() => new() { IsSeparator = true };

    /// <summary>
    /// Create a menu item with synchronous click handler
    /// </summary>
    /// <param name="text">Display text (use &amp; for mnemonic)</param>
    /// <param name="onClick">Click handler</param>
    /// <param name="id">Optional unique identifier for dynamic updates</param>
    /// <param name="accelerator">Optional display-only accelerator text (e.g. "Ctrl+N")</param>
    /// <param name="isEnabled">Whether the item starts enabled</param>
    public static MenuItemDefinition Create(
        string text,
        Action onClick,
        string? id = null,
        string? accelerator = null,
        bool isEnabled = true)
    {
        return new MenuItemDefinition
        {
            Id = id,
            Text = text,
            OnClick = onClick,
            Accelerator = accelerator,
            IsEnabled = isEnabled
        };
    }

    /// <summary>
    /// Create a menu item with asynchronous click handler
    /// </summary>
    /// <param name="text">Display text (use &amp; for mnemonic)</param>
    /// <param name="onClickAsync">Async click handler</param>
    /// <param name="id">Optional unique identifier for dynamic updates</param>
    /// <param name="accelerator">Optional display-only accelerator text</param>
    /// <param name="isEnabled">Whether the item starts enabled</param>
    public static MenuItemDefinition CreateAsync(
        string text,
        Func<Task> onClickAsync,
        string? id = null,
        string? accelerator = null,
        bool isEnabled = true)
    {
        return new MenuItemDefinition
        {
            Id = id,
            Text = text,
            OnClickAsync = onClickAsync,
            Accelerator = accelerator,
            IsEnabled = isEnabled
        };
    }

    /// <summary>
    /// Create a checkable menu item
    /// </summary>
    /// <param name="text">Display text</param>
    /// <param name="isChecked">Initial checked state</param>
    /// <param name="onClick">Click handler (fired after check state is toggled)</param>
    /// <param name="id">Optional unique identifier for dynamic updates</param>
    /// <param name="accelerator">Optional display-only accelerator text</param>
    public static MenuItemDefinition CreateCheckable(
        string text,
        bool isChecked,
        Action onClick,
        string? id = null,
        string? accelerator = null)
    {
        return new MenuItemDefinition
        {
            Id = id,
            Text = text,
            IsCheckable = true,
            IsChecked = isChecked,
            OnClick = onClick,
            Accelerator = accelerator
        };
    }

    /// <summary>
    /// Create a submenu (dropdown) item containing child items
    /// </summary>
    /// <param name="text">Display text (use &amp; for mnemonic, e.g. "&amp;File")</param>
    /// <param name="subItems">Child menu items</param>
    /// <param name="id">Optional unique identifier</param>
    public static MenuItemDefinition CreateSubMenu(
        string text,
        IEnumerable<MenuItemDefinition> subItems,
        string? id = null)
    {
        return new MenuItemDefinition
        {
            Id = id,
            Text = text,
            SubItems = subItems.ToList()
        };
    }
}
