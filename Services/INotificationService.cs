using CheapAvaloniaBlazor.Models;

namespace CheapAvaloniaBlazor.Services;

/// <summary>
/// Dual-channel notification service providing both Avalonia desktop toasts
/// and OS-level system notifications via JavaScript Web Notification API
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Show an Avalonia-rendered desktop toast notification (cross-platform, always works)
    /// </summary>
    /// <param name="title">Notification title</param>
    /// <param name="message">Optional notification message body</param>
    /// <param name="notificationType">Notification severity level</param>
    /// <param name="expiration">How long to show the toast (null for default)</param>
    void ShowDesktopNotification(string title, string? message = null,
        NotificationType notificationType = NotificationType.Information, TimeSpan? expiration = null);

    /// <summary>
    /// Show a system notification via JavaScript Web Notification API (OS notification center).
    /// Requires EnableSystemNotifications to be true in options. May request browser permission on first use.
    /// </summary>
    /// <param name="title">Notification title</param>
    /// <param name="message">Notification message body</param>
    Task ShowSystemNotificationAsync(string title, string message);

    /// <summary>
    /// Whether system (JS Web Notification API) notifications are enabled in options
    /// </summary>
    bool SystemNotificationsEnabled { get; }
}
