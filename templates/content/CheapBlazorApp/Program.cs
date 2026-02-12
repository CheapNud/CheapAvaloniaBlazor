using CheapAvaloniaBlazor.Extensions;

namespace CheapBlazorApp;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var builder = new CheapAvaloniaBlazor.Hosting.HostBuilder()
            .WithTitle("CheapBlazorApp")
            .WithSize(1024, 768)
            .ConfigureOptions(options =>
            {
                options.EnableDevTools = false;
                options.EnableContextMenu = false;
            })
            .AddMudBlazor();

        builder.RunApp(args);
    }
}
