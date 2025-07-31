using Avalonia.Controls;

/// <summary>
/// Interface for windows that can host Blazor content
/// </summary>
public interface IBlazorWindow
{
    /// <summary>
    /// The window title
    /// </summary>
    string? Title { get; set; }

    /// <summary>
    /// The window width
    /// </summary>
    double Width { get; set; }

    /// <summary>
    /// The window height
    /// </summary>
    double Height { get; set; }

    /// <summary>
    /// Show the window
    /// </summary>
    void Show();

    /// <summary>
    /// Show the window as a dialog
    /// </summary>
    /// <param name="owner">The owner window</param>
    /// <returns>Task that completes when the dialog is closed</returns>
    Task ShowDialog(Window? owner);

    /// <summary>
    /// Close the window
    /// </summary>
    void Close();

    /// <summary>
    /// Run the window and start the application message loop
    /// </summary>
    void Run();
}