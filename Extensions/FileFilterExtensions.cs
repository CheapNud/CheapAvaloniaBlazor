using CheapAvaloniaBlazor.Models;

namespace CheapAvaloniaBlazor.Extensions;

// Extension methods
internal static class FileFilterExtensions
{
    public static string[][] ToPhotinoFilters(this List<FileFilter> filters)
    {
        return filters.Select(f => new[] { f.Name, string.Join(";", f.Extensions) }).ToArray();
    }
}