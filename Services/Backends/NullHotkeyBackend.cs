using CheapAvaloniaBlazor.Models;

namespace CheapAvaloniaBlazor.Services.Backends;

/// <summary>
/// No-op backend for platforms where global hotkeys are not supported (macOS, unsupported Linux sessions).
/// </summary>
internal sealed class NullHotkeyBackend : IHotkeyBackend
{
    public bool IsSupported => false;

#pragma warning disable CS0067 // Event is never used (intentional — null backend never fires)
    public event Action<int>? HotkeyPressed;
#pragma warning restore CS0067

    public bool Register(int hotkeyId, HotkeyModifiers modifiers, Avalonia.Input.Key key) => false;

    public bool Unregister(int hotkeyId) => false;

    public void UnregisterAll() { }

    public void Dispose() { }
}
