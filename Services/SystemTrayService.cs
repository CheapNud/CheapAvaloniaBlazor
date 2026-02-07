using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CheapAvaloniaBlazor.Configuration;
using CheapAvaloniaBlazor.Models;
using Microsoft.Extensions.Logging;

namespace CheapAvaloniaBlazor.Services;

/// <summary>
/// System tray service implementation using Avalonia's TrayIcon API
/// </summary>
public class SystemTrayService : ISystemTrayService
{
    private readonly CheapAvaloniaBlazorOptions _options;
    private readonly PhotinoMessageHandler _messageHandler;
    private readonly ILogger<SystemTrayService>? _logger;

    private TrayIcon? _trayIcon;
    private NativeMenu? _contextMenu;
    private readonly List<TrayMenuItemDefinition> _customMenuItems = [];
    private bool _isDisposed;

    public bool IsVisible => _trayIcon?.IsVisible ?? false;

    public event Action? TrayIconClicked;
#pragma warning disable CS0067 // Event is never used - intentionally part of interface for future expansion
    public event Action? TrayIconDoubleClicked;
#pragma warning restore CS0067

    public SystemTrayService(
        CheapAvaloniaBlazorOptions options,
        PhotinoMessageHandler messageHandler,
        ILogger<SystemTrayService>? logger = null)
    {
        _options = options;
        _messageHandler = messageHandler;
        _logger = logger;
    }

    public void ShowTrayIcon()
    {
        Dispatcher.UIThread.Post(() =>
        {
            EnsureTrayIconCreated();
            if (_trayIcon != null)
            {
                _trayIcon.IsVisible = true;
                _logger?.LogDebug("System tray icon shown");
            }
        });
    }

