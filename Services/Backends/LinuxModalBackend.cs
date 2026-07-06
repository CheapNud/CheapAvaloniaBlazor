using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

namespace CheapAvaloniaBlazor.Services.Backends;

/// <summary>
/// GTK modal backend. Photino does not expose native window pointers on Linux, so windows
/// are located by title in the process-wide GTK toplevel list — all Photino windows live on
/// the single in-process GTK main loop. Parent disabling maps to gtk_widget_set_sensitive,
/// the closest GTK equivalent of Win32 EnableWindow.
/// </summary>
[SupportedOSPlatform("linux")]
internal sealed class LinuxModalBackend : IModalBackend
{
    private readonly ILogger _logger;

    public bool IsSupported => true;

    public LinuxModalBackend(ILogger logger)
    {
        // [SupportedOSPlatform] is analyzer-only — enforce at runtime too.
        if (!OperatingSystem.IsLinux())
            throw new PlatformNotSupportedException("LinuxModalBackend requires Linux.");

        _logger = logger;
    }

    public void DisableParentWindow(ModalWindowRef parent, ModalWindowRef modal)
        => SetParentSensitivity(parent, modal, sensitive: false);

    public void EnableParentWindow(ModalWindowRef parent)
        => SetParentSensitivity(parent, modal: default, sensitive: true);

    public void PostCloseMessage(ModalWindowRef window)
    {
        if (window.Window is null) return;

        try
        {
            // PhotinoWindow.Close marshals to the GTK main loop via Invoke — safe from any thread.
            window.Window.Close();
        }
        catch (Exception ex)
        {
            // Close throws if the native window is not initialized yet or already destroyed.
            _logger.LogWarning(ex, "Failed to close window via PhotinoWindow.Close");
        }
    }

    public void Dispose() { }

    private void SetParentSensitivity(ModalWindowRef parent, ModalWindowRef modal, bool sensitive)
    {
        var parentWindow = parent.Window;
        if (parentWindow is null) return;

        string parentTitle;
        string? modalTitle;
        try
        {
            // Photino truncates Linux titles to 31 chars on the managed side too,
            // so the managed title always equals the GTK title.
            parentTitle = parentWindow.Title;
            modalTitle = modal.Window?.Title;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read window titles for modal sensitivity change");
            return;
        }

        if (!sensitive && parentTitle == modalTitle)
        {
            // Titles are the only way to tell Photino's GTK toplevels apart. With identical
            // titles the parent cannot be distinguished from the modal, so fail open (parent
            // stays interactive) rather than risk freezing the modal itself.
            _logger.LogWarning("Modal and parent share the title '{Title}' — parent will not be disabled. " +
                "Give the modal window a distinct title to get modal behavior on Linux.", parentTitle);
            return;
        }

        // All GTK calls must run on the GTK main loop; PhotinoWindow.Invoke schedules onto it.
        parentWindow.Invoke(() =>
        {
            try
            {
                var matches = ApplySensitivityByTitle(parentTitle, sensitive);
                if (matches == 0)
                {
                    _logger.LogWarning("No GTK toplevel titled '{Title}' found to {Action}",
                        parentTitle, sensitive ? "enable" : "disable");
                }
                else
                {
                    _logger.LogDebug("{Action} {Count} GTK toplevel(s) titled '{Title}' for modal",
                        sensitive ? "Enabled" : "Disabled", matches, parentTitle);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GTK sensitivity change failed for '{Title}'", parentTitle);
            }
        });
    }

    private static int ApplySensitivityByTitle(string title, bool sensitive)
    {
        var matchCount = 0;
        var toplevels = gtk_window_list_toplevels();
        try
        {
            for (var node = toplevels; node != IntPtr.Zero;)
            {
                var entry = Marshal.PtrToStructure<GListNode>(node);
                if (entry.Data != IntPtr.Zero)
                {
                    var widgetTitle = Marshal.PtrToStringUTF8(gtk_window_get_title(entry.Data));
                    if (widgetTitle == title)
                    {
                        gtk_widget_set_sensitive(entry.Data, sensitive);
                        matchCount++;
                    }
                }
                node = entry.Next;
            }
        }
        finally
        {
            if (toplevels != IntPtr.Zero)
                g_list_free(toplevels);
        }

        return matchCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct GListNode
    {
        public IntPtr Data;
        public IntPtr Next;
        public IntPtr Prev;
    }

    // ── P/Invoke ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a newly allocated GList whose elements are borrowed GtkWindow pointers —
    /// free the list with g_list_free, never the elements.
    /// </summary>
    [DllImport("libgtk-3.so.0")]
    private static extern IntPtr gtk_window_list_toplevels();

    /// <summary>
    /// Returns a pointer to the window's internal UTF-8 title (borrowed, do not free),
    /// or NULL when the window has no title.
    /// </summary>
    [DllImport("libgtk-3.so.0")]
    private static extern IntPtr gtk_window_get_title(IntPtr window);

    [DllImport("libgtk-3.so.0")]
    private static extern void gtk_widget_set_sensitive(IntPtr widget, [MarshalAs(UnmanagedType.Bool)] bool sensitive);

    [DllImport("libglib-2.0.so.0")]
    private static extern void g_list_free(IntPtr list);
}
