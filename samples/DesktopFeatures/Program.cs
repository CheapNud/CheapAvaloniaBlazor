using CheapAvaloniaBlazor.Extensions;
using DesktopFeatures.Services;
using Microsoft.Extensions.DependencyInjection;

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
            .AddMudBlazor();

        // Register custom services
        builder.Services.AddSingleton<SettingsService>();

        builder.RunApp(args);
    }
}
