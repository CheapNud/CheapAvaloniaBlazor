using CheapAvaloniaBlazor.Models;

namespace CheapAvaloniaBlazor.Services;

/// <summary>
/// Service for creating and managing child windows and modal dialogs.
/// Child windows are created on the main Photino thread via Invoke() and connect to the
/// same Blazor server as independent SignalR circuits.
/// </summary>
public interface IWindowService : IDisposable
{
    /// <summary>
    /// Whether the current platform supports modal dialog behavior (parent window disabling).
    /// Windows are always supported; this indicates modal-specific support.
    /// </summary>
    bool IsModalSupported { get; }

    /// <summary>
    /// Create a new child window. Returns the window ID that can be used with other methods.
    /// </summary>
    /// <param name="options">Window creation options (URL path or component type, size, title, etc.)</param>
    /// <param name="cancellationToken">Token to cancel the window creation.</param>
    /// <returns>Unique window ID for the new window.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the component type whitelist is full
    /// (more than <see cref="Constants.Window.MaxRegisteredComponentTypes"/> distinct types).
    /// Re-using the same component type does not count against the limit.</exception>
    Task<string> CreateWindowAsync(WindowOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a modal dialog that disables the parent window until closed.
    /// The returned task completes when the modal is closed (via CompleteModal or user X-close).
    /// </summary>
    /// <param name="options">Window creation options. ParentWindowId defaults to main window.</param>
    /// <param name="cancellationToken">Token to cancel the modal wait. If cancelled, the parent
    /// window is re-enabled and the modal window is closed. Returns <see cref="ModalResult.Cancel()"/>.</param>
    /// <returns>The modal result (confirmed/cancelled with optional data payload).</returns>
    /// <inheritdoc cref="CreateWindowAsync" path="/exception"/>
    Task<ModalResult> CreateModalAsync(WindowOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Close a child window by its ID. No-op if the window has already been closed.
    /// </summary>
    Task CloseWindowAsync(string windowId);

    /// <summary>
    /// Complete a modal dialog with a result. Called by the Blazor component inside the modal.
    /// This re-enables the parent window and closes the modal.
    /// </summary>
    /// <param name="windowId">The modal window's ID (from _windowId query parameter).</param>
    /// <param name="result">The dialog result to return to the caller of CreateModalAsync.</param>
    void CompleteModal(string windowId, ModalResult result);

    /// <summary>
    /// Get all active child window IDs (does not include the main window).
    /// </summary>
    IReadOnlyList<string> GetWindows();

    /// <summary>
    /// Send a message to a specific window. The target component subscribes to <see cref="MessageReceived"/>
    /// and filters by its own window ID.
    /// </summary>
    void SendMessage(string windowId, string messageType, object? payload = null);

    /// <summary>
    /// Broadcast a message to all windows (main + children).
    /// </summary>
    void BroadcastMessage(string messageType, object? payload = null);

    /// <summary>
    /// Resolve a previously registered component type by its full name.
    /// Only types passed via <see cref="WindowOptions.ComponentType"/> are registered.
    /// Used internally by WindowHost.razor — prevents arbitrary type instantiation from URL params.
    /// </summary>
    /// <returns>The registered component type, or null if not found.</returns>
    Type? ResolveWindowComponent(string fullName);

    /// <summary>
    /// Fires when a new child window has been created and its native handle is available.
    /// Parameter: windowId.
    /// </summary>
    event Action<string>? WindowCreated;

    /// <summary>
    /// Fires when a child window has been closed.
    /// Parameter: windowId.
    /// </summary>
    event Action<string>? WindowClosed;

    /// <summary>
    /// Fires when a message is sent to a specific window or broadcast to all.
    /// Parameters: (targetWindowId, messageType, payload).
    /// Use "*" as targetWindowId for broadcast messages.
    /// </summary>
    event Action<string, string, object?>? MessageReceived;
}
