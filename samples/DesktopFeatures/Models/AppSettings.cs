namespace DesktopFeatures.Models;

/// <summary>
/// Application settings that persist between sessions.
/// Stored as JSON in %LocalAppData%\DesktopFeatures\settings.json
/// </summary>
public class AppSettings
{
    public bool IsDarkMode { get; set; } = true;
    public string LastOpenedFolder { get; set; } = "";
    public string LastSavedFile { get; set; } = "";
    public int WindowWidth { get; set; } = 1024;
    public int WindowHeight { get; set; } = 768;
    public List<string> RecentFiles { get; set; } = [];

    /// <summary>
    /// Add a file to recent files list, keeping only last 10
    /// </summary>
    public void AddRecentFile(string path)
    {
        RecentFiles.Remove(path); // Remove if exists
        RecentFiles.Insert(0, path); // Add to front
        if (RecentFiles.Count > 10)
            RecentFiles.RemoveAt(RecentFiles.Count - 1);
    }
}
