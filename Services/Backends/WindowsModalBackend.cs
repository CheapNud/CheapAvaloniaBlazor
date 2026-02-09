using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

namespace CheapAvaloniaBlazor.Services.Backends;

/// <summary>
/// Win32 modal backend that uses EnableWindow to disable/enable the parent window
/// and PostMessage for thread-safe window close operations.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class WindowsModalBackend : IModalBackend
{
    private readonly ILogger _logger;

    public bool IsSupported => true;

    public WindowsModalBackend(ILogger logger)
    {
        _logger = logger;
    }

    public void DisableParentWindow(IntPtr parentHandle)
    {
        if (parentHandle == IntPtr.Zero) return;

        if (!IsWindow(parentHandle))
        {
            _logger.LogWarning("Cannot disable parent window — handle {Handle} is not a valid window", parentHandle);
            return;
        }

        EnableWindow(parentHandle, false);
        _logger.LogDebug("Disabled parent window {Handle} for modal", parentHandle);
    }

    public void EnableParentWindow(IntPtr parentHandle)
    {
        if (parentHandle == IntPtr.Zero) return;

        if (!IsWindow(parentHandle))
        {
            _logger.LogWarning("Cannot enable parent window — handle {Handle} is not a valid window", parentHandle);
            return;
        }

        EnableWindow(parentHandle, true);

        // Bring the parent to the foreground after modal closes
        SetForegroundWindow(parentHandle);
        _logger.LogDebug("Re-enabled parent window {Handle} after modal", parentHandle);
    }

    public void PostCloseMessage(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero) return;

        if (!IsWindow(windowHandle))
        {
            _logger.LogWarning("Cannot close window — handle {Handle} is not a valid window", windowHandle);
            return;
        }

        PostMessage(windowHandle, Constants.Window.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
        _logger.LogDebug("Posted WM_CLOSE to window {Handle}", windowHandle);
    }

    public void Dispose() { }

    // ── P/Invoke ─────────────────────────────────────────────────────────────

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnableWindow(IntPtr hWnd, [MarshalAs(UnmanagedType.Bool)] bool bEnable);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
}
