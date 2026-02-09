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

    // CRITICAL: The WndProc delegate must not be collected by GC while the subclass is active.
    // Field reference alone can be accidentally removed during refactoring, so we also pin
    // with GCHandle to make the intent explicit and survive any future code cleanup.
    private WndProcDelegate? _wndProcDelegate;
    private GCHandle _wndProcDelegateHandle;

    private readonly Dictionary<int, MenuItemDefinition> _win32IdToDefinition = [];
    private readonly Dictionary<string, int> _stringIdToWin32Id = [];
    private readonly List<IntPtr> _popupMenuHandles = [];
    private int _nextMenuId = Constants.MenuBar.FirstMenuItemId;

    private int _disposed; // 0 = not disposed, 1 = disposed (Interlocked)
    private bool _handlingCommand;

    public bool IsSupported => OperatingSystem.IsWindows();

    public event Action<string>? MenuItemClicked;

    /// <summary>
    /// Fired when an async menu item callback throws. Allows the orchestrator to surface errors
    /// that would otherwise be silently swallowed in fire-and-forget from the WndProc thread.
    /// </summary>
    internal event Action<Exception>? AsyncExceptionOccurred;

    public WindowsMenuBarBackend(ILogger logger)
    {
        _logger = logger;
    }

    [SupportedOSPlatform("windows")]
    public void Initialize(IntPtr windowHandle, IEnumerable<MenuItemDefinition> menus)
    {
        if (Volatile.Read(ref _disposed) != 0) return;
        if (windowHandle == IntPtr.Zero) return;

        _windowHandle = windowHandle;

        // Subclass the window to intercept WM_COMMAND.
        // GCHandle.Alloc prevents GC even if the field reference is accidentally removed during refactoring.
        _wndProcDelegate = WndProc;
        _wndProcDelegateHandle = GCHandle.Alloc(_wndProcDelegate);

        try
        {
            _originalWndProc = SetWindowLongPtr(_windowHandle, GWLP_WNDPROC,
                Marshal.GetFunctionPointerForDelegate(_wndProcDelegate));

            _logger.LogDebug("WindowsMenuBarBackend: Subclassed WndProc on HWND {Handle}", windowHandle);

            BuildAndAttachMenuBar(menus);
        }
        catch
        {
            // ORDER MATTERS: Restore WndProc FIRST while the delegate is still alive,
            // then free the GCHandle. Reversing this order leaves a freed delegate as
            // the active WndProc — Windows dispatching a message would access-violate.
            if (_originalWndProc != IntPtr.Zero && IsWindow(_windowHandle))
            {
                SetWindowLongPtr(_windowHandle, GWLP_WNDPROC, _originalWndProc);
                _originalWndProc = IntPtr.Zero;
            }

            if (_wndProcDelegateHandle.IsAllocated)
                _wndProcDelegateHandle.Free();
            _wndProcDelegate = null;

            throw;
        }
    }

    [SupportedOSPlatform("windows")]
    public void SetMenuBar(IEnumerable<MenuItemDefinition> menus)
    {
        if (Volatile.Read(ref _disposed) != 0) return;
        if (_windowHandle == IntPtr.Zero) return;

        // Destroy old menu bar (DestroyMenu recursively destroys attached submenus)
        if (_menuBarHandle != IntPtr.Zero)
        {
            SetMenu(_windowHandle, IntPtr.Zero);
            DestroyMenu(_menuBarHandle);
            _menuBarHandle = IntPtr.Zero;
        }

        // Destroy any popup handles from error paths that weren't attached to the bar.
        // DestroyMenu on already-freed handles (destroyed by parent above) returns FALSE harmlessly.
        foreach (var popupHandle in _popupMenuHandles)
        {
            DestroyMenu(popupHandle);
        }
        _popupMenuHandles.Clear();
        _win32IdToDefinition.Clear();
        _stringIdToWin32Id.Clear();
        _nextMenuId = Constants.MenuBar.FirstMenuItemId;

        BuildAndAttachMenuBar(menus);
    }

    [SupportedOSPlatform("windows")]
    public void EnableMenuItem(string menuItemId, bool enabled)
    {
        if (Volatile.Read(ref _disposed) != 0) return;
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
        if (Volatile.Read(ref _disposed) != 0) return;
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
        // Atomic check-and-set: only one thread enters disposal
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0) return;

        // ORDER MATTERS: Restore WndProc FIRST, then destroy menu.
        // Pending WM_COMMAND messages could reference freed memory otherwise.
        // IsWindow() guards against the HWND being destroyed externally before Dispose runs.
        var windowStillExists = _windowHandle != IntPtr.Zero && IsWindow(_windowHandle);

        if (_originalWndProc != IntPtr.Zero && windowStillExists)
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
                if (windowStillExists)
                    SetMenu(_windowHandle, IntPtr.Zero);

                // DestroyMenu recursively destroys attached submenus
                DestroyMenu(_menuBarHandle);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "WindowsMenuBarBackend: Failed to destroy menu");
            }
            _menuBarHandle = IntPtr.Zero;
        }

        // Destroy any popup handles that weren't attached to the menu bar (error paths).
        // DestroyMenu on already-freed handles returns FALSE harmlessly.
        foreach (var popupHandle in _popupMenuHandles)
        {
            DestroyMenu(popupHandle);
        }

        _popupMenuHandles.Clear();
        _win32IdToDefinition.Clear();
        _stringIdToWin32Id.Clear();

        if (_wndProcDelegateHandle.IsAllocated)
            _wndProcDelegateHandle.Free();
        _wndProcDelegate = null;

        _logger.LogDebug("WindowsMenuBarBackend: Disposed");
    }

    [SupportedOSPlatform("windows")]
    private void BuildAndAttachMenuBar(IEnumerable<MenuItemDefinition> menus)
    {
        _menuBarHandle = CreateMenu();
        if (_menuBarHandle == IntPtr.Zero)
        {
            _logger.LogError("WindowsMenuBarBackend: CreateMenu() returned null — menu bar will not be created");
            return;
        }

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
                if (menuId < 0) continue; // ID space exhausted, skip item

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
        if (popupHandle == IntPtr.Zero)
        {
            _logger.LogError("WindowsMenuBarBackend: CreatePopupMenu() returned null — submenu will be empty");
            return IntPtr.Zero;
        }
        _popupMenuHandles.Add(popupHandle);

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
            if (menuId < 0) continue; // ID space exhausted, skip item

            var displayText = BuildDisplayText(menuItem);

            uint flags = MF_STRING;
            if (!menuItem.IsEnabled) flags |= MF_GRAYED;
            if (menuItem.IsCheckable && menuItem.IsChecked) flags |= MF_CHECKED;

            AppendMenu(popupHandle, flags, (uint)menuId, displayText);
        }

        return popupHandle;
    }

    /// <summary>
    /// Assigns a Win32 command ID to a menu item definition.
    /// Returns -1 if the ID space is exhausted (item should be skipped by caller).
    /// </summary>
    private int AssignMenuId(MenuItemDefinition definition)
    {
        if (_nextMenuId > Constants.MenuBar.MaxMenuItemId)
        {
            _logger.LogError(
                "Menu item ID overflow: exceeded maximum of {Max} (0x{MaxHex:X4}). " +
                "Menu item '{Text}' will not be interactive. Reduce the number of menu items.",
                Constants.MenuBar.MaxMenuItemId, Constants.MenuBar.MaxMenuItemId, definition.Text);
            return -1;
        }

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
        // Cache locally: Dispose() on another thread could zero _originalWndProc mid-execution.
        var cachedOriginalWndProc = _originalWndProc;

        if (msg == WM_COMMAND && !_handlingCommand && Volatile.Read(ref _disposed) == 0)
        {
            var commandId = LOWORD(wParam);
            var notificationCode = HIWORD(wParam);

            // notificationCode == 0 means menu click (not accelerator or control notification).
            // _handlingCommand guards against reentrancy: if a callback programmatically
            // triggers another WM_COMMAND, we forward to the original WndProc instead of re-entering.
            if (notificationCode == 0 && _win32IdToDefinition.TryGetValue(commandId, out var definition))
            {
                _handlingCommand = true;
                try
                {
                    HandleMenuItemClick(definition);
                }
                finally
                {
                    _handlingCommand = false;
                }
                return IntPtr.Zero;
            }
        }

        // Use cached pointer — safe even if Dispose() zeroed _originalWndProc concurrently.
        // If cached pointer is Zero (Dispose already ran), use DefWindowProc as final fallback.
        if (cachedOriginalWndProc == IntPtr.Zero)
            return DefWindowProc(hWnd, msg, wParam, lParam);

        return CallWindowProc(cachedOriginalWndProc, hWnd, msg, wParam, lParam);
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

        // Invoke async callback (fire-and-forget since WndProc is synchronous).
        // Exceptions are logged and surfaced via AsyncExceptionOccurred for the orchestrator.
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
                    AsyncExceptionOccurred?.Invoke(ex);
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

    [DllImport("user32.dll")]
    [SupportedOSPlatform("windows")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [SupportedOSPlatform("windows")]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    #endregion
}
