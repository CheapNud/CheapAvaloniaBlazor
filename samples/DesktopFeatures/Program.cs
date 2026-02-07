using CheapAvaloniaBlazor.Extensions;

namespace DesktopFeatures;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var builder = new CheapAvaloniaBlazor.Hosting.HostBuilder()
            .WithTitle("Desktop Features Demo")
            .WithSize(1200, 800)
            .ConfigureOptions(options =>
            {
                options.EnableDevTools = true;
                options.EnableContextMenu = true;
                options.EnableConsoleLogging = true;
            })
            // System tray configuration
            .EnableSystemTray()
            .CloseToTray()
            .WithTrayTooltip("Desktop Features Demo - Click to restore")
            // Notification configuration
            .EnableSystemNotifications()
            // Settings persistence
            .WithSettingsAppName("DesktopFeatures")
            .AddMudBlazor();

        builder.RunApp(args);
    }
}
