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
        // [SupportedOSPlatform] is analyzer-only — enforce at runtime too.
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("WindowsModalBackend requires Windows.");

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

        if (!EnableWindow(parentHandle, false))
        {
            var errorCode = Marshal.GetLastWin32Error();
            _logger.LogWarning("EnableWindow(disable) failed for handle {Handle}, Win32 error {ErrorCode}", parentHandle, errorCode);
            return;
        }

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

        // EnableWindow returns the PREVIOUS enabled state (false = was disabled, true = was enabled).
        // A return of false when enabling means the window WAS disabled, which is expected for modals.
        // The call only truly "fails" if the handle is invalid (guarded above via IsWindow).
        EnableWindow(parentHandle, true);

        if (!SetForegroundWindow(parentHandle))
        {
            _logger.LogDebug("SetForegroundWindow failed for handle {Handle} — window may not have input focus", parentHandle);
        }

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

        if (!PostMessage(windowHandle, Constants.Window.WM_CLOSE, IntPtr.Zero, IntPtr.Zero))
        {
            var errorCode = Marshal.GetLastWin32Error();
            _logger.LogWarning("PostMessage(WM_CLOSE) failed for handle {Handle}, Win32 error {ErrorCode}", windowHandle, errorCode);
            return;
        }

        _logger.LogDebug("Posted WM_CLOSE to window {Handle}", windowHandle);
    }

    public void Dispose() { }

    // ── P/Invoke ─────────────────────────────────────────────────────────────

    /// <summary>
    /// EnableWindow returns the PREVIOUS enabled state, not success/failure.
    /// Returns true if the window was previously disabled, false if it was previously enabled.
    /// The call fails only if the handle is invalid (guard with IsWindow first).
    /// </summary>
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
