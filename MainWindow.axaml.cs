using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Photino.NET;
using System;
using System.Threading.Tasks;

namespace CheapAvaloniaBlazor;

public partial class MainWindow : Window
{
    private PhotinoWindow? _photinoWindow;
    private bool _isPhotinoWindowOpen = false;

    public MainWindow()
    {
        InitializeComponent();
        Title = "AppName";
        Width = 1200;
        Height = 800;

        // Show loading state initially
        ShowLoadingContent();

        // Initialize Photino after window is loaded
        Loaded += async (s, e) => await InitializePhotino();

        // Handle window closing
        Closing += OnWindowClosing;
    }

    private async Task InitializePhotino()
    {
        try
        {
            // Wait for Blazor server to be ready
            while (!App.IsBlazorServerReady)
            {
                await Task.Delay(100);
            }

            // Additional delay to ensure server is fully started
            await Task.Delay(2000);

            // Hide the Avalonia window and show Photino window
            Hide();

            // Create Photino window with your Blazor app
            _photinoWindow = new PhotinoWindow()
                .SetTitle("AppName")
                .SetUseOsDefaultSize(false) // Important: disable OS default sizing
                .SetSize(1200, 800)
                .SetResizable(true)
                .Center() // Center the window on screen
                .RegisterWebMessageReceivedHandler((sender, message) =>
                {
                    // Handle messages from JavaScript if needed
                    System.Diagnostics.Debug.WriteLine($"Received message: {message}");
                })
                .Load(App.BlazorUrl); // Correct method name

            // Set up event handlers with correct API - register AFTER creating window
            _photinoWindow.WindowClosing += (sender, args) =>
            {
                _isPhotinoWindowOpen = false;
                // Close the Avalonia application when Photino window closes
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
                    {
                        lifetime.Shutdown();
                    }
                });
                return false; // Allow window to close
            };

            _isPhotinoWindowOpen = true;

            System.Diagnostics.Debug.WriteLine($"Opening Photino window with URL: {App.BlazorUrl}");

            // Show and wait for the Photino window
            _photinoWindow.WaitForClose();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to initialize Photino: {ex}");

            // Show the Avalonia window with fallback content
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                Show();
                ShowFallbackContent($"Failed to initialize WebView: {ex.Message}");
            });
        }
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

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        // If Photino window is open, don't close Avalonia window
        if (_isPhotinoWindowOpen)
        {
            e.Cancel = true;
            Hide(); // Just hide it instead
        }
        else
        {
            // Close the Photino window if it exists
            try
            {
                _photinoWindow?.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error closing Photino window: {ex.Message}");
            }
        }
    }
}