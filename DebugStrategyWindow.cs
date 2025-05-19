using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AvaloniaWebView;
using Photino.NET;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CheapAvaloniaBlazor;

public partial class DebugStrategyWindow : Window
{
    private PhotinoWindow? _photinoWindow;
    private bool _isPhotinoWindowOpen = false;
    private System.Threading.Timer? _timeoutTimer;
    private const int TIMEOUT_SECONDS = 6;

    public DebugStrategyWindow()
    {
        InitializeComponent();
        Title = "AppName - Debug Strategy Selection";
        Width = 800;
        Height = 600;

        // Create debug UI
        CreateDebugUI();

        // Start timeout timer
        StartTimeoutTimer();

        // Handle window closing
        Closing += OnWindowClosing;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void CreateDebugUI()
    {
        var mainPanel = new StackPanel
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Spacing = 20,
            Margin = new Avalonia.Thickness(40),
            Background = Avalonia.Media.Brushes.White // Ensure white background
        };

        // Title
        mainPanel.Children.Add(new TextBlock
        {
            Text = "🐛 Debug Mode - Strategy Selection",
            FontSize = 28,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Foreground = Avalonia.Media.Brushes.DarkBlue
        });

        // Subtitle with timeout info
        var timeoutLabel = new TextBlock
        {
            Text = $"Select a WebView strategy to test (auto-continues in {TIMEOUT_SECONDS} seconds)",
            FontSize = 14,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Foreground = Avalonia.Media.Brushes.DarkGray,
            Margin = new Avalonia.Thickness(0, 0, 0, 20)
        };
        mainPanel.Children.Add(timeoutLabel);

        // Platform info
        var platformPanel = new Border
        {
            Background = Avalonia.Media.Brushes.LightYellow,
            BorderBrush = Avalonia.Media.Brushes.DarkGoldenrod,
            BorderThickness = new Avalonia.Thickness(1),
            CornerRadius = new Avalonia.CornerRadius(8),
            Padding = new Avalonia.Thickness(15),
            Margin = new Avalonia.Thickness(0, 0, 0, 20)
        };

        var platformInfo = new StackPanel { Spacing = 5 };
        platformInfo.Children.Add(new TextBlock
        {
            Text = $"OS: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}",
            FontSize = 12,
            Foreground = Avalonia.Media.Brushes.Black
        });
        platformInfo.Children.Add(new TextBlock
        {
            Text = $"Recommended: {PlatformHelper.GetRecommendedWebViewStrategy()}",
            FontSize = 12,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            Foreground = Avalonia.Media.Brushes.DarkBlue
        });
        platformInfo.Children.Add(new TextBlock
        {
            Text = $"Available: {string.Join(", ", PlatformHelper.GetAllWebViewStrategies())}",
            FontSize = 12,
            Foreground = Avalonia.Media.Brushes.Black
        });

        platformPanel.Child = platformInfo;
        mainPanel.Children.Add(platformPanel);

        // Strategy buttons (in order of preference)
        var buttonPanel = new StackPanel { Spacing = 15 };

        // Button 1: Photino.NET
        var photinoNetButton = CreateStrategyButton(
            "1️⃣ Photino.NET",
            "Manual native WebView",
            PlatformHelper.IsPhotinoNetSupported(),
            async () => await TryPhotinoNet()
        );
        buttonPanel.Children.Add(photinoNetButton);

        // Button 2: WebView.Avalonia
        var avaloniaWebViewButton = CreateStrategyButton(
            "2️⃣ WebView.Avalonia",
            "Community WebView component",
            PlatformHelper.IsAvaloniaWebViewSupported(),
            async () => await TryAvaloniaWebView()
        );
        buttonPanel.Children.Add(avaloniaWebViewButton);

        // Button 3: Embedded Browser
        var embeddedBrowserButton = CreateStrategyButton(
            "3️⃣ Embedded Browser",
            "Fallback UI with browser link",
            true, // Always available
            async () => await TryEmbeddedBrowser()
        );
        buttonPanel.Children.Add(embeddedBrowserButton);

        mainPanel.Children.Add(buttonPanel);

        // Auto-continue button
        var autoContinueButton = new Button
        {
            Content = "🚀 Auto-Continue (Default Strategy)",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Padding = new Avalonia.Thickness(30, 15),
            FontSize = 14,
            Background = Avalonia.Media.Brushes.Green,
            Foreground = Avalonia.Media.Brushes.White,
            Margin = new Avalonia.Thickness(0, 30, 0, 0)
        };

        autoContinueButton.Click += async (s, e) => await AutoContinue();
        mainPanel.Children.Add(autoContinueButton);

        // Set the main content with white background
        Content = new Border
        {
            Background = Avalonia.Media.Brushes.White,
            Child = new ScrollViewer
            {
                Content = mainPanel,
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                Background = Avalonia.Media.Brushes.White
            }
        };
    }

