using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using CheapAvaloniaBlazor.Models;
using Microsoft.Extensions.Logging;

namespace CheapAvaloniaBlazor.Services.Backends;

/// <summary>
/// Windows native menu bar backend using Win32 CreateMenu + WndProc subclassing.
/// Attaches a native menu bar to the Photino HWND and handles WM_COMMAND messages for menu clicks.
/// </summary>
internal sealed class WindowsMenuBarBackend : IMenuBarBackend
{
    private readonly ILogger _logger;

    private IntPtr _windowHandle;
    private IntPtr _menuBarHandle;
    private IntPtr _originalWndProc;

    // CRITICAL: Must be stored as an instance field to prevent GC from collecting the delegate.
    // If GC collects this while the WndProc is still subclassed, the app crashes with access violation.
    private WndProcDelegate? _wndProcDelegate;

    private readonly Dictionary<int, MenuItemDefinition> _win32IdToDefinition = [];
    private readonly Dictionary<string, int> _stringIdToWin32Id = [];
    private int _nextMenuId = Constants.MenuBar.FirstMenuItemId;

    private volatile bool _disposed;

    public bool IsSupported => OperatingSystem.IsWindows();

    public event Action<string>? MenuItemClicked;

    public WindowsMenuBarBackend(ILogger logger)
    {
        _logger = logger;
    }

    [SupportedOSPlatform("windows")]
    public void Initialize(IntPtr windowHandle, IEnumerable<MenuItemDefinition> menus)
    {
        if (_disposed) return;
        if (windowHandle == IntPtr.Zero) return;

        _windowHandle = windowHandle;

        // Subclass the window to intercept WM_COMMAND
        _wndProcDelegate = WndProc;
        _originalWndProc = SetWindowLongPtr(_windowHandle, GWLP_WNDPROC,
            Marshal.GetFunctionPointerForDelegate(_wndProcDelegate));

        _logger.LogDebug("WindowsMenuBarBackend: Subclassed WndProc on HWND {Handle}", windowHandle);

        BuildAndAttachMenuBar(menus);
    }

    [SupportedOSPlatform("windows")]
    public void SetMenuBar(IEnumerable<MenuItemDefinition> menus)
    {
        if (_disposed) return;
        if (_windowHandle == IntPtr.Zero) return;

        // Destroy old menu bar
        if (_menuBarHandle != IntPtr.Zero)
        {
            SetMenu(_windowHandle, IntPtr.Zero);
            DestroyMenu(_menuBarHandle);
            _menuBarHandle = IntPtr.Zero;
        }

        _win32IdToDefinition.Clear();
        _stringIdToWin32Id.Clear();
        _nextMenuId = Constants.MenuBar.FirstMenuItemId;

        BuildAndAttachMenuBar(menus);
    }

    [SupportedOSPlatform("windows")]
    public void EnableMenuItem(string menuItemId, bool enabled)
    {
        if (_disposed) return;
        if (!_stringIdToWin32Id.TryGetValue(menuItemId, out var win32Id)) return;

        var flags = MF_BYCOMMAND | (enabled ? MF_ENABLED : MF_GRAYED);
        EnableMenuItemNative(_menuBarHandle, (uint)win32Id, flags);
        DrawMenuBar(_windowHandle);

        // Update the definition state
        if (_win32IdToDefinition.TryGetValue(win32Id, out var definition))
            definition.IsEnabled = enabled;
    }

    [SupportedOSPlatform("windows")]
    public void CheckMenuItem(string menuItemId, bool isChecked)
    {
        if (_disposed) return;
        if (!_stringIdToWin32Id.TryGetValue(menuItemId, out var win32Id)) return;

        var flags = MF_BYCOMMAND | (isChecked ? MF_CHECKED : MF_UNCHECKED);
        CheckMenuItemNative(_menuBarHandle, (uint)win32Id, flags);
        DrawMenuBar(_windowHandle);

        // Update the definition state
        if (_win32IdToDefinition.TryGetValue(win32Id, out var definition))
            definition.IsChecked = isChecked;
    }

