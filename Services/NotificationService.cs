using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using CheapAvaloniaBlazor.Configuration;
using CheapAvaloniaBlazor.Models;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using AvaloniaNotificationType = Avalonia.Controls.Notifications.NotificationType;
using AvaloniaNotificationPosition = Avalonia.Controls.Notifications.NotificationPosition;
using NotificationPosition = CheapAvaloniaBlazor.Models.NotificationPosition;
using NotificationType = CheapAvaloniaBlazor.Models.NotificationType;

namespace CheapAvaloniaBlazor.Services;

/// <summary>
/// Dual-channel notification service: Avalonia desktop toasts + JS Web Notification API
/// Follows the same singleton + Dispatcher.UIThread.Post() pattern as SystemTrayService
/// </summary>
public class NotificationService : INotificationService
{
    private readonly CheapAvaloniaBlazorOptions _options;
    private readonly ILogger<NotificationService>? _logger;

    private Window? _overlayWindow;
    private WindowNotificationManager? _notificationManager;
    private bool _overlayInitialized;

    public bool SystemNotificationsEnabled => _options.EnableSystemNotifications;

    public NotificationService(
        CheapAvaloniaBlazorOptions options,
        ILogger<NotificationService>? logger = null)
    {
        _options = options;
        _logger = logger;
    }

    public void ShowDesktopNotification(string title, string? message = null,
        NotificationType notificationType = NotificationType.Information,
        TimeSpan? expiration = null)
    {
        var actualExpiration = expiration ?? TimeSpan.FromSeconds(Constants.Notifications.DefaultExpirationSeconds);
        var avaloniaType = MapNotificationType(notificationType);

        Dispatcher.UIThread.Post(() =>
        {
            EnsureOverlayCreated();

            if (_notificationManager is null)
            {
                _logger?.LogWarning("NotificationManager not available, cannot show desktop notification");
                return;
            }

            var notification = new Notification(title, message ?? string.Empty, avaloniaType, actualExpiration);
            _notificationManager.Show(notification);
            _logger?.LogDebug("Desktop notification shown: {Title} ({Type})", title, notificationType);
        });
    }

    public async Task ShowSystemNotificationAsync(IJSRuntime jsRuntime, string title, string message)
    {
        if (!_options.EnableSystemNotifications)
        {
            _logger?.LogDebug("System notifications disabled, skipping: {Title}", title);
            return;
        }

        await jsRuntime.InvokeVoidAsync(Constants.JavaScript.ShowNotificationMethod, title, message);
        _logger?.LogDebug("System notification sent via JS: {Title}", title);
    }

    private void EnsureOverlayCreated()
    {
        if (_overlayInitialized) return;
        _overlayInitialized = true;

        _logger?.LogDebug("Creating notification overlay window");

        _overlayWindow = new Window
        {
            SystemDecorations = SystemDecorations.None,
            TransparencyLevelHint = [WindowTransparencyLevel.Transparent],
            Background = null,
            ShowInTaskbar = false,
            Topmost = true,
            CanResize = false,
            Width = Constants.Notifications.OverlayWidth,
            Height = Constants.Notifications.OverlayHeight,
            ShowActivated = false
        };

        // Position at the correct corner of the primary screen
        PositionOverlayWindow();

        _notificationManager = new WindowNotificationManager(_overlayWindow)
        {
            Position = MapNotificationPosition(_options.DesktopNotificationPosition),
            MaxItems = _options.MaxDesktopNotifications
        };

        _overlayWindow.Show();

        _logger?.LogDebug("Notification overlay window created at position {Position}, max items {MaxItems}",
            _options.DesktopNotificationPosition, _options.MaxDesktopNotifications);
    }

    private void PositionOverlayWindow()
    {
        if (_overlayWindow is null) return;

        var screens = _overlayWindow.Screens;
        var primaryScreen = screens.Primary ?? screens.All.FirstOrDefault();
        if (primaryScreen is null) return;

        var workArea = primaryScreen.WorkingArea;
        var scaling = primaryScreen.Scaling;
        var overlayWidth = (int)(Constants.Notifications.OverlayWidth * scaling);
        var overlayHeight = (int)(Constants.Notifications.OverlayHeight * scaling);

        var (left, top) = _options.DesktopNotificationPosition switch
        {
            NotificationPosition.TopLeft => (workArea.X, workArea.Y),
            NotificationPosition.TopRight => (workArea.X + workArea.Width - overlayWidth, workArea.Y),
            NotificationPosition.BottomLeft => (workArea.X, workArea.Y + workArea.Height - overlayHeight),
            NotificationPosition.BottomRight => (workArea.X + workArea.Width - overlayWidth, workArea.Y + workArea.Height - overlayHeight),
            NotificationPosition.TopCenter => (workArea.X + (workArea.Width - overlayWidth) / 2, workArea.Y),
            NotificationPosition.BottomCenter => (workArea.X + (workArea.Width - overlayWidth) / 2, workArea.Y + workArea.Height - overlayHeight),
            _ => (workArea.X + workArea.Width - overlayWidth, workArea.Y + workArea.Height - overlayHeight)
        };

        _overlayWindow.Position = new PixelPoint(left, top);
    }

    private static AvaloniaNotificationType MapNotificationType(NotificationType notificationType) =>
        notificationType switch
        {
            NotificationType.Information => AvaloniaNotificationType.Information,
            NotificationType.Success => AvaloniaNotificationType.Success,
            NotificationType.Warning => AvaloniaNotificationType.Warning,
            NotificationType.Error => AvaloniaNotificationType.Error,
            _ => AvaloniaNotificationType.Information
        };

    private static AvaloniaNotificationPosition MapNotificationPosition(NotificationPosition position) =>
        position switch
        {
            NotificationPosition.TopLeft => AvaloniaNotificationPosition.TopLeft,
            NotificationPosition.TopRight => AvaloniaNotificationPosition.TopRight,
            NotificationPosition.BottomLeft => AvaloniaNotificationPosition.BottomLeft,
            NotificationPosition.BottomRight => AvaloniaNotificationPosition.BottomRight,
            NotificationPosition.TopCenter => AvaloniaNotificationPosition.TopCenter,
            NotificationPosition.BottomCenter => AvaloniaNotificationPosition.BottomCenter,
            _ => AvaloniaNotificationPosition.BottomRight
        };
}
