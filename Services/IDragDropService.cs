using CheapAvaloniaBlazor.Models;

namespace CheapAvaloniaBlazor.Services;

/// <summary>
/// Service for receiving file drag-and-drop events from the application window.
/// Uses HTML5 drag events in the WebView2 content, bridged to C# via the Photino message channel.
/// <para>
/// <b>V1 limitation:</b> File system paths are not available due to browser sandboxing in WebView2.
/// The <see cref="DroppedFileInfo.FilePath"/> property will be null. A future V2 enhancement
/// may add native backends (Win32 <c>IDropTarget</c> / <c>DragAcceptFiles</c>) for file path extraction.
/// </para>
/// </summary>
public interface IDragDropService
{
    /// <summary>
    /// Fires when one or more files are dropped onto the application window.
    /// </summary>
    event Action<IReadOnlyList<DroppedFileInfo>>? FilesDropped;

    /// <summary>
    /// Fires when a drag operation carrying files enters the application window.
    /// Subscribe to provide visual drop-zone feedback (e.g. highlight a target area).
    /// </summary>
    event Action? DragEnter;

    /// <summary>
    /// Fires when a drag operation leaves the application window or a drop completes.
    /// Subscribe to clean up visual drop-zone feedback.
    /// </summary>
    event Action? DragLeave;

    /// <summary>
    /// Whether a drag operation is currently over the application window.
    /// Transitions to <c>true</c> on <see cref="DragEnter"/> and back to <c>false</c>
    /// on <see cref="DragLeave"/> or after <see cref="FilesDropped"/>.
    /// </summary>
    bool IsDragOver { get; }
}
