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
    /// </summary>
    void DisableParentWindow(IntPtr parentHandle);

    /// <summary>
    /// Re-enable the parent window after the modal is closed.
    /// </summary>
    void EnableParentWindow(IntPtr parentHandle);

    /// <summary>
    /// Post a close message to a window in a thread-safe manner.
    /// </summary>
    void PostCloseMessage(IntPtr windowHandle);
}
