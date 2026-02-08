using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia.Input;
using CheapAvaloniaBlazor.Models;
using CheapAvaloniaBlazor.Utilities;
using Microsoft.Extensions.Logging;

namespace CheapAvaloniaBlazor.Services;

/// <summary>
/// Windows implementation of global hotkeys using Win32 RegisterHotKey/UnregisterHotKey.
/// A dedicated background thread runs a GetMessage loop to receive WM_HOTKEY messages.
/// On non-Windows platforms, IsSupported returns false and RegisterHotkey throws.
/// </summary>
public sealed class HotkeyService : IHotkeyService, IDisposable
{
    private readonly ILogger<HotkeyService> _logger;
    private readonly ConcurrentDictionary<int, HotkeyRegistration> _registrations = new();
    private readonly ConcurrentQueue<HotkeyCommand> _commandQueue = new();

    private Thread? _messageThread;
    private int _messageThreadId;
    private volatile bool _disposed;
    private int _nextId;

    public bool IsSupported => OperatingSystem.IsWindows();

    public event Action<int>? HotkeyPressed;

    public HotkeyService(ILogger<HotkeyService> logger)
    {
        _logger = logger;
    }

    public int RegisterHotkey(HotkeyModifiers modifiers, Key key, Action callback)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Global hotkeys are only supported on Windows.");

        var virtualKey = KeyMapper.ToVirtualKey(key);
        if (virtualKey is null)
            throw new ArgumentException($"Key '{key}' cannot be mapped to a Win32 virtual key code.", nameof(key));

        var hotkeyId = Interlocked.Increment(ref _nextId);
        var registration = new HotkeyRegistration(hotkeyId, modifiers, key, callback);

        EnsureMessageThread();

        // Queue the register command and wake the message thread
        var completionSource = new TaskCompletionSource<bool>();
        _commandQueue.Enqueue(new HotkeyCommand(HotkeyCommandType.Register, hotkeyId, modifiers, virtualKey.Value, completionSource));
        WakeMessageThread();

        // Block until the message thread processes the registration
        var registered = completionSource.Task.GetAwaiter().GetResult();
        if (!registered)
        {
            var errorCode = Marshal.GetLastWin32Error();
            throw new InvalidOperationException(
                $"Failed to register hotkey {modifiers}+{key} (Win32 error {errorCode}). " +
                "The key combination may already be registered by another application.");
        }

