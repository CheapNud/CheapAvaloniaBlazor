namespace CheapAvaloniaBlazor.Models;

// Supporting classes
public class FileDialogOptions
{
    public string? Title { get; set; }
    public bool MultiSelect { get; set; }
    public string? DefaultFileName { get; set; }
    public List<FileFilter>? Filters { get; set; }
}
