using CheapAvaloniaBlazor.Extensions;

namespace MinimalApp;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var builder = new CheapAvaloniaBlazor.Hosting.HostBuilder()
            .WithTitle("Minimal App")
            .WithSize(1024, 768)
            // Native app feel - no console window, no DevTools, no right-click menu
            .ConfigureOptions(options =>
            {
                options.EnableDevTools = false;
                options.EnableContextMenu = false;
                // EnableConsoleLogging defaults to false, which hides the console window
            })
            .AddMudBlazor();

        builder.RunApp(args);
    }
}