    [SupportedOSPlatform("windows")]
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // ORDER MATTERS: Restore WndProc FIRST, then destroy menu.
        // Pending WM_COMMAND messages could reference freed memory otherwise.
        if (_originalWndProc != IntPtr.Zero && _windowHandle != IntPtr.Zero)
        {
            try
            {
                SetWindowLongPtr(_windowHandle, GWLP_WNDPROC, _originalWndProc);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "WindowsMenuBarBackend: Failed to restore original WndProc");
            }
            _originalWndProc = IntPtr.Zero;
        }

        if (_menuBarHandle != IntPtr.Zero)
        {
            try
            {
                if (_windowHandle != IntPtr.Zero)
                    SetMenu(_windowHandle, IntPtr.Zero);
                DestroyMenu(_menuBarHandle);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "WindowsMenuBarBackend: Failed to destroy menu");
            }
            _menuBarHandle = IntPtr.Zero;
        }

        _win32IdToDefinition.Clear();
        _stringIdToWin32Id.Clear();

        _logger.LogDebug("WindowsMenuBarBackend: Disposed");
    }

    [SupportedOSPlatform("windows")]
    private void BuildAndAttachMenuBar(IEnumerable<MenuItemDefinition> menus)
    {
        _menuBarHandle = CreateMenu();

        foreach (var topLevelMenu in menus)
        {
            if (topLevelMenu.SubItems is { Count: > 0 })
            {
                var popupHandle = BuildPopupMenu(topLevelMenu.SubItems);
                AppendMenu(_menuBarHandle, MF_POPUP, (uint)popupHandle, topLevelMenu.Text);
            }
            else
            {
                // Top-level item without children (unusual but supported)
                var menuId = AssignMenuId(topLevelMenu);
                var flags = MF_STRING;
                if (!topLevelMenu.IsEnabled) flags |= MF_GRAYED;
                AppendMenu(_menuBarHandle, flags, (uint)menuId, topLevelMenu.Text);
            }
        }

        SetMenu(_windowHandle, _menuBarHandle);
        DrawMenuBar(_windowHandle);

        _logger.LogDebug("WindowsMenuBarBackend: Built menu bar with {Count} top-level items", _win32IdToDefinition.Count);
    }

    [SupportedOSPlatform("windows")]
    private IntPtr BuildPopupMenu(List<MenuItemDefinition> items)
    {
        var popupHandle = CreatePopupMenu();

        foreach (var menuItem in items)
        {
            if (menuItem.IsSeparator)
            {
                AppendMenu(popupHandle, MF_SEPARATOR, 0, null);
                continue;
            }

            if (menuItem.SubItems is { Count: > 0 })
            {
                // Nested submenu
                var nestedPopup = BuildPopupMenu(menuItem.SubItems);
                AppendMenu(popupHandle, MF_POPUP | MF_STRING, (uint)nestedPopup, menuItem.Text);
                continue;
            }

            var menuId = AssignMenuId(menuItem);
            var displayText = BuildDisplayText(menuItem);

            uint flags = MF_STRING;
            if (!menuItem.IsEnabled) flags |= MF_GRAYED;
            if (menuItem.IsCheckable && menuItem.IsChecked) flags |= MF_CHECKED;

            AppendMenu(popupHandle, flags, (uint)menuId, displayText);
        }

        return popupHandle;
    }

    private int AssignMenuId(MenuItemDefinition definition)
    {
        var menuId = _nextMenuId++;
        _win32IdToDefinition[menuId] = definition;

        if (!string.IsNullOrEmpty(definition.Id))
            _stringIdToWin32Id[definition.Id] = menuId;

        return menuId;
    }

    private static string BuildDisplayText(MenuItemDefinition definition)
    {
        if (string.IsNullOrEmpty(definition.Accelerator))
            return definition.Text;

        return $"{definition.Text}\t{definition.Accelerator}";
    }

    [SupportedOSPlatform("windows")]
    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_COMMAND)
        {
            var commandId = LOWORD(wParam);
            var notificationCode = HIWORD(wParam);

            // notificationCode == 0 means menu click (not accelerator or control notification)
            if (notificationCode == 0 && _win32IdToDefinition.TryGetValue(commandId, out var definition))
            {
                HandleMenuItemClick(definition);
                return IntPtr.Zero;
            }
        }

        return CallWindowProc(_originalWndProc, hWnd, msg, wParam, lParam);
    }

    private void HandleMenuItemClick(MenuItemDefinition definition)
    {
        // Toggle check state for checkable items
        if (definition.IsCheckable)
        {
            definition.IsChecked = !definition.IsChecked;

            if (OperatingSystem.IsWindows() && !string.IsNullOrEmpty(definition.Id))
            {
                CheckMenuItem(definition.Id, definition.IsChecked);
            }
        }

        // Invoke sync callback
        if (definition.OnClick is not null)
        {
            try
            {
                definition.OnClick();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Menu item OnClick threw for '{Text}'", definition.Text);
            }
        }

        // Invoke async callback (fire-and-forget since WndProc is synchronous)
        if (definition.OnClickAsync is not null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await definition.OnClickAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Menu item OnClickAsync threw for '{Text}'", definition.Text);
                }
            });
        }

        // Fire MenuItemClicked event with string ID
        if (!string.IsNullOrEmpty(definition.Id))
        {
            try
            {
                MenuItemClicked?.Invoke(definition.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MenuItemClicked event handler threw for '{Id}'", definition.Id);
            }
        }
    }

    private static int LOWORD(IntPtr ptr) => (int)(ptr.ToInt64() & 0xFFFF);
    private static int HIWORD(IntPtr ptr) => (int)((ptr.ToInt64() >> 16) & 0xFFFF);

    #region Win32 P/Invoke

    private const int GWLP_WNDPROC = -4;
    private const uint WM_COMMAND = 0x0111;

    private const uint MF_STRING = 0x0000;
    private const uint MF_SEPARATOR = 0x0800;
    private const uint MF_POPUP = 0x0010;
    private const uint MF_GRAYED = 0x0001;
    private const uint MF_ENABLED = 0x0000;
    private const uint MF_CHECKED = 0x0008;
    private const uint MF_UNCHECKED = 0x0000;
    private const uint MF_BYCOMMAND = 0x0000;

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    [SupportedOSPlatform("windows")]
    private static extern IntPtr CreateMenu();

    [DllImport("user32.dll")]
    [SupportedOSPlatform("windows")]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [SupportedOSPlatform("windows")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, uint uIDNewItem, string? lpNewItem);

    [DllImport("user32.dll")]
    [SupportedOSPlatform("windows")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetMenu(IntPtr hWnd, IntPtr hMenu);

    [DllImport("user32.dll")]
    [SupportedOSPlatform("windows")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DrawMenuBar(IntPtr hWnd);

    [DllImport("user32.dll")]
    [SupportedOSPlatform("windows")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll", EntryPoint = "EnableMenuItem")]
    [SupportedOSPlatform("windows")]
    private static extern int EnableMenuItemNative(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

    [DllImport("user32.dll", EntryPoint = "CheckMenuItem")]
    [SupportedOSPlatform("windows")]
    private static extern uint CheckMenuItemNative(IntPtr hMenu, uint uIDCheckItem, uint uCheck);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    [SupportedOSPlatform("windows")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    [SupportedOSPlatform("windows")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    #endregion
}
