using CheapAvaloniaBlazor.Extensions;

namespace TemplateApp;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var builder = new CheapAvaloniaBlazor.Hosting.HostBuilder()
            .WithTitle("TemplateApp")
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
// drift
