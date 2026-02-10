namespace CheapAvaloniaBlazor.Models;

/// <summary>
/// Result returned by a modal dialog created via <see cref="Services.IWindowService.CreateModalAsync"/>.
/// The modal component calls <see cref="Services.IWindowService.CompleteModal"/> to set the result.
/// </summary>
public class ModalResult
{
    /// <summary>
    /// Whether the user confirmed the dialog (OK/Save). False indicates cancel or X-close.
    /// </summary>
    public bool Confirmed { get; set; }

    /// <summary>
    /// Optional data payload returned by the dialog (form values, selection, etc.).
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// Create a confirmed result with optional data.
    /// </summary>
    public static ModalResult Ok(object? data = null) => new() { Confirmed = true, Data = data };

    /// <summary>
    /// Create a cancelled result (no data).
    /// </summary>
    public static ModalResult Cancel() => new() { Confirmed = false };

    /// <summary>
    /// Retrieve the data payload as a typed value.
    /// </summary>
    public T? GetData<T>() => Data is T typed ? typed : default;
}