    private Border CreateStrategyButton(string title, string description, bool isSupported, Func<Task> action)
    {
        var container = new Border
        {
            Background = isSupported ? Avalonia.Media.Brushes.LightGreen : Avalonia.Media.Brushes.LightGray,
            BorderBrush = isSupported ? Avalonia.Media.Brushes.DarkGreen : Avalonia.Media.Brushes.Gray,
            BorderThickness = new Avalonia.Thickness(2),
            CornerRadius = new Avalonia.CornerRadius(8),
            Padding = new Avalonia.Thickness(15),
            Margin = new Avalonia.Thickness(5)
        };

        var panel = new StackPanel { Spacing = 8 };

        var titleBlock = new TextBlock
        {
            Text = title,
            FontSize = 16,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            Foreground = isSupported ? Avalonia.Media.Brushes.DarkGreen : Avalonia.Media.Brushes.DarkGray
        };

        var descBlock = new TextBlock
        {
            Text = description,
            FontSize = 12,
            Foreground = Avalonia.Media.Brushes.Black
        };

        var statusBlock = new TextBlock
        {
            Text = isSupported ? "✅ Supported" : "❌ Not supported on this platform",
            FontSize = 11,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            Foreground = isSupported ? Avalonia.Media.Brushes.DarkGreen : Avalonia.Media.Brushes.Red
        };

        var button = new Button
        {
            Content = $"Test {title.Split(' ')[1]}",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            IsEnabled = isSupported,
            Padding = new Avalonia.Thickness(20, 8),
            FontSize = 13,
            Background = isSupported ? Avalonia.Media.Brushes.DarkGreen : Avalonia.Media.Brushes.Gray,
            Foreground = Avalonia.Media.Brushes.White
        };

        button.Click += async (s, e) =>
        {
            try
            {
                if (_timeoutTimer != null)
                {
                    _timeoutTimer.Dispose(); // Cancel timeout when user makes a choice
                    _timeoutTimer = null;
                }
                await action();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in button click: {ex}");
            }
        };

        panel.Children.Add(titleBlock);
        panel.Children.Add(descBlock);
        panel.Children.Add(statusBlock);
        panel.Children.Add(button);

        container.Child = panel;
        return container;
    }

    private void StartTimeoutTimer()
    {
        _timeoutTimer = new System.Threading.Timer(TimeoutCallback, null, TimeSpan.FromSeconds(TIMEOUT_SECONDS), Timeout.InfiniteTimeSpan);
    }

    private void TimeoutCallback(object? state)
    {
        try
        {
            // Use Invoke instead of InvokeAsync to avoid potential recursion
            Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
            {
                try
                {
                    _timeoutTimer?.Dispose();
                    _timeoutTimer = null;
                    System.Diagnostics.Debug.WriteLine("Debug timeout reached, auto-continuing with default strategy");
                    await AutoContinue();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in timeout callback: {ex}");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error posting timeout callback: {ex}");
        }
    }

    private async Task AutoContinue()
    {
        try
        {
            if (_timeoutTimer != null)
            {
                _timeoutTimer.Dispose();
                _timeoutTimer = null;
            }

            System.Diagnostics.Debug.WriteLine("Auto-continuing with recommended strategy");

            // Start Blazor server first
            _ = Task.Run(async () =>
            {
                try
                {
                    await App.Instance.StartBlazorServerForFallback();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error starting Blazor server: {ex}");
                }
            });

            // Wait a moment for server to start
            await Task.Delay(1000);

            // Use the normal MainWindow fallback logic
            var mainWindow = new MainWindow();

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            {
                lifetime.MainWindow = mainWindow;
                mainWindow.Show();
            }

            // Close this debug window
            Close();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in AutoContinue: {ex}");
        }
    }

    private async Task TryPhotinoBlazor()
    {
        // Removed - Photino.Blazor has been eliminated from the template
        await AutoContinue();
    }

    private async Task TryPhotinoNet()
    {
        _timeoutTimer?.Dispose();
        System.Diagnostics.Debug.WriteLine("User selected: Photino.NET");

        // Reset Blazor server to get a fresh port
        App.ResetBlazorServer();

        // Start Blazor server first
        _ = Task.Run(App.Instance.StartBlazorServerForFallback);

        // Wait for server to start
        while (!App.IsBlazorServerReady)
        {
            await Task.Delay(100);
        }
        await Task.Delay(2000);

        try
        {
            // Hide this window and try Photino.NET
            Hide();

            var photinoWindow = new PhotinoWindow()
                .SetTitle("AppName - Photino.NET Test")
                .SetUseOsDefaultSize(false)
                .SetSize(1200, 800)
                .SetResizable(true)
                .Center()
                .Load(App.BlazorUrl);

            photinoWindow.WindowClosing += (sender, args) =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
                    {
                        lifetime.Shutdown();
                    }
                });
                return false;
            };

            _photinoWindow = photinoWindow;
            _isPhotinoWindowOpen = true;

            System.Diagnostics.Debug.WriteLine("Photino.NET test window opened successfully");
            photinoWindow.WaitForClose();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Photino.NET test failed: {ex}");
            await AutoContinue();
        }
    }

    private async Task TryAvaloniaWebView()
    {
        _timeoutTimer?.Dispose();
        System.Diagnostics.Debug.WriteLine("User selected: WebView.Avalonia");

        // Reset Blazor server to get a fresh port
        App.ResetBlazorServer();

        // Start Blazor server first
        _ = Task.Run(App.Instance.StartBlazorServerForFallback);

        // Wait for server to start
        while (!App.IsBlazorServerReady)
        {
            await Task.Delay(100);
        }
        await Task.Delay(2000);

        try
        {
            // Create WebView window
            var webViewWindow = new Window
            {
                Title = "AppName - WebView.Avalonia Test",
                Width = 1200,
                Height = 800
            };

            var webView = new WebView
            {
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch
            };

            webViewWindow.Content = webView;
            webView.Url = new Uri(App.BlazorUrl);

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            {
                lifetime.MainWindow = webViewWindow;
                webViewWindow.Show();
            }

            System.Diagnostics.Debug.WriteLine("WebView.Avalonia test window opened successfully");
            Close();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"WebView.Avalonia test failed: {ex}");
            await AutoContinue();
        }
    }

    private async Task TryEmbeddedBrowser()
    {
        _timeoutTimer?.Dispose();
        System.Diagnostics.Debug.WriteLine("User selected: Embedded Browser");

        // Reset Blazor server to get a fresh port
        App.ResetBlazorServer();

        // Start Blazor server first
        _ = Task.Run(App.Instance.StartBlazorServerForFallback);

        // Create MainWindow which will show embedded browser interface
        var mainWindow = new MainWindow();

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.MainWindow = mainWindow;
            mainWindow.Show();
        }

        System.Diagnostics.Debug.WriteLine("Embedded Browser interface opened successfully");
        Close();
    }

    private void OnWindowClosing(object? sender, Avalonia.Controls.WindowClosingEventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("DebugStrategyWindow closing");

            if (_timeoutTimer != null)
            {
                _timeoutTimer.Dispose();
                _timeoutTimer = null;
            }

            if (_isPhotinoWindowOpen && _photinoWindow != null)
            {
                e.Cancel = true;
                Hide();
                return;
            }

            // Close Photino window if it exists
            if (_photinoWindow != null)
            {
                try
                {
                    _photinoWindow.Close();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error closing Photino window: {ex.Message}");
                }
                _photinoWindow = null;
            }

            // Don't shutdown application here as it might cause stack overflow
            // Let the parent handle shutdown
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnWindowClosing: {ex}");
        }
    }
}