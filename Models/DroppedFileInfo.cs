namespace CheapAvaloniaBlazor.Models;

/// <summary>
/// Metadata about a file dropped onto the application window via drag-and-drop.
/// </summary>
public class DroppedFileInfo
{
    /// <summary>
    /// File name including extension (e.g. "document.pdf").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long Size { get; init; }

    /// <summary>
    /// MIME content type (e.g. "image/png", "application/pdf").
    /// Empty string when the browser cannot determine the type.
    /// </summary>
    public string ContentType { get; init; } = string.Empty;

    /// <summary>
    /// When the file was last modified.
    /// </summary>
    public DateTimeOffset LastModified { get; init; }

    /// <summary>
    /// Full file system path. Only populated when a native drag-and-drop backend
    /// is available (future V2 enhancement). Null when using the cross-platform
    /// HTML5 fallback, which does not expose file paths due to browser sandboxing.
    /// </summary>
    public string? FilePath { get; init; }
}
