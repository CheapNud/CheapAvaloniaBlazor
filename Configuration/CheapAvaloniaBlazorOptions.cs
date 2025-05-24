// Configuration options
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

public class CheapAvaloniaBlazorOptions
{
    public int Port { get; set; } = 5000;
    public bool UseHttps { get; set; } = false;
    public bool EnableConsoleLogging { get; set; } = false;
    public string? ContentRoot { get; set; }
    public string? WebRoot { get; set; }

    // Configuration delegates
    public Action<IServiceCollection>? ConfigureServices { get; set; }
    public Action<WebApplication>? ConfigurePipeline { get; set; }
    public Action<WebApplication>? ConfigureEndpoints { get; set; }

    // Window options
    public string DefaultWindowTitle { get; set; } = "Blazor Desktop App";
    public int DefaultWindowWidth { get; set; } = 1200;
    public int DefaultWindowHeight { get; set; } = 800;
    public bool CenterWindow { get; set; } = true;
    public bool Resizable { get; set; } = true;
}