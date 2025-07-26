using Avalonia;
using CheapAvaloniaBlazor.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace CheapAvaloniaBlazor.Extensions;

public static class AppBuilderExtensions
{
    public static AppBuilder UseCheapAvaloniaBlazor(
        this AppBuilder builder,
        Action<CheapAvaloniaBlazorOptions>? configure = null)
    {
        var options = new CheapAvaloniaBlazorOptions();
        configure?.Invoke(options);

        // Register services
        builder.AfterSetup(app =>
        {
            var services = new ServiceCollection();
            services.AddCheapAvaloniaBlazor(options);

            // Store service provider for later use
            CheapAvaloniaBlazorRuntime.Initialize(services.BuildServiceProvider());
        });

        return builder;
    }
}