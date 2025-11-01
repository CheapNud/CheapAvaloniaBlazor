using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace CheapAvaloniaBlazor.Configuration;

/// <summary>
/// Configuration for the splash screen shown during application startup
/// </summary>
public class SplashScreenConfig
{
    /// <summary>
    /// Whether to show the splash screen during startup
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Title text displayed on the splash screen
    /// </summary>
    public string Title { get; set; } = Constants.Defaults.DefaultWindowTitle;

    /// <summary>
    /// Loading message displayed on the splash screen
    /// </summary>
    public string LoadingMessage { get; set; } = Constants.Defaults.SplashLoadingMessage;

    /// <summary>
    /// Splash screen width in pixels
    /// </summary>
    public int Width { get; set; } = Constants.Defaults.SplashWindowWidth;

    /// <summary>
    /// Splash screen height in pixels
    /// </summary>
    public int Height { get; set; } = Constants.Defaults.SplashWindowHeight;

    /// <summary>
    /// Background color for the splash screen
    /// </summary>
    public string BackgroundColor { get; set; } = Constants.Defaults.SplashBackgroundColor;

    /// <summary>
    /// Foreground (text) color for the splash screen
    /// </summary>
    public string ForegroundColor { get; set; } = Constants.Defaults.SplashForegroundColor;

    /// <summary>
    /// Font size for the title text
    /// </summary>
    public double TitleFontSize { get; set; } = Constants.Defaults.SplashTitleFontSize;

    /// <summary>
    /// Font size for the loading message
    /// </summary>
    public double MessageFontSize { get; set; } = Constants.Defaults.SplashMessageFontSize;

    /// <summary>
    /// Whether to show an animated loading indicator
    /// </summary>
    public bool ShowLoadingIndicator { get; set; } = true;

    /// <summary>
    /// Custom content to display on the splash screen
    /// If provided, this overrides the default splash UI
    /// </summary>
    public Func<Control>? CustomContentFactory { get; set; }

    /// <summary>
    /// Create default splash screen configuration
    /// </summary>
    public static SplashScreenConfig CreateDefault()
    {
        return new SplashScreenConfig
        {
            Enabled = true,
            Title = Constants.Defaults.DefaultWindowTitle,
            LoadingMessage = Constants.Defaults.SplashLoadingMessage,
            Width = Constants.Defaults.SplashWindowWidth,
            Height = Constants.Defaults.SplashWindowHeight,
            BackgroundColor = Constants.Defaults.SplashBackgroundColor,
            ForegroundColor = Constants.Defaults.SplashForegroundColor,
            TitleFontSize = Constants.Defaults.SplashTitleFontSize,
            MessageFontSize = Constants.Defaults.SplashMessageFontSize,
            ShowLoadingIndicator = true
        };
    }

    /// <summary>
    /// Create the default splash screen content
    /// </summary>
    internal Control CreateDefaultContent()
    {
        var backgroundColor = Brush.Parse(BackgroundColor);
        var foregroundBrush = Brush.Parse(ForegroundColor);

        var mainPanel = new StackPanel
        {
            Background = backgroundColor,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 20
        };

        // Title
        mainPanel.Children.Add(new TextBlock
        {
            Text = Title,
            FontSize = TitleFontSize,
            FontWeight = FontWeight.Bold,
            Foreground = foregroundBrush,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        // Loading message
        mainPanel.Children.Add(new TextBlock
        {
            Text = LoadingMessage,
            FontSize = MessageFontSize,
            Foreground = foregroundBrush,
            HorizontalAlignment = HorizontalAlignment.Center,
            Opacity = 0.8
        });

        // Loading indicator (simple animated dots)
        if (ShowLoadingIndicator)
        {
            mainPanel.Children.Add(new TextBlock
            {
                Text = "•  •  •",
                FontSize = MessageFontSize,
                Foreground = foregroundBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                Opacity = 0.6
            });
        }

        return new Border
        {
            Background = backgroundColor,
            Child = mainPanel
        };
    }
}
