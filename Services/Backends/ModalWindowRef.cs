using Photino.NET;

namespace CheapAvaloniaBlazor.Services.Backends;

/// <summary>
/// Platform-neutral reference to a native window for modal operations.
/// The Windows backend works through <see cref="Handle"/>; Photino does not expose
/// native window handles on Linux, so the GTK backend works through the
/// <see cref="Window"/> reference instead.
/// </summary>
internal readonly struct ModalWindowRef(IntPtr handle, PhotinoWindow? window)
{
    public IntPtr Handle { get; } = handle;
    public PhotinoWindow? Window { get; } = window;

    public bool IsEmpty => Handle == IntPtr.Zero && Window is null;
}
