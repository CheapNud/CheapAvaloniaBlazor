using CheapAvaloniaBlazor.Models;

// Fix for CS1503: Convert the `Filters` property to the expected type `(string Name, string[] Extensions)[]`
// by adding a helper method `ToPhotinoFilters` to handle the conversion.
public static class FileDialogOptionsExtensions
{
    public static (string Name, string[] Extensions)[]? ToPhotinoFilters(this List<FileFilter>? filters)
    {
        return filters?.Select(filter => (filter.Name, filter.Extensions)).ToArray();
    }
}