        _registrations[hotkeyId] = registration;
        _logger.LogInformation("Registered global hotkey: {Modifiers}+{Key} (ID={Id})", modifiers, key, hotkeyId);
        return hotkeyId;
    }

    public bool UnregisterHotkey(int hotkeyId)
    {
        if (_disposed) return false;
        if (!OperatingSystem.IsWindows()) return false;
        if (!_registrations.TryRemove(hotkeyId, out var registration)) return false;

        if (_messageThread?.IsAlive == true)
        {
            var completionSource = new TaskCompletionSource<bool>();
            _commandQueue.Enqueue(new HotkeyCommand(HotkeyCommandType.Unregister, hotkeyId, HotkeyModifiers.None, 0, completionSource));
            WakeMessageThread();
            completionSource.Task.GetAwaiter().GetResult();
        }

        _logger.LogInformation("Unregistered global hotkey: {Modifiers}+{Key} (ID={Id})", registration.Modifiers, registration.Key, hotkeyId);
        return true;
    }

    public void UnregisterAll()
    {
        if (_disposed) return;
        if (!OperatingSystem.IsWindows()) return;

        var ids = _registrations.Keys.ToArray();
        foreach (var hotkeyId in ids)
        {
            UnregisterHotkey(hotkeyId);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (OperatingSystem.IsWindows())
        {
            UnregisterAll();
            StopMessageThread();
        }
    }

    [SupportedOSPlatform("windows")]
    private void EnsureMessageThread()
    {
        if (_messageThread?.IsAlive == true) return;

        var readySignal = new ManualResetEventSlim(false);

        _messageThread = new Thread(() => MessagePumpLoop(readySignal))
        {
            IsBackground = true,
            Name = "CheapAvaloniaBlazor.HotkeyMessagePump"
        };
        _messageThread.Start();

        // Wait for the thread to be ready (message queue created)
        readySignal.Wait(TimeSpan.FromSeconds(5));
        readySignal.Dispose();
    }

    [SupportedOSPlatform("windows")]
    private void MessagePumpLoop(ManualResetEventSlim readySignal)
    {
        _messageThreadId = GetCurrentThreadId();

        // Force message queue creation by peeking
        PeekMessage(out _, IntPtr.Zero, 0, 0, 0);
        readySignal.Set();

        _logger.LogDebug("Hotkey message pump started on thread {ThreadId}", _messageThreadId);

        while (GetMessage(out var msg, IntPtr.Zero, 0, 0))
        {
            if (msg.message == WM_APP)
            {
                // Process queued commands
                ProcessCommands();
            }
            else if (msg.message == WM_HOTKEY)
            {
                var hotkeyId = (int)msg.wParam;
                if (_registrations.TryGetValue(hotkeyId, out var registration))
                {
                    try
                    {
                        registration.Callback();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Hotkey callback threw for ID={Id}", hotkeyId);
                    }

                    try
                    {
                        HotkeyPressed?.Invoke(hotkeyId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "HotkeyPressed event handler threw for ID={Id}", hotkeyId);
                    }
                }
            }
        }

        _logger.LogDebug("Hotkey message pump exiting");
    }

    [SupportedOSPlatform("windows")]
    private void ProcessCommands()
    {
        while (_commandQueue.TryDequeue(out var command))
        {
            switch (command.CommandType)
            {
                case HotkeyCommandType.Register:
                    var registered = RegisterHotKeyNative(IntPtr.Zero, command.HotkeyId, (uint)command.Modifiers, (uint)command.VirtualKey);
                    command.CompletionSource.SetResult(registered);
                    break;

                case HotkeyCommandType.Unregister:
                    UnregisterHotKeyNative(IntPtr.Zero, command.HotkeyId);
                    command.CompletionSource.SetResult(true);
                    break;
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private void WakeMessageThread()
    {
        if (_messageThreadId != 0)
        {
            PostThreadMessage(_messageThreadId, WM_APP, IntPtr.Zero, IntPtr.Zero);
        }
    }

    [SupportedOSPlatform("windows")]
    private void StopMessageThread()
    {
        if (_messageThread?.IsAlive != true) return;

        if (_messageThreadId != 0)
        {
            PostThreadMessage(_messageThreadId, WM_QUIT, IntPtr.Zero, IntPtr.Zero);
        }

        _messageThread.Join(TimeSpan.FromSeconds(3));
    }

    #region Win32 P/Invoke

    private const int WM_HOTKEY = 0x0312;
    private const int WM_QUIT = 0x0012;
    private const int WM_APP = 0x8000;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public POINT pt;
    }

    [DllImport("user32.dll", SetLastError = true, EntryPoint = "RegisterHotKey")]
    [SupportedOSPlatform("windows")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKeyNative(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true, EntryPoint = "UnregisterHotKey")]
    [SupportedOSPlatform("windows")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKeyNative(IntPtr hWnd, int id);

    [DllImport("user32.dll", SetLastError = true)]
    [SupportedOSPlatform("windows")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    [SupportedOSPlatform("windows")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PeekMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

    [DllImport("user32.dll", SetLastError = true)]
    [SupportedOSPlatform("windows")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostThreadMessage(int idThread, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    [SupportedOSPlatform("windows")]
    private static extern int GetCurrentThreadId();

    #endregion

    #region Command types

    private enum HotkeyCommandType { Register, Unregister }

    private sealed record HotkeyCommand(
        HotkeyCommandType CommandType,
        int HotkeyId,
        HotkeyModifiers Modifiers,
        int VirtualKey,
        TaskCompletionSource<bool> CompletionSource);

    #endregion
}
