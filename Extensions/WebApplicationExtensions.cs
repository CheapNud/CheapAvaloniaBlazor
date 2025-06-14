using Avalonia;
using Avalonia.Controls;
using CheapAvaloniaBlazor.Hosting;
using CheapAvaloniaBlazor.Windows;

namespace CheapAvaloniaBlazor.Extensions
{
    public static class WebApplicationExtensions
    {
        /// <summary>
        /// Creates a Blazor-hosted window with custom service configuration
        /// </summary>
        public static BlazorHostWindow CreateBlazorWindow(
            this Application application,
            Action<HostBuilder>? configure = null)
        {
            var builder = new HostBuilder();
            configure?.Invoke(builder);

            return builder.Build();
        }

        /// <summary>
        /// Creates a Blazor-hosted window with a custom window type
        /// </summary>
        public static T CreateBlazorWindow<T>(
            this Application application,
            Action<HostBuilder>? configure = null)
            where T : Window, IBlazorWindow, new()
        {
            var builder = new HostBuilder();
            configure?.Invoke(builder);

            return builder.Build<T>();
        }
    }
}
