using Microsoft.AspNetCore.Components;

namespace CheapAvaloniaBlazor.Models;

/// <summary>
/// Options for creating a child window or modal dialog via <see cref="Services.IWindowService"/>.
/// Use the static factory methods for concise creation.
/// </summary>
public class WindowOptions
{
    /// <summary>
    /// Window title. Defaults to the application title if null.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Window width in pixels.
    /// </summary>
    public int Width { get; set; } = Constants.Window.DefaultChildWidth;

    /// <summary>
    /// Window height in pixels.
    /// </summary>
    public int Height { get; set; } = Constants.Window.DefaultChildHeight;

    /// <summary>
    /// Whether the window can be resized.
    /// </summary>
    public bool Resizable { get; set; } = true;

    /// <summary>
    /// Center the window on the parent window. When false, the OS decides placement.
    /// </summary>
    public bool CenterOnParent { get; set; } = true;

    // ── Content (exactly one should be set) ──────────────────────────────────

    /// <summary>
    /// Blazor URL path to load (e.g. "/settings"). Mutually exclusive with <see cref="ComponentType"/>.
    /// </summary>
    public string? UrlPath { get; set; }

    /// <summary>
    /// Blazor component type to render via DynamicComponent (e.g. typeof(SettingsDialog)).
    /// Mutually exclusive with <see cref="UrlPath"/>. The component does NOT need a @page directive.
    /// </summary>
    /// <remarks>
    /// Each distinct component type is registered in an internal whitelist on first use.
    /// The whitelist is capped at <see cref="Constants.Window.MaxRegisteredComponentTypes"/> (256)
    /// distinct types. Re-using the same type for multiple windows does not count again.
    /// This limit exists as a safety guard — typical apps use far fewer component types.
    /// </remarks>
    public Type? ComponentType { get; set; }

    /// <summary>
    /// Optional parameters to pass to the component when using <see cref="ComponentType"/>.
    /// </summary>
    public Dictionary<string, object>? Parameters { get; set; }

    // ── Modal ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Parent window ID for modal dialogs. Null defaults to the main window.
    /// </summary>
    public string? ParentWindowId { get; set; }

    // ── Factory methods ──────────────────────────────────────────────────────

    /// <summary>
    /// Create options that load a Blazor URL path (e.g. "/settings").
    /// </summary>
    public static WindowOptions FromUrl(string path, string? title = null) => new()
    {
        UrlPath = path,
        Title = title,
    };

    /// <summary>
    /// Create options that render a Blazor component type via DynamicComponent.
    /// </summary>
    public static WindowOptions FromComponent<T>(string? title = null) where T : IComponent => new()
    {
        ComponentType = typeof(T),
        Title = title,
    };

    /// <summary>
    /// Create options that render a Blazor component type via DynamicComponent.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="componentType"/> does not implement <see cref="IComponent"/>.</exception>
    public static WindowOptions FromComponent(Type componentType, string? title = null)
    {
        if (!typeof(IComponent).IsAssignableFrom(componentType))
            throw new ArgumentException($"Type '{componentType.FullName}' does not implement IComponent.", nameof(componentType));

        return new()
        {
            ComponentType = componentType,
            Title = title,
        };
    }
}
