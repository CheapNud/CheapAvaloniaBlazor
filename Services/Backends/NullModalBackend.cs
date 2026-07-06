namespace CheapAvaloniaBlazor.Services.Backends;

/// <summary>
/// No-op modal backend for platforms where native modal behavior is not available (macOS).
/// Windows still open and close, but the parent is not disabled.
/// </summary>
internal sealed class NullModalBackend : IModalBackend
{
    public bool IsSupported => false;

    public void DisableParentWindow(ModalWindowRef parent, ModalWindowRef modal) { }

    public void EnableParentWindow(ModalWindowRef parent) { }

    public void PostCloseMessage(ModalWindowRef window) { }

    public void Dispose() { }
}
