using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using CheapAvaloniaBlazor.Models;
using Microsoft.Extensions.Logging;
using Photino.NET;

namespace CheapAvaloniaBlazor.Services.Backends;

/// <summary>
/// Linux native menu bar backend using in-process GTK 3 interop.
/// Photino's GtkWindow holds the WebKit webview as its only child, so this backend
/// reparents the webview into a vertical GtkBox with a gtk_menu_bar packed above it.
/// All GTK calls are marshaled to the GTK main loop via PhotinoWindow.Invoke.
/// Display-only accelerator text (MenuItemDefinition.Accelerator) is not rendered —
/// GTK reserves that column for real accelerators, and actual key binding is
/// IHotkeyService's job anyway.
/// </summary>
[SupportedOSPlatform("linux")]
internal sealed class GtkMenuBarBackend : IMenuBarBackend
{
    private readonly ILogger _logger;

    private PhotinoWindow? _window;
    private IntPtr _gtkWindow;
    private IntPtr _contentBox;
    private IntPtr _menuBar;

    // CRITICAL: The activate delegate must not be collected by GC while GTK signals reference
    // its function pointer. Field reference alone can be accidentally removed during
    // refactoring, so we also pin with GCHandle to make the intent explicit.
    private ActivateCallback? _activateDelegate;
    private GCHandle _activateDelegateHandle;
    private IntPtr _activateFunctionPtr;

    private readonly Dictionary<IntPtr, MenuItemDefinition> _widgetToDefinition = [];
    private readonly Dictionary<string, IntPtr> _stringIdToWidget = [];
    private readonly List<(IntPtr Widget, nuint HandlerId)> _signalConnections = [];

    private int _disposed; // 0 = not disposed, 1 = disposed (Interlocked)
    private bool _suppressActivate;

    public bool IsSupported => OperatingSystem.IsLinux();

    public event Action<string>? MenuItemClicked;

    /// <summary>
    /// Fired when an async menu item callback throws. Allows the orchestrator to surface errors
    /// that would otherwise be silently swallowed in fire-and-forget from the GTK thread.
    /// </summary>
    internal event Action<Exception>? AsyncExceptionOccurred;

    public GtkMenuBarBackend(ILogger logger)
    {
        // [SupportedOSPlatform] is analyzer-only — enforce at runtime too.
        if (!OperatingSystem.IsLinux())
            throw new PlatformNotSupportedException("GtkMenuBarBackend requires Linux.");

        _logger = logger;
    }

