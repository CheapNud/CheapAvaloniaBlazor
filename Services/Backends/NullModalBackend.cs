namespace CheapAvaloniaBlazor.Services.Backends;

/// <summary>
/// No-op modal backend for platforms where native modal behavior is not available (Linux, macOS).
/// Windows still open and close, but the parent is not disabled.
/// </summary>
internal sealed class NullModalBackend : IModalBackend
{
    public bool IsSupported => false;

    public void DisableParentWindow(IntPtr parentHandle) { }

    public void EnableParentWindow(IntPtr parentHandle) { }

    public void PostCloseMessage(IntPtr windowHandle) { }

    public void Dispose() { }
}
