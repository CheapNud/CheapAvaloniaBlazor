using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Photino.NET;

namespace CheapAvaloniaBlazor.Services.Backends;

/// <summary>
/// GTK modal backend. Photino does not expose native window pointers on Linux, so windows
/// are located by title in the process-wide GTK toplevel list — all Photino windows live on
/// the single in-process GTK main loop. Parent disabling maps to gtk_widget_set_sensitive,
/// the closest GTK equivalent of Win32 EnableWindow.
/// Title matching happens ONLY at disable time; the matched widget pointers are remembered
/// per parent so re-enabling works even if the parent's title changes while the modal is open.
/// Known limitation: a parent and modal sharing the same title cannot be told apart — the
/// parent is left enabled (fail open) in that case.
/// </summary>
[SupportedOSPlatform("linux")]
internal sealed class LinuxModalBackend : IModalBackend
{
    // Photino truncates Linux titles to 31 chars natively; its managed setter mirrors that,
    // but normalize both sides anyway in case a future Photino version diverges.
    private const int GtkTitleMaxLength = 31;

    private readonly ILogger _logger;

    // Widget pointers disabled per parent window. Only touched on the GTK thread, so no
    // locking is needed: PhotinoWindow.Invoke runs the callback directly when already on
    // the window's thread and otherwise dispatches through Photino_Invoke onto the single
    // GTK main loop — every access is serialized on that one thread.
    // Entries are removed on enable and the map is cleared in Dispose, so a parent
    // reference is only retained while its modal is actually open.
    private readonly Dictionary<PhotinoWindow, List<IntPtr>> _disabledByParent = [];

    public bool IsSupported => OperatingSystem.IsLinux();

    public LinuxModalBackend(ILogger logger)
    {
        // [SupportedOSPlatform] is analyzer-only — enforce at runtime too.
        if (!OperatingSystem.IsLinux())
            throw new PlatformNotSupportedException("LinuxModalBackend requires Linux.");

        _logger = logger;
    }

    public void DisableParentWindow(ModalWindowRef parent, ModalWindowRef modal)
    {
        var parentWindow = parent.Window;
        if (parentWindow is null) return;
        var modalWindow = modal.Window;

        // All GTK calls (and the title reads) run on the GTK main loop; PhotinoWindow.Invoke
        // executes synchronously on it, so titles cannot go stale between read and apply.
        parentWindow.Invoke(() =>
        {
            try
            {
                var parentTitle = NormalizeTitle(parentWindow.Title);
                var modalTitle = NormalizeTitle(modalWindow?.Title);

                if (parentTitle == modalTitle)
                {
                    // Titles are the only way to tell Photino's GTK toplevels apart. With
                    // identical titles the parent cannot be distinguished from the modal, so
                    // fail open (parent stays interactive) rather than risk freezing the modal.
                    _logger.LogWarning("Modal and parent share the title '{Title}' — parent will not be disabled. " +
                        "Give the modal window a distinct title to get modal behavior on Linux.", parentTitle);
                    return;
                }

                var matches = FindToplevelsByTitle(parentTitle);
                if (matches.Count == 0)
                {
                    _logger.LogWarning("No GTK toplevel titled '{Title}' found to disable", parentTitle);
                    return;
                }

                foreach (var widget in matches)
                {
                    gtk_widget_set_sensitive(widget, false);
                }

                // Remember exactly what was disabled — EnableParentWindow re-enables these
                // pointers instead of re-matching by (possibly changed) title.
                _disabledByParent[parentWindow] = matches;
                _logger.LogDebug("Disabled {Count} GTK toplevel(s) titled '{Title}' for modal", matches.Count, parentTitle);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GTK parent disable failed");
            }
        });
    }

    public void EnableParentWindow(ModalWindowRef parent)
    {
        var parentWindow = parent.Window;
        if (parentWindow is null) return;

        parentWindow.Invoke(() =>
        {
            try
            {
                if (!_disabledByParent.Remove(parentWindow, out var disabledWidgets))
                    return; // nothing was disabled for this parent (e.g. title-collision fail-open)

                // Guard against stale pointers: the parent could have been destroyed via the
                // window manager while disabled. Only touch widgets that are still toplevels.
                var liveToplevels = ListToplevels();
                var enabledCount = 0;
                foreach (var widget in disabledWidgets)
                {
                    if (liveToplevels.Contains(widget))
                    {
                        gtk_widget_set_sensitive(widget, true);
                        enabledCount++;
                    }
                }

                _logger.LogDebug("Re-enabled {Count} GTK toplevel(s) after modal", enabledCount);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GTK parent enable failed");
            }
        });
    }

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

    public void Dispose()
    {
        _disabledByParent.Clear();
    }

    private static string? NormalizeTitle(string? title)
        => title is { Length: > GtkTitleMaxLength } ? title[..GtkTitleMaxLength] : title;

    private static List<IntPtr> FindToplevelsByTitle(string? title)
    {
        var matches = new List<IntPtr>();
        foreach (var widget in ListToplevels())
        {
            var widgetTitle = NormalizeTitle(Marshal.PtrToStringUTF8(gtk_window_get_title(widget)));
            if (widgetTitle == title)
                matches.Add(widget);
        }

        return matches;
    }

    private static List<IntPtr> ListToplevels()
    {
        var widgets = new List<IntPtr>();
        var toplevels = gtk_window_list_toplevels();
        try
        {
            for (var node = toplevels; node != IntPtr.Zero;)
            {
                var entry = Marshal.PtrToStructure<GListNode>(node);
                if (entry.Data != IntPtr.Zero)
                    widgets.Add(entry.Data);
                node = entry.Next;
            }
        }
        finally
        {
            if (toplevels != IntPtr.Zero)
                g_list_free(toplevels);
        }

        return widgets;
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