    public void Initialize(PhotinoWindow window, IEnumerable<MenuItemDefinition> menus)
    {
        if (Volatile.Read(ref _disposed) != 0) return;

        _window = window;
        _activateDelegate = OnMenuItemActivated;
        _activateDelegateHandle = GCHandle.Alloc(_activateDelegate);
        _activateFunctionPtr = Marshal.GetFunctionPointerForDelegate(_activateDelegate);

        // PhotinoWindow.Invoke executes synchronously on the GTK main loop.
        window.Invoke(() =>
        {
            try
            {
                _gtkWindow = FindGtkToplevel(window);
                if (_gtkWindow == IntPtr.Zero)
                {
                    _logger.LogWarning("GtkMenuBarBackend: could not locate the GTK toplevel window — menu bar not attached");
                    return;
                }

                var webview = gtk_bin_get_child(_gtkWindow);
                if (webview == IntPtr.Zero)
                {
                    _logger.LogWarning("GtkMenuBarBackend: GTK window has no child widget — menu bar not attached");
                    return;
                }

                // Photino adds the WebKit webview directly to the window; log the actual child
                // type so a future Photino layout change is diagnosable from logs.
                _logger.LogDebug("GtkMenuBarBackend: reparenting window child of type {GType}",
                    Marshal.PtrToStringUTF8(g_type_name_from_instance(webview)) ?? "<unknown>");

                _menuBar = gtk_menu_bar_new();
                BuildMenuShell(_menuBar, menus);

                // Reparent: the webview is the window's direct child; move it into a vertical
                // box under the menu bar. The ref keeps it alive across the remove.
                _contentBox = gtk_box_new(GtkOrientationVertical, 0);
                g_object_ref(webview);
                gtk_container_remove(_gtkWindow, webview);
                gtk_box_pack_start(_contentBox, _menuBar, false, false, 0);
                gtk_box_pack_start(_contentBox, webview, true, true, 0);
                gtk_container_add(_gtkWindow, _contentBox);
                g_object_unref(webview);
                gtk_widget_show_all(_contentBox);

                _logger.LogDebug("GtkMenuBarBackend: menu bar attached to GTK window {Handle}", _gtkWindow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GtkMenuBarBackend: failed to attach the menu bar");
            }
        });
    }

    public void SetMenuBar(IEnumerable<MenuItemDefinition> menus)
    {
        if (Volatile.Read(ref _disposed) != 0) return;
        if (_window is null || _contentBox == IntPtr.Zero)
        {
            _logger.LogDebug("GtkMenuBarBackend: SetMenuBar skipped — menu bar was never attached (Initialize failed or not called)");
            return;
        }

        _window.Invoke(() =>
        {
            try
            {
                DisconnectSignals();
                _widgetToDefinition.Clear();
                _stringIdToWidget.Clear();

                if (_menuBar != IntPtr.Zero)
                {
                    // Destroys the bar and every attached item/submenu recursively.
                    gtk_widget_destroy(_menuBar);
                }

                _menuBar = gtk_menu_bar_new();
                BuildMenuShell(_menuBar, menus);
                gtk_box_pack_start(_contentBox, _menuBar, false, false, 0);
                gtk_box_reorder_child(_contentBox, _menuBar, 0);
                gtk_widget_show_all(_menuBar);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GtkMenuBarBackend: failed to replace the menu bar");
            }
        });
    }

    public void EnableMenuItem(string menuItemId, bool enabled)
    {
        if (Volatile.Read(ref _disposed) != 0) return;
        if (_window is null) return;
        if (!_stringIdToWidget.TryGetValue(menuItemId, out var widget)) return;

        _window.Invoke(() =>
        {
            gtk_widget_set_sensitive(widget, enabled);

            if (_widgetToDefinition.TryGetValue(widget, out var definition))
                definition.IsEnabled = enabled;
        });
    }

    public void CheckMenuItem(string menuItemId, bool isChecked)
    {
        if (Volatile.Read(ref _disposed) != 0) return;
        if (_window is null) return;
        if (!_stringIdToWidget.TryGetValue(menuItemId, out var widget)) return;

        _window.Invoke(() =>
        {
            // set_active emits "activate" — suppress so a programmatic toggle doesn't
            // fire the item's click handlers. Safe without locking: both this lambda
            // and the activate handler run on the GTK thread.
            _suppressActivate = true;
            try
            {
                gtk_check_menu_item_set_active(widget, isChecked);
            }
            finally
            {
                _suppressActivate = false;
            }

            if (_widgetToDefinition.TryGetValue(widget, out var definition))
                definition.IsChecked = isChecked;
        });
    }

    public void Dispose()
    {
        // Atomic check-and-set: only one thread enters disposal
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0) return;

        var disconnected = false;
        try
        {
            // ORDER MATTERS: disconnect the signal handlers on the GTK thread FIRST, while
            // the delegate is still alive — a pending click after the GCHandle is freed
            // would call through a collected delegate and access-violate.
            _window?.Invoke(DisconnectSignals);
            disconnected = true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "GtkMenuBarBackend: could not disconnect menu signals during dispose");
        }

        // If disconnection failed (GTK loop already gone at shutdown), deliberately leak the
        // delegate — the process is exiting and a dangling GTK signal must never observe a
        // freed function pointer.
        if (disconnected && _activateDelegateHandle.IsAllocated)
        {
            _activateDelegateHandle.Free();
            _activateDelegate = null;
        }

