using CheapAvaloniaBlazor.Models;

namespace CheapAvaloniaBlazor.Services.Backends;

/// <summary>
/// No-op backend for platforms where native menu bars are not supported (Linux, macOS).
/// </summary>
internal sealed class NullMenuBarBackend : IMenuBarBackend
{
    public bool IsSupported => false;

#pragma warning disable CS0067 // Event is never used (intentional — null backend never fires)
    public event Action<string>? MenuItemClicked;
#pragma warning restore CS0067

    public void Initialize(IntPtr windowHandle, IEnumerable<MenuItemDefinition> menus) { }

    public void SetMenuBar(IEnumerable<MenuItemDefinition> menus) { }

    public void EnableMenuItem(string menuItemId, bool enabled) { }

    public void CheckMenuItem(string menuItemId, bool isChecked) { }

    public void Dispose() { }
}