    public void HideTrayIcon()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_trayIcon != null)
            {
                _trayIcon.IsVisible = false;
                _logger?.LogDebug("System tray icon hidden");
            }
        });
    }

    public void SetTrayIcon(string iconPath)
    {
        if (string.IsNullOrEmpty(iconPath) || !System.IO.File.Exists(iconPath))
        {
            _logger?.LogWarning("Tray icon path is invalid or file does not exist: {IconPath}", iconPath);
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            EnsureTrayIconCreated();
            try
            {
                _trayIcon!.Icon = new WindowIcon(iconPath);
                _logger?.LogDebug("System tray icon set from path: {IconPath}", iconPath);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to set tray icon from path: {IconPath}", iconPath);
            }
        });
    }

    public void SetTrayIcon(WindowIcon icon)
    {
        Dispatcher.UIThread.Post(() =>
        {
            EnsureTrayIconCreated();
            _trayIcon!.Icon = icon;
            _logger?.LogDebug("System tray icon set from WindowIcon");
        });
    }

    public void SetTrayTooltip(string tooltip)
    {
        Dispatcher.UIThread.Post(() =>
        {
            EnsureTrayIconCreated();
            _trayIcon!.ToolTipText = tooltip;
            _logger?.LogDebug("System tray tooltip set: {Tooltip}", tooltip);
        });
    }

    public void SetTrayMenu(IEnumerable<TrayMenuItemDefinition> menuItems)
    {
        _customMenuItems.Clear();
        _customMenuItems.AddRange(menuItems);

        Dispatcher.UIThread.Post(RebuildContextMenu);
    }

    public void AddTrayMenuItem(TrayMenuItemDefinition menuItem)
    {
        _customMenuItems.Add(menuItem);
        Dispatcher.UIThread.Post(RebuildContextMenu);
    }

    public void ClearTrayMenu()
    {
        _customMenuItems.Clear();
        Dispatcher.UIThread.Post(RebuildContextMenu);
    }

    public void MinimizeToTray()
    {
        _logger?.LogDebug("Minimizing to tray");

        // Hide the Photino window completely (not just minimize to taskbar)
        if (!_messageHandler.HideWindow())
        {
            // Fallback to minimize if hide not supported on this platform
            _logger?.LogWarning("Window hiding not supported, falling back to minimize");
            _messageHandler.MinimizeWindow();
        }

        // Show tray icon if not already visible
        ShowTrayIcon();
    }

    public void RestoreFromTray()
    {
        _logger?.LogDebug("Restoring from tray");

        // Show the hidden Photino window and bring to foreground
        if (!_messageHandler.ShowWindowFromHidden())
        {
            // Fallback to restore if show not supported
            _logger?.LogWarning("Window show not supported, falling back to restore");
            _messageHandler.RestoreWindow();
        }
    }

    private void EnsureTrayIconCreated()
    {
        if (_trayIcon != null) return;

        _logger?.LogDebug("Creating system tray icon");

        _trayIcon = new TrayIcon();

        // Set initial tooltip
        var tooltip = _options.TrayTooltip
            ?? _options.DefaultWindowTitle
            ?? Constants.SystemTray.DefaultTooltip;
        _trayIcon.ToolTipText = tooltip;

        // Set initial icon
        var iconPath = _options.TrayIconPath ?? _options.IconPath;
        var iconSet = false;

        if (!string.IsNullOrEmpty(iconPath) && System.IO.File.Exists(iconPath))
        {
            try
            {
                _trayIcon.Icon = new WindowIcon(iconPath);
                iconSet = true;
                _logger?.LogDebug("Tray icon loaded from: {IconPath}", iconPath);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to load tray icon from: {IconPath}", iconPath);
            }
        }

        // Create fallback icon if no icon was set
        if (!iconSet)
        {
            try
            {
                var fallbackIcon = CreateFallbackIcon();
                if (fallbackIcon != null)
                {
                    _trayIcon.Icon = fallbackIcon;
                    _logger?.LogDebug("Using fallback tray icon");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to create fallback tray icon");
            }
        }

        // Wire up click events
        _trayIcon.Clicked += OnTrayIconClicked;

        // Build context menu
        RebuildContextMenu();

        // Register the tray icon with Avalonia
        if (Application.Current is { } app)
        {
            var trayIcons = TrayIcon.GetIcons(app);
            if (trayIcons == null)
            {
                trayIcons = new TrayIcons();
                TrayIcon.SetIcons(app, trayIcons);
            }
            trayIcons.Add(_trayIcon);
        }

        _logger?.LogDebug("System tray icon created successfully");
    }

    private static WindowIcon? CreateFallbackIcon()
    {
        // Create a simple 16x16 colored square as fallback icon
        const int size = 16;

        var bitmap = new WriteableBitmap(
            new Avalonia.PixelSize(size, size),
            new Avalonia.Vector(96, 96),
            Avalonia.Platform.PixelFormat.Bgra8888,
            Avalonia.Platform.AlphaFormat.Premul);

        using (var frameBuffer = bitmap.Lock())
        {
            // Create pixel data array (BGRA format)
            // Blue color: B=0xFF, G=0x66, R=0x33, A=0xFF => #3366FF
            var pixelData = new byte[size * size * 4];
            for (int i = 0; i < size * size; i++)
            {
                int offset = i * 4;
                pixelData[offset + 0] = 0xFF; // Blue
                pixelData[offset + 1] = 0x66; // Green
                pixelData[offset + 2] = 0x33; // Red
                pixelData[offset + 3] = 0xFF; // Alpha
            }

            // Copy to the bitmap buffer
            System.Runtime.InteropServices.Marshal.Copy(pixelData, 0, frameBuffer.Address, pixelData.Length);
        }

        // Convert bitmap to WindowIcon via stream
        using var stream = new MemoryStream();
        bitmap.Save(stream);
        stream.Position = 0;

        return new WindowIcon(stream);
    }

    private void RebuildContextMenu()
    {
        _contextMenu = new NativeMenu();

        // Add custom menu items first
        foreach (var item in _customMenuItems)
        {
            AddMenuItemToNativeMenu(_contextMenu, item);
        }

        // Add separator before default items if we have custom items
        if (_customMenuItems.Count > 0 && _options.ShowDefaultTrayMenuItems)
        {
            _contextMenu.Add(new NativeMenuItemSeparator());
        }

        // Add default menu items if enabled
        if (_options.ShowDefaultTrayMenuItems)
        {
            // Show item
            var showItem = new NativeMenuItem(Constants.SystemTray.ShowMenuText);
            showItem.Click += (_, _) => RestoreFromTray();
            _contextMenu.Add(showItem);

            // Exit item
            var exitItem = new NativeMenuItem(Constants.SystemTray.ExitMenuText);
            exitItem.Click += (_, _) => ExitApplication();
            _contextMenu.Add(exitItem);
        }

        if (_trayIcon != null)
        {
            _trayIcon.Menu = _contextMenu;
        }

        _logger?.LogDebug("Context menu rebuilt with {CustomCount} custom items", _customMenuItems.Count);
    }

    private void AddMenuItemToNativeMenu(NativeMenu menu, TrayMenuItemDefinition definition)
    {
        if (definition.IsSeparator)
        {
            menu.Add(new NativeMenuItemSeparator());
            return;
        }

        // Check for submenu
        if (definition.SubMenuItems is { Count: > 0 })
        {
            var subMenu = new NativeMenu();
            foreach (var subItem in definition.SubMenuItems)
            {
                AddMenuItemToNativeMenu(subMenu, subItem);
            }

            var parentItem = new NativeMenuItem(definition.Text)
            {
                Menu = subMenu,
                IsEnabled = definition.IsEnabled
            };
            menu.Add(parentItem);
            return;
        }

        // Regular menu item
        var menuItem = new NativeMenuItem(definition.Text)
        {
            IsEnabled = definition.IsEnabled
        };

        // Handle toggle state for checkable items
        if (definition.IsCheckable)
        {
            menuItem.ToggleType = NativeMenuItemToggleType.CheckBox;
            menuItem.IsChecked = definition.IsChecked;
        }

        // Wire up click handler
        menuItem.Click += (_, _) =>
        {
            // Toggle check state for checkable items
            if (definition.IsCheckable)
            {
                definition.IsChecked = !definition.IsChecked;
            }

            // Execute click handlers
            if (definition.OnClickAsync != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await definition.OnClickAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error executing async menu item click handler");
                    }
                });
            }
            else
            {
                definition.OnClick?.Invoke();
            }
        };

        menu.Add(menuItem);
    }

    private void OnTrayIconClicked(object? sender, EventArgs e)
    {
        _logger?.LogDebug("Tray icon clicked");
        TrayIconClicked?.Invoke();

        // Default behavior: restore on click
        RestoreFromTray();
    }

    private void ExitApplication()
    {
        _logger?.LogDebug("Exit requested from tray menu");

        Dispatcher.UIThread.Post(() =>
        {
            // Clean up tray icon
            HideTrayIcon();

            // Shutdown the application
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            {
                lifetime.Shutdown();
            }
        });
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        Dispatcher.UIThread.Post(() =>
        {
            if (_trayIcon != null)
            {
                _trayIcon.Clicked -= OnTrayIconClicked;
                _trayIcon.IsVisible = false;

                // Remove from application tray icons
                if (Application.Current is { } app)
                {
                    var trayIcons = TrayIcon.GetIcons(app);
                    trayIcons?.Remove(_trayIcon);
                }

                _trayIcon.Dispose();
                _trayIcon = null;
            }
        });

        _isDisposed = true;
        _logger?.LogDebug("SystemTrayService disposed");
    }
}
