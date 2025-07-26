/// <summary>
/// Interface for windows that host Blazor content
/// </summary>
public interface IBlazorWindow
{
    string Title { get; set; }
    double Width { get; set; }
    double Height { get; set; }
}