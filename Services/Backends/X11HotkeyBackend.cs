using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia.Input;
using CheapAvaloniaBlazor.Models;
using CheapAvaloniaBlazor.Utilities;
using Microsoft.Extensions.Logging;

namespace CheapAvaloniaBlazor.Services.Backends;

/// <summary>
/// X11 global hotkey backend using XGrabKey/XUngrabKey.
/// Works on X11 sessions and XWayland. Uses a polling thread with XPending + XNextEvent.
/// Registers each hotkey with 4 modifier variants to handle NumLock/CapsLock state.
/// </summary>
[SupportedOSPlatform("linux")]
internal sealed class X11HotkeyBackend : IHotkeyBackend
{
    private readonly ILogger _logger;
    private readonly ConcurrentQueue<X11Command> _commandQueue = new();

    // (keycode, cleanModifiers) → hotkeyId for event matching
    private readonly Dictionary<(int keycode, uint modifiers), int> _grabMap = [];
    // hotkeyId → (keycode, modifiers) for ungrab
    private readonly Dictionary<int, (int keycode, uint modifiers)> _registrationMap = [];

    private IntPtr _display;
    private IntPtr _rootWindow;
    private IntPtr _eventBuffer;
    private Thread? _pollThread;
    private volatile bool _stopping;
    private volatile bool _disposed;

    public bool IsSupported { get; }

    public event Action<int>? HotkeyPressed;

    // X11 modifier masks
    private const uint ShiftMask = 1;
    private const uint LockMask = 2;       // CapsLock
    private const uint ControlMask = 4;
    private const uint Mod1Mask = 8;        // Alt
    private const uint Mod2Mask = 16;       // NumLock
    private const uint Mod4Mask = 64;       // Super/Win
    private const int KeyPress = 2;

    // Lock modifier combinations for grab variants
    private static readonly uint[] LockVariants = [0, LockMask, Mod2Mask, LockMask | Mod2Mask];