        _widgetToDefinition.Clear();
        _stringIdToWidget.Clear();
        _window = null;
    }

    // ── Menu construction ────────────────────────────────────────────────────

    private void BuildMenuShell(IntPtr menuShell, IEnumerable<MenuItemDefinition> menus)
    {
        foreach (var definition in menus)
        {
            var item = CreateMenuItem(definition);
            gtk_menu_shell_append(menuShell, item);
        }
    }

    private IntPtr CreateMenuItem(MenuItemDefinition definition)
    {
        if (definition.IsSeparator)
            return gtk_separator_menu_item_new();

        IntPtr item;
        var label = ConvertMnemonics(definition.Text);

        if (definition.IsCheckable)
        {
            item = gtk_check_menu_item_new_with_mnemonic(label);
            // Set the initial state before the signal is connected so it doesn't fire.
            gtk_check_menu_item_set_active(item, definition.IsChecked);
        }
        else
        {
            item = gtk_menu_item_new_with_mnemonic(label);
        }

        if (definition.SubItems is { Count: > 0 })
        {
            var submenu = gtk_menu_new();
            BuildMenuShell(submenu, definition.SubItems);
            gtk_menu_item_set_submenu(item, submenu);
        }
        else
        {
            // Leaf item — connect the click signal. Submenu parents also emit "activate"
            // when they open, which must not trigger handlers.
            var handlerId = g_signal_connect_data(item, "activate", _activateFunctionPtr,
                IntPtr.Zero, IntPtr.Zero, 0);
            _signalConnections.Add((item, handlerId));
        }

        if (!definition.IsEnabled)
            gtk_widget_set_sensitive(item, false);

        _widgetToDefinition[item] = definition;
        if (!string.IsNullOrEmpty(definition.Id))
            _stringIdToWidget[definition.Id] = item;

        return item;
    }

    /// <summary>
    /// Win32 mnemonics use '&amp;' ("&amp;&amp;" = literal ampersand); GTK uses '_'
    /// ("__" = literal underscore). Definitions are written Win32-style.
    /// Internal for unit testing (platform-independent string logic).
    /// </summary>
    internal static string ConvertMnemonics(string text)
    {
        var converted = text.Replace("_", "__");
        converted = converted.Replace("&&", "\0");
        converted = converted.Replace("&", "_");
        return converted.Replace("\0", "&");
    }

    // Photino truncates Linux titles to 31 chars natively; its managed setter mirrors that,
    // but normalize both comparison sides anyway in case a future Photino version diverges.
    private const int GtkTitleMaxLength = 31;

    private static string? NormalizeTitle(string? title)
        => title is { Length: > GtkTitleMaxLength } ? title[..GtkTitleMaxLength] : title;

    /// <summary>
    /// Locates the Photino GtkWindow. Photino exposes no native pointers on Linux, so when
    /// several toplevels exist they are told apart by (31-char-normalized) title.
    /// </summary>
    private IntPtr FindGtkToplevel(PhotinoWindow window)
    {
        var candidates = new List<IntPtr>();
        var toplevels = gtk_window_list_toplevels();
        try
        {
            for (var node = toplevels; node != IntPtr.Zero;)
            {
                var entry = Marshal.PtrToStructure<GListNode>(node);
                if (entry.Data != IntPtr.Zero)
                    candidates.Add(entry.Data);
                node = entry.Next;
            }
        }
        finally
        {
            if (toplevels != IntPtr.Zero)
                g_list_free(toplevels);
        }

        if (candidates.Count == 1)
            return candidates[0];

        var title = NormalizeTitle(window.Title);
        foreach (var candidate in candidates)
        {
            if (NormalizeTitle(Marshal.PtrToStringUTF8(gtk_window_get_title(candidate))) == title)
                return candidate;
        }

        return IntPtr.Zero;
    }

    // ── Click dispatch (runs on the GTK thread) ──────────────────────────────

    private void OnMenuItemActivated(IntPtr widget, IntPtr userData)
    {
        if (_suppressActivate) return;
        if (Volatile.Read(ref _disposed) != 0) return;
        if (!_widgetToDefinition.TryGetValue(widget, out var definition)) return;

        // Sync the managed checked state with what GTK just toggled.
        if (definition.IsCheckable)
            definition.IsChecked = gtk_check_menu_item_get_active(widget);

        if (definition.OnClick is not null)
        {
            try
            {
                definition.OnClick();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Menu item OnClick threw for '{Text}'", definition.Text);
            }
        }

        // Fire-and-forget since the GTK signal handler is synchronous.
        if (definition.OnClickAsync is not null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await definition.OnClickAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Menu item OnClickAsync threw for '{Text}'", definition.Text);
                    AsyncExceptionOccurred?.Invoke(ex);
                }
            });
        }

        if (!string.IsNullOrEmpty(definition.Id))
        {
            try
            {
                MenuItemClicked?.Invoke(definition.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MenuItemClicked event handler threw for '{Id}'", definition.Id);
            }
        }
    }

    private void DisconnectSignals()
    {
        foreach (var (widget, handlerId) in _signalConnections)
        {
            g_signal_handler_disconnect(widget, handlerId);
        }
        _signalConnections.Clear();
    }

    // ── GTK P/Invoke ─────────────────────────────────────────────────────────

    private const int GtkOrientationVertical = 1;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ActivateCallback(IntPtr widget, IntPtr userData);

    [StructLayout(LayoutKind.Sequential)]
    private struct GListNode
    {
        public IntPtr Data;
        public IntPtr Next;
        public IntPtr Prev;
    }

    /// <summary>
    /// Returns a newly allocated GList whose elements are borrowed GtkWindow pointers —
    /// free the list with g_list_free, never the elements.
    /// </summary>
    [DllImport("libgtk-3.so.0")]
    private static extern IntPtr gtk_window_list_toplevels();

    [DllImport("libgtk-3.so.0")]
    private static extern IntPtr gtk_window_get_title(IntPtr window);

    [DllImport("libgtk-3.so.0")]
    private static extern IntPtr gtk_bin_get_child(IntPtr bin);

    [DllImport("libgtk-3.so.0")]
    private static extern IntPtr gtk_box_new(int orientation, int spacing);

    [DllImport("libgtk-3.so.0")]
    private static extern void gtk_box_pack_start(IntPtr box, IntPtr child,
        [MarshalAs(UnmanagedType.Bool)] bool expand, [MarshalAs(UnmanagedType.Bool)] bool fill, uint padding);

    [DllImport("libgtk-3.so.0")]
    private static extern void gtk_box_reorder_child(IntPtr box, IntPtr child, int position);

    [DllImport("libgtk-3.so.0")]
    private static extern void gtk_container_add(IntPtr container, IntPtr widget);

    [DllImport("libgtk-3.so.0")]
    private static extern void gtk_container_remove(IntPtr container, IntPtr widget);

    [DllImport("libgtk-3.so.0")]
    private static extern IntPtr gtk_menu_bar_new();

    [DllImport("libgtk-3.so.0")]
    private static extern IntPtr gtk_menu_new();

    [DllImport("libgtk-3.so.0")]
    private static extern IntPtr gtk_menu_item_new_with_mnemonic([MarshalAs(UnmanagedType.LPUTF8Str)] string label);

    [DllImport("libgtk-3.so.0")]
    private static extern IntPtr gtk_check_menu_item_new_with_mnemonic([MarshalAs(UnmanagedType.LPUTF8Str)] string label);

    [DllImport("libgtk-3.so.0")]
    private static extern IntPtr gtk_separator_menu_item_new();

    [DllImport("libgtk-3.so.0")]
    private static extern void gtk_menu_item_set_submenu(IntPtr menuItem, IntPtr submenu);

    [DllImport("libgtk-3.so.0")]
    private static extern void gtk_menu_shell_append(IntPtr menuShell, IntPtr child);

    [DllImport("libgtk-3.so.0")]
    private static extern void gtk_check_menu_item_set_active(IntPtr checkMenuItem,
        [MarshalAs(UnmanagedType.Bool)] bool isActive);

    [DllImport("libgtk-3.so.0")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool gtk_check_menu_item_get_active(IntPtr checkMenuItem);

    [DllImport("libgtk-3.so.0")]
    private static extern void gtk_widget_set_sensitive(IntPtr widget, [MarshalAs(UnmanagedType.Bool)] bool sensitive);

    [DllImport("libgtk-3.so.0")]
    private static extern void gtk_widget_show_all(IntPtr widget);

    [DllImport("libgtk-3.so.0")]
    private static extern void gtk_widget_destroy(IntPtr widget);

    [DllImport("libgobject-2.0.so.0")]
    private static extern void g_object_ref(IntPtr instance);

    [DllImport("libgobject-2.0.so.0")]
    private static extern void g_object_unref(IntPtr instance);

    // gulong is pointer-sized on both LP64 (64-bit) and ILP32 (32-bit ARM) Linux,
    // so nuint is the correct marshaling type — ulong would misread on 32-bit.
    [DllImport("libgobject-2.0.so.0")]
    private static extern nuint g_signal_connect_data(IntPtr instance,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string detailedSignal,
        IntPtr handler, IntPtr data, IntPtr destroyData, int connectFlags);

    [DllImport("libgobject-2.0.so.0")]
    private static extern void g_signal_handler_disconnect(IntPtr instance, nuint handlerId);

    /// <summary>
    /// Returns the type name of a GObject instance (borrowed string, do not free).
    /// </summary>
    [DllImport("libgobject-2.0.so.0")]
    private static extern IntPtr g_type_name_from_instance(IntPtr instance);

    [DllImport("libglib-2.0.so.0")]
    private static extern void g_list_free(IntPtr list);
}
