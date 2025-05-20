using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using AvaloniaWebView;
using Photino.NET;
using System;
using System.Threading.Tasks;

namespace CheapAvaloniaBlazor;

public partial class MainWindow : Window
{
    private PhotinoWindow? _photinoWindow;
    private bool _isPhotinoWindowOpen = false;
    private bool _usePhotino = true;
    private bool _useAvaloniaWebView = true;

    public MainWindow()
    {
        InitializeComponent();
        Title = "AppName";
        Width = 1200;
        Height = 800;

        // Show loading state initially
        ShowLoadingContent();

        // Initialize after window is loaded
        Loaded += async (s, e) => await InitializeWebView();

        // Handle window closing
        Closing += OnWindowClosing;
    }

    private async Task InitializeWebView()
    {
        try
        {
            // Wait for Blazor server to be ready (only needed in fallback mode)
            while (!App.IsBlazorServerReady)
            {
                await Task.Delay(100);
            }

            // Additional delay to ensure server is fully started
            await Task.Delay(2000);

            // Get available strategies for this system
            var strategies = PlatformHelper.GetAllWebViewStrategies();
            System.Diagnostics.Debug.WriteLine($"Available fallback strategies: {string.Join(", ", strategies)}");

            // Try Photino.NET first (manual setup)
            if (_usePhotino && PlatformHelper.IsPhotinoNetSupported())
            {
                try
                {
                    await InitializePhotino();
                    return;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Photino.NET failed: {ex}");
                    _usePhotino = false;
                }
            }

            // Try Avalonia WebView second if supported
            if (_useAvaloniaWebView && PlatformHelper.IsAvaloniaWebViewSupported())
            {
                try
                {
                    await InitializeAvaloniaWebView();
                    return;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Avalonia WebView failed: {ex}");
                    _useAvaloniaWebView = false;
                }
            }

            // Fallback to embedded browser interface
            ShowEmbeddedBrowserInterface();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to initialize WebView: {ex}");
            ShowFallbackContent($"Initialization failed: {ex.Message}");
        }
    }

