using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Reflection;

namespace CheapAvaloniaBlazor.Utilities;

/// <summary>
/// Centralizes the reflection-based MapRazorComponents logic used by both
/// <see cref="Extensions.WebApplicationExtensions"/> and <see cref="Services.EmbeddedBlazorHostService"/>.
///
/// The library doesn't have a compile-time reference to the consumer's App type,
/// so we use reflection to call the generic MapRazorComponents&lt;TApp&gt; method.
///
/// Note: DynamicDependency/AOT attributes are not used here. This library targets Blazor Server
/// which inherently requires runtime reflection for SignalR circuits. AOT is not a target.
/// </summary>
public static class BlazorComponentMapper
{
    /// <summary>
    /// Discovers the consumer's App component type from the entry assembly.
    /// Returns null if not found.
    /// </summary>
    public static Type? DiscoverAppType()
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly == null)
            return null;

        return entryAssembly.GetType(Constants.ComponentNames.App)
            ?? entryAssembly.GetTypes().FirstOrDefault(t => t.Name == Constants.ComponentNames.App);
    }

    /// <summary>
    /// Calls MapRazorComponents&lt;TApp&gt;().AddAdditionalAssemblies(...).AddInteractiveServerRenderMode()
    /// via reflection.
    /// </summary>
    /// <param name="app">The WebApplication to map components on.</param>
    /// <param name="appType">The consumer's App component type (from <see cref="DiscoverAppType"/>).</param>
    /// <param name="libraryAssembly">The calling library's assembly, excluded from additional assembly scanning.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <returns>True if mapping succeeded, false if a reflection step failed.</returns>
    public static bool TryMapRazorComponents(
        WebApplication app,
        Type appType,
        Assembly libraryAssembly,
        ILogger? logger = null)
    {
        // Step 1: Find MapRazorComponents generic method
        var mapMethod = typeof(RazorComponentsEndpointRouteBuilderExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == "MapRazorComponents" && m.IsGenericMethod);

        if (mapMethod == null)
        {
            logger?.LogError("MapRazorComponents method not found on RazorComponentsEndpointRouteBuilderExtensions. " +
                "The ASP.NET Core framework API may have changed.");
            return false;
        }

        var genericMapMethod = mapMethod.MakeGenericMethod(appType);
        var conventionBuilder = genericMapMethod.Invoke(null, [app]);

        if (conventionBuilder == null)
        {
            logger?.LogError("MapRazorComponents<{AppType}> returned null.", appType.FullName);
            return false;
        }

        // Step 2: Discover additional assemblies with routable Razor components
        var additionalAssemblies = DiscoverRoutableAssemblies(appType, libraryAssembly, logger);

        if (additionalAssemblies is { Length: > 0 })
        {
            var addAssembliesMethod = typeof(RazorComponentsEndpointConventionBuilderExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "AddAdditionalAssemblies");

            if (addAssembliesMethod == null)
            {
                logger?.LogWarning("AddAdditionalAssemblies method not found. " +
                    "Routable components in referenced assemblies will not be discovered.");
            }
            else
            {
                conventionBuilder = addAssembliesMethod.Invoke(null, [conventionBuilder, additionalAssemblies]);
            }
        }

        // Step 3: Add InteractiveServerRenderMode
        var addServerModeMethod = typeof(ServerRazorComponentsEndpointConventionBuilderExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == "AddInteractiveServerRenderMode" && m.GetParameters().Length == 1);

        if (addServerModeMethod == null)
        {
            logger?.LogError("AddInteractiveServerRenderMode method not found on ServerRazorComponentsEndpointConventionBuilderExtensions. " +
                "The ASP.NET Core framework API may have changed.");
            return false;
        }

        addServerModeMethod.Invoke(null, [conventionBuilder]);

        logger?.LogInformation("MapRazorComponents<{AppType}> with InteractiveServerRenderMode configured successfully.", appType.FullName);
        return true;
    }

    /// <summary>
    /// Discovers referenced assemblies that contain routable Razor components (@page directives).
    /// MapRazorComponents only scans the App type's assembly by default - additional assemblies
    /// with routable pages (like shared component libraries) must be registered explicitly.
    /// </summary>
    private static Assembly[]? DiscoverRoutableAssemblies(Type appType, Assembly libraryAssembly, ILogger? logger)
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly == null)
            return null;

        var routeAttributeType = typeof(Microsoft.AspNetCore.Components.RouteAttribute);

        var assemblies = entryAssembly.GetReferencedAssemblies()
            .Select(name =>
            {
                try
                {
                    return Assembly.Load(name);
                }
                catch (Exception ex) when (ex is BadImageFormatException or FileNotFoundException or FileLoadException)
                {
                    logger?.LogDebug("Could not load referenced assembly {AssemblyName}: {Error}", name.Name, ex.Message);
                    return null;
                }
            })
            .Where(asm => asm != null && asm != entryAssembly && asm != libraryAssembly)
            .Where(asm => asm!.GetTypes().Any(t => t.GetCustomAttributes(routeAttributeType, false).Length > 0))
            .ToArray();

        if (assemblies is { Length: > 0 })
        {
            logger?.LogInformation("Discovered {Count} additional assemblies with routable components: {Assemblies}",
                assemblies.Length,
                string.Join(", ", assemblies.Select(a => a!.GetName().Name)));
        }

        return assemblies!;
    }
}
