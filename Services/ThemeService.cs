using Avalonia;
using Avalonia.Styling;
using Avalonia.Threading;
using CheapAvaloniaBlazor.Models;
using Microsoft.Extensions.Logging;

namespace CheapAvaloniaBlazor.Services;

/// <summary>
/// Singleton service that detects OS dark/light mode via Avalonia's platform theme detection.
/// Subscribes to Avalonia's ActualThemeVariantChanged to track runtime switches.
/// </summary>
public class ThemeService : IThemeService
{
    private readonly ILogger<ThemeService>? _logger;
    private volatile bool _isDarkMode;

    public SystemTheme CurrentTheme => _isDarkMode ? SystemTheme.Dark : SystemTheme.Light;
    public bool IsDarkMode => _isDarkMode;

    public event Action<SystemTheme>? ThemeChanged;

    public ThemeService(ILogger<ThemeService>? logger = null)
    {
        _logger = logger;

        // Read initial theme and subscribe to changes on the Avalonia UI thread
        Dispatcher.UIThread.Post(() =>
        {
            if (Application.Current is not { } app)
            {
                _logger?.LogWarning("ThemeService: Application.Current is null - defaulting to Light theme");
                return;
            }

            _isDarkMode = app.ActualThemeVariant == ThemeVariant.Dark;
            _logger?.LogDebug("ThemeService: Initial OS theme is {Theme}", CurrentTheme);

            app.ActualThemeVariantChanged += OnAvaloniaThemeChanged;
            _logger?.LogDebug("ThemeService: Subscribed to OS theme changes");
        });
    }

    private void OnAvaloniaThemeChanged(object? sender, EventArgs e)
    {
        if (Application.Current is not { } app)
            return;

        var wasDark = _isDarkMode;
        _isDarkMode = app.ActualThemeVariant == ThemeVariant.Dark;

        if (wasDark != _isDarkMode)
        {
            var newTheme = CurrentTheme;
            _logger?.LogDebug("ThemeService: OS theme changed to {Theme}", newTheme);
            ThemeChanged?.Invoke(newTheme);
        }
    }
}