    private async Task InitializePhotino()
    {
        System.Diagnostics.Debug.WriteLine("Initializing manual Photino.NET");

        // Hide the Avalonia window and show Photino window
        Hide();

        // Create Photino window with your Blazor app
        _photinoWindow = new PhotinoWindow()
            .SetTitle("AppName")
            .SetUseOsDefaultSize(false)
            .SetSize(1200, 800)
            .SetResizable(true)
            .Center()
            .RegisterWebMessageReceivedHandler((sender, message) =>
            {
                System.Diagnostics.Debug.WriteLine($"Received message: {message}");
            })
            .Load(App.BlazorUrl);

        _photinoWindow.WindowClosing += (sender, args) =>
        {
            _isPhotinoWindowOpen = false;
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
                {
                    lifetime.Shutdown();
                }
            });
            return false;
        };

        _isPhotinoWindowOpen = true;

        System.Diagnostics.Debug.WriteLine($"Opening manual Photino window with URL: {App.BlazorUrl}");
        _photinoWindow.WaitForClose();
    }

    private async Task InitializeAvaloniaWebView()
    {
        System.Diagnostics.Debug.WriteLine("Initializing Avalonia WebView");

        try
        {
            // Create WebView using WebView.Avalonia package
            var webView = new WebView
            {
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch
            };

            // Set content and show window first
            Content = webView;
            Show();

            // Navigate to Blazor app using the correct property
            webView.Url = new Uri(App.BlazorUrl);

            System.Diagnostics.Debug.WriteLine($"Avalonia WebView loaded with URL: {App.BlazorUrl}");

            // Wait a bit to ensure navigation completes
            await Task.Delay(1000);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Avalonia WebView initialization failed: {ex}");
            throw; // Re-throw to trigger fallback
        }
    }

    private void ShowEmbeddedBrowserInterface()
    {
        System.Diagnostics.Debug.WriteLine("Showing embedded browser interface");

        // Create embedded browser interface
        var browserPanel = new StackPanel
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
            Background = Avalonia.Media.Brushes.White,
            Spacing = 20
        };

        // Header
        var headerPanel = new StackPanel
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Spacing = 10,
            Margin = new Avalonia.Thickness(20, 30, 20, 10)
        };

        headerPanel.Children.Add(new TextBlock
        {
            Text = "AppName",
            FontSize = 28,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        });

        headerPanel.Children.Add(new TextBlock
        {
            Text = "Desktop Application - Fallback Mode",
            FontSize = 16,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Foreground = Avalonia.Media.Brushes.Gray
        });

        browserPanel.Children.Add(headerPanel);

        // Info panel
        var infoPanel = new Border
        {
            Background = Avalonia.Media.Brushes.LightBlue,
            CornerRadius = new Avalonia.CornerRadius(8),
            Padding = new Avalonia.Thickness(20),
            Margin = new Avalonia.Thickness(40, 0),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
        };

        var infoStack = new StackPanel
        {
            Spacing = 15
        };

        infoStack.Children.Add(new TextBlock
        {
            Text = "🚀 Application Running Successfully",
            FontSize = 18,
            FontWeight = Avalonia.Media.FontWeight.SemiBold
        });

        infoStack.Children.Add(new TextBlock
        {
            Text = $"Your Blazor application is running at:",
            FontSize = 14
        });

        infoStack.Children.Add(new SelectableTextBlock
        {
            Text = App.BlazorUrl,
            FontSize = 14,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            Foreground = Avalonia.Media.Brushes.DarkBlue
        });

        // Determine the reason for fallback
        string reasonText;
        if (!PlatformHelper.IsPhotinoNetSupported() || !_usePhotino)
        {
            reasonText = $"Photino.NET not supported: {PlatformHelper.GetPhotinoUnsupportedReason()}";
        }
        else if (!PlatformHelper.IsAvaloniaWebViewSupported() || !_useAvaloniaWebView)
        {
            reasonText = "WebView component not available on this system";
        }
        else
        {
            reasonText = "Using fallback mode for compatibility";
        }

        infoStack.Children.Add(new TextBlock
        {
            Text = reasonText,
            FontSize = 12,
            Foreground = Avalonia.Media.Brushes.DarkGray,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        });

        infoPanel.Child = infoStack;
        browserPanel.Children.Add(infoPanel);

        // Buttons panel
        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Spacing = 15,
            Margin = new Avalonia.Thickness(0, 20)
        };

        var openBrowserButton = new Button
        {
            Content = "🌐 Open in Browser",
            Padding = new Avalonia.Thickness(20, 12),
            FontSize = 14,
            Background = Avalonia.Media.Brushes.DodgerBlue,
            Foreground = Avalonia.Media.Brushes.White
        };

        openBrowserButton.Click += (s, e) =>
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = App.BlazorUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to open browser: {ex.Message}");
            }
        };

        var retryWebViewButton = new Button
        {
            Content = "🔄 Retry WebViews",
            Padding = new Avalonia.Thickness(20, 12),
            FontSize = 14
        };

        retryWebViewButton.Click += async (s, e) =>
        {
            ShowLoadingContent();
            _usePhotino = true;
            _useAvaloniaWebView = true;
            await InitializeWebView();
        };

        buttonPanel.Children.Add(openBrowserButton);
        if (PlatformHelper.IsPhotinoNetSupported() || PlatformHelper.IsAvaloniaWebViewSupported())
        {
            buttonPanel.Children.Add(retryWebViewButton);
        }

        browserPanel.Children.Add(buttonPanel);

        // Footer
        var footerPanel = new StackPanel
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Margin = new Avalonia.Thickness(0, 30),
            Spacing = 5
        };

        footerPanel.Children.Add(new TextBlock
        {
            Text = "💡 The application tries: Photino.NET → WebView.Avalonia → Browser",
            FontSize = 12,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Foreground = Avalonia.Media.Brushes.Gray
        });

        var platformInfo = $"Platform: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}";
        footerPanel.Children.Add(new TextBlock
        {
            Text = platformInfo,
            FontSize = 10,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Foreground = Avalonia.Media.Brushes.LightGray
        });

        browserPanel.Children.Add(footerPanel);

        // Set content and show window
        Content = new ScrollViewer
        {
            Content = browserPanel,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };

        Show();
    }

    private void ShowLoadingContent()
    {
        var loadingPanel = new StackPanel
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Spacing = 20
        };

        loadingPanel.Children.Add(new TextBlock
        {
            Text = "AppName",
            FontSize = 32,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        });

        loadingPanel.Children.Add(new TextBlock
        {
            Text = "Starting application...",
            FontSize = 16,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        });

        loadingPanel.Children.Add(new ProgressBar
        {
            IsIndeterminate = true,
            Width = 300,
            Height = 6
        });

        Content = loadingPanel;
    }

    private void ShowFallbackContent(string message)
    {
        var fallbackPanel = new StackPanel
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Spacing = 20
        };

        fallbackPanel.Children.Add(new TextBlock
        {
            Text = "AppName",
            FontSize = 32,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        });

        fallbackPanel.Children.Add(new TextBlock
        {
            Text = message,
            FontSize = 16,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            MaxWidth = 600
        });

        var openBrowserButton = new Button
        {
            Content = "Open in Browser",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Padding = new Avalonia.Thickness(20, 10)
        };

        openBrowserButton.Click += (s, e) =>
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = App.BlazorUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to open browser: {ex.Message}");
            }
        };

        fallbackPanel.Children.Add(openBrowserButton);

        Content = fallbackPanel;
    }

    private void OnWindowClosing(object? sender, Avalonia.Controls.WindowClosingEventArgs e)
    {
        if (_isPhotinoWindowOpen)
        {
            e.Cancel = true;
            Hide();
        }
        else
        {
            try
            {
                _photinoWindow?.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error closing Photino window: {ex.Message}");
            }

            // Shutdown the application
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            {
                lifetime.Shutdown();
            }
        }
    }
}