using Avalonia;
using Avalonia.Controls;
using CheapAvaloniaBlazor.Configuration;

namespace CheapAvaloniaBlazor.Extensions;

/// <summary>
/// Extension methods for Window configuration
/// </summary>
public static class WindowConfigurationExtensions
{
    /// <summary>
    /// Apply CheapAvaloniaBlazorOptions to a window
    /// </summary>
    public static void ApplyOptions(this Window window, CheapAvaloniaBlazorOptions options)
    {
        window.Title = options.DefaultWindowTitle;
        window.Width = options.DefaultWindowWidth;
        window.Height = options.DefaultWindowHeight;
        window.CanResize = options.Resizable;

        if (options.CenterWindow)
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
        else if (options.WindowLeft.HasValue && options.WindowTop.HasValue)
        {
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Position = new PixelPoint(options.WindowLeft.Value, options.WindowTop.Value);
        }
    }
}
