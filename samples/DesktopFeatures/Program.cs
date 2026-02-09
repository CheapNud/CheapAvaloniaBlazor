using CheapAvaloniaBlazor.Extensions;
using CheapAvaloniaBlazor.Models;

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
            // Native menu bar
            .WithMenuBar(
            [
                MenuItemDefinition.CreateSubMenu("&File",
                [
                    MenuItemDefinition.Create("&New", () => { }, id: "file_new", accelerator: "Ctrl+N"),
                    MenuItemDefinition.Create("&Open...", () => { }, id: "file_open", accelerator: "Ctrl+O"),
                    MenuItemDefinition.Separator(),
                    MenuItemDefinition.Create("E&xit", () => Environment.Exit(0), id: "file_exit", accelerator: "Alt+F4"),
                ]),
                MenuItemDefinition.CreateSubMenu("&View",
                [
                    MenuItemDefinition.CreateCheckable("&Dark Mode", false, () => { }, id: "view_darkmode"),
                    MenuItemDefinition.Separator(),
                    MenuItemDefinition.Create("&Refresh", () => { }, id: "view_refresh", accelerator: "F5"),
                ]),
                MenuItemDefinition.CreateSubMenu("&Help",
                [
                    MenuItemDefinition.Create("&About", () => { }, id: "help_about"),
                ]),
            ])
            .AddMudBlazor();

        builder.RunApp(args);
    }
}