    public X11HotkeyBackend(ILogger logger)
    {
        _logger = logger;

        if (!OperatingSystem.IsLinux())
        {
            IsSupported = false;
            return;
        }

        try
        {
            _display = XOpenDisplay(IntPtr.Zero);
            if (_display == IntPtr.Zero)
            {
                _logger.LogDebug("X11: XOpenDisplay failed — no X server available");
                IsSupported = false;
                return;
            }

            _rootWindow = XDefaultRootWindow(_display);
            IsSupported = true;
            _logger.LogDebug("X11 hotkey backend initialized (display={Display}, root={Root})", _display, _rootWindow);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "X11 hotkey backend initialization failed");
            IsSupported = false;
        }
    }

    public bool Register(int hotkeyId, HotkeyModifiers modifiers, Key key)
    {
        if (_disposed || !IsSupported) return false;

        var keysym = KeyMapper.ToX11KeySym(key);
        if (keysym is null) return false;

        EnsurePollThread();

        var completionSource = new TaskCompletionSource<bool>();
        _commandQueue.Enqueue(new X11Command(X11CommandType.Register, hotkeyId, modifiers, keysym.Value, completionSource));

        return completionSource.Task.GetAwaiter().GetResult();
    }

    public bool Unregister(int hotkeyId)
    {
        if (_disposed || !IsSupported) return false;

        var completionSource = new TaskCompletionSource<bool>();
        _commandQueue.Enqueue(new X11Command(X11CommandType.Unregister, hotkeyId, HotkeyModifiers.None, 0, completionSource));

        return completionSource.Task.GetAwaiter().GetResult();
    }

    public void UnregisterAll()
    {
        if (_disposed || !IsSupported) return;

        var ids = _registrationMap.Keys.ToArray();
        foreach (var hotkeyId in ids)
            Unregister(hotkeyId);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _stopping = true;

        _pollThread?.Join(TimeSpan.FromSeconds(3));

        // Ungrab everything directly (we're on the disposing thread, poll thread is stopped)
        if (_display != IntPtr.Zero)
        {
            foreach (var (_, (keycode, modMask)) in _registrationMap)
            {
                foreach (var lockVariant in LockVariants)
                    XUngrabKey(_display, keycode, modMask | lockVariant, _rootWindow);
            }

            XFlush(_display);
            _registrationMap.Clear();
            _grabMap.Clear();
            XCloseDisplay(_display);
            _display = IntPtr.Zero;
        }

        if (_eventBuffer != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_eventBuffer);
            _eventBuffer = IntPtr.Zero;
        }
    }

    private void EnsurePollThread()
    {
        if (_pollThread?.IsAlive == true) return;

        if (_eventBuffer == IntPtr.Zero)
            _eventBuffer = Marshal.AllocHGlobal(192); // XEvent is 192 bytes on 64-bit

        _pollThread = new Thread(PollLoop)
        {
            IsBackground = true,
            Name = "CheapAvaloniaBlazor.X11HotkeyPoll"
        };
        _pollThread.Start();
    }

    private void PollLoop()
    {
        _logger.LogDebug("X11 hotkey poll thread started");

        while (!_stopping)
        {
            // Process commands first (all X11 calls on this thread)
            ProcessCommands();

            // Process pending X11 events
            while (XPending(_display) > 0)
            {
                XNextEvent(_display, _eventBuffer);

                var eventType = Marshal.ReadInt32(_eventBuffer, 0);
                if (eventType == KeyPress)
                {
                    // XKeyEvent offsets for 64-bit: state at 80, keycode at 84
                    var rawState = (uint)Marshal.ReadInt32(_eventBuffer, 80);
                    var keycode = Marshal.ReadInt32(_eventBuffer, 84);

                    // Strip CapsLock and NumLock from the received state
                    var cleanState = rawState & ~(LockMask | Mod2Mask);

                    if (_grabMap.TryGetValue((keycode, cleanState), out var hotkeyId))
                    {
                        try
                        {
                            HotkeyPressed?.Invoke(hotkeyId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "X11 HotkeyPressed handler threw for ID={Id}", hotkeyId);
                        }
                    }
                }
            }

            Thread.Sleep(50);
        }

        _logger.LogDebug("X11 hotkey poll thread exiting");
    }

    private void ProcessCommands()
    {
        while (_commandQueue.TryDequeue(out var command))
        {
            switch (command.CommandType)
            {
                case X11CommandType.Register:
                    var registered = ExecuteGrab(command.HotkeyId, command.Modifiers, command.KeySym);
                    command.CompletionSource.SetResult(registered);
                    break;

                case X11CommandType.Unregister:
                    var unregistered = ExecuteUngrab(command.HotkeyId);
                    command.CompletionSource.SetResult(unregistered);
                    break;
            }
        }
    }

    private bool ExecuteGrab(int hotkeyId, HotkeyModifiers modifiers, long keysym)
    {
        var keycode = XKeysymToKeycode(_display, new IntPtr(keysym));
        if (keycode == 0)
        {
            _logger.LogWarning("X11: XKeysymToKeycode returned 0 for keysym 0x{KeySym:X}", keysym);
            return false;
        }

        var x11Mods = ToX11Modifiers(modifiers);

        // Register with all 4 lock-state variants
        foreach (var lockVariant in LockVariants)
        {
            XGrabKey(_display, keycode, x11Mods | lockVariant, _rootWindow, false, GrabModeAsync, GrabModeAsync);
        }

        XFlush(_display);

        _grabMap[(keycode, x11Mods)] = hotkeyId;
        _registrationMap[hotkeyId] = (keycode, x11Mods);

        _logger.LogDebug("X11: Grabbed keycode={Keycode} mods=0x{Mods:X} for hotkey ID={Id}", keycode, x11Mods, hotkeyId);
        return true;
    }

    private bool ExecuteUngrab(int hotkeyId)
    {
        if (!_registrationMap.TryGetValue(hotkeyId, out var registration))
            return false;

        var (keycode, modMask) = registration;

        foreach (var lockVariant in LockVariants)
            XUngrabKey(_display, keycode, modMask | lockVariant, _rootWindow);

        XFlush(_display);

        _grabMap.Remove((keycode, modMask));
        _registrationMap.Remove(hotkeyId);

        _logger.LogDebug("X11: Ungrabbed keycode={Keycode} mods=0x{Mods:X} for hotkey ID={Id}", keycode, modMask, hotkeyId);
        return true;
    }

    private static uint ToX11Modifiers(HotkeyModifiers modifiers)
    {
        uint x11Mods = 0;
        if (modifiers.HasFlag(HotkeyModifiers.Shift)) x11Mods |= ShiftMask;
        if (modifiers.HasFlag(HotkeyModifiers.Ctrl)) x11Mods |= ControlMask;
        if (modifiers.HasFlag(HotkeyModifiers.Alt)) x11Mods |= Mod1Mask;
        if (modifiers.HasFlag(HotkeyModifiers.Win)) x11Mods |= Mod4Mask;
        return x11Mods;
    }

    #region X11 P/Invoke

    private const int GrabModeAsync = 1;

    [DllImport("libX11.so.6")]
    [SupportedOSPlatform("linux")]
    private static extern IntPtr XOpenDisplay(IntPtr displayName);

    [DllImport("libX11.so.6")]
    [SupportedOSPlatform("linux")]
    private static extern IntPtr XDefaultRootWindow(IntPtr display);

    [DllImport("libX11.so.6")]
    [SupportedOSPlatform("linux")]
    private static extern int XGrabKey(IntPtr display, int keycode, uint modifiers, IntPtr grabWindow,
        [MarshalAs(UnmanagedType.Bool)] bool ownerEvents, int pointerMode, int keyboardMode);

    [DllImport("libX11.so.6")]
    [SupportedOSPlatform("linux")]
    private static extern int XUngrabKey(IntPtr display, int keycode, uint modifiers, IntPtr grabWindow);

    [DllImport("libX11.so.6")]
    [SupportedOSPlatform("linux")]
    private static extern int XKeysymToKeycode(IntPtr display, IntPtr keysym);

    [DllImport("libX11.so.6")]
    [SupportedOSPlatform("linux")]
    private static extern int XNextEvent(IntPtr display, IntPtr eventReturn);

    [DllImport("libX11.so.6")]
    [SupportedOSPlatform("linux")]
    private static extern int XPending(IntPtr display);

    [DllImport("libX11.so.6")]
    [SupportedOSPlatform("linux")]
    private static extern int XFlush(IntPtr display);

    [DllImport("libX11.so.6")]
    [SupportedOSPlatform("linux")]
    private static extern int XCloseDisplay(IntPtr display);

    #endregion

    #region Command types

    private enum X11CommandType { Register, Unregister }

    private sealed record X11Command(
        X11CommandType CommandType,
        int HotkeyId,
        HotkeyModifiers Modifiers,
        long KeySym,
        TaskCompletionSource<bool> CompletionSource);

    #endregion
}
