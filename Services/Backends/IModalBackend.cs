namespace CheapAvaloniaBlazor.Services.Backends;

/// <summary>
/// Platform-specific backend for modal dialog window management.
/// Handles disabling/enabling the parent window to create true modal behavior.
/// </summary>
internal interface IModalBackend : IDisposable
{
    /// <summary>
    /// Whether this backend provides actual modal behavior (parent window disabling).
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// Disable the parent window so it cannot receive input while the modal is open.
    /// The modal window reference is provided because some backends (GTK) need it to
    /// tell the two windows apart; the Win32 backend ignores it.
    /// </summary>
    void DisableParentWindow(ModalWindowRef parent, ModalWindowRef modal);

    /// <summary>
    /// Re-enable the parent window after the modal is closed.
    /// </summary>
    void EnableParentWindow(ModalWindowRef parent);

    /// <summary>
    /// Request a window close in a thread-safe manner.
    /// </summary>
    void PostCloseMessage(ModalWindowRef window);
}
