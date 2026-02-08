using CheapAvaloniaBlazor.Models;

namespace CheapAvaloniaBlazor.Services;

/// <summary>
/// Service for detecting the OS dark/light mode preference and tracking runtime changes.
/// Uses Avalonia's platform theme detection under the hood.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// The current OS theme preference
    /// </summary>
    SystemTheme CurrentTheme { get; }

    /// <summary>
    /// Convenience property: true when the OS is in dark mode
    /// </summary>
    bool IsDarkMode { get; }

    /// <summary>
    /// Fired when the OS theme preference changes at runtime
    /// </summary>
    event Action<SystemTheme>? ThemeChanged;
}
