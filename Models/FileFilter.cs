namespace CheapAvaloniaBlazor.Models;

public class FileFilter
{
    public string Name { get; set; } = "";
    public string[] Extensions { get; set; } = Array.Empty<string>();
}
