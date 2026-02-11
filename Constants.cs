namespace CheapAvaloniaBlazor;

/// <summary>
/// Central repository for all constant values used throughout CheapAvaloniaBlazor
/// </summary>
public static class Constants
{
    /// <summary>
    /// Path-related constants
    /// </summary>
    public static class Paths
    {
        public const string ComponentsDirectory = "Components";
        public const string WwwRoot = "wwwroot";
        public const string EmbeddedResourceNamespace = "CheapAvaloniaBlazor.wwwroot";
    }

    /// <summary>
    /// Endpoint and URL constants
    /// </summary>
    public static class Endpoints
    {
        public const string ContentPath = "/_content/CheapAvaloniaBlazor";
        public const string ErrorPage = "/Error";
        public const string TestEndpoint = "/_content/CheapAvaloniaBlazor/test";
        public const string JavaScriptBridgeEndpoint = "/_content/CheapAvaloniaBlazor/cheap-blazor-interop.js";
    }

    /// <summary>
    /// JavaScript and resource file names
    /// </summary>
    public static class Resources
    {
        public const string JavaScriptBridgeFileName = "cheap-blazor-interop.js";
        public const string JavaScriptBridgeResourcePattern = "cheap-blazor-interop";
    }

    /// <summary>
    /// Diagnostic and logging constants
    /// </summary>
    public static class Diagnostics
    {
        public const string Prefix = "DIAGNOSTICS:";
        public const string ServiceProviderCreated = "DIAGNOSTICS: Service provider created for scope";
        public const string CompleteServiceRegistration = "DIAGNOSTICS: Complete service registration summary:";
        public const string RazorComponentsAdded = "DIAGNOSTICS: - RazorComponents: Added with InteractiveServerComponents";
        public const string DesktopInteropAdded = "DIAGNOSTICS: - DesktopInteropService: Added";
        public const string NavigationManagerAutoRegistered = "DIAGNOSTICS: - NavigationManager: Auto-registered by AddRazorComponents";
    }

    /// <summary>
    /// Default configuration values
    /// </summary>
    public static class Defaults
    {
        // Network
        public const int DefaultPort = 5000;
        public const string HttpScheme = "http";
        public const string HttpsScheme = "https";
        public const string LocalhostAddress = "localhost";

        // Window
        public const string DefaultWindowTitle = "Blazor Desktop App";
        public const int DefaultWindowWidth = 1200;
        public const int DefaultWindowHeight = 800;
        public const int MinimumWindowSize = 1;
        public const int OffScreenPosition = -32000;
        public const int MinimumResizableWidth = 640;
        public const int MinimumResizableHeight = 480;

        // Timeouts and Intervals
        public const int StartupTimeoutSeconds = 30;
        public const int StartupCheckIntervalMilliseconds = 100;
        public const int HttpClientTimeoutSeconds = 5;
        public const int ServerStabilizationDelayMilliseconds = 1000;
        public const int ServerReadinessCheckDelayMilliseconds = 500;
        public const int ServerReadinessMaxAttempts = 10;
        public const int ServerShutdownTimeoutSeconds = 5;
        public const int WindowBringToFrontDelayMilliseconds = 100;
        public const int ScriptExecutionTimeoutSeconds = 30;

        // Port scanning
        public const int PortScanRange = 100;

        // Blazor Server Configuration
        public const int DisconnectedCircuitRetentionMinutes = 3;
        public const int DisconnectedCircuitMaxRetained = 100;
        public const int ClientTimeoutSeconds = 60;
        public const int HandshakeTimeoutSeconds = 30;
        public const int MaximumReceiveMessageSizeBytes = 32 * 1024; // 32KB

        // JavaScript
        public const int MaxScriptLength = 10000;

        // Application Data
        public const string AppDataFolderName = "CheapAvaloniaBlazor";

        // Zoom
        public const int DefaultZoomLevel = 100;

        // Retry
        public const int MaxStartupRetries = 3;

        // Reconnect intervals
        public static readonly int[] ReconnectIntervals = { 0, 2, 10, 30 };

        // Splash Screen
        public const string SplashLoadingMessage = "Loading...";
        public const int SplashWindowWidth = 400;
        public const int SplashWindowHeight = 250;
        public const string SplashBackgroundColor = "#2D2D30";
        public const string SplashForegroundColor = "#FFFFFF";
        public const double SplashTitleFontSize = 24.0;
        public const double SplashMessageFontSize = 14.0;
    }

    /// <summary>
    /// Component and type name constants
    /// </summary>
    public static class ComponentNames
    {
        public const string App = "App";
        public const string Routes = "Routes";
        public const string RootComponentSelector = "app";
    }

    /// <summary>
    /// HTTP and Web constants
    /// </summary>
    public static class Http
    {
        public const string ConnectionHeader = "Connection";
        public const string ContentTypeHtml = "text/html";
        public const string ContentTypeJavaScript = "application/javascript";
        public const int StatusCodeNotFound = 404;
        public const int StatusCodeInternalServerError = 500;
    }

    /// <summary>
    /// Window state constants
    /// </summary>
    public static class WindowStates
    {
        public const string Normal = "normal";
        public const string Maximized = "maximized";
        public const string Minimized = "minimized";
    }

    /// <summary>
    /// Message type constants for Photino communication
    /// </summary>
    public static class MessageTypes
    {
        public const string Minimize = "minimize";
        public const string Maximize = "maximize";
        public const string Restore = "restore";
        public const string ToggleMaximize = "toggleMaximize";
        public const string Close = "close";
        public const string SetTitle = "setTitle";
        public const string GetWindowState = "getWindowState";
        public const string ScriptResultPrefix = "scriptResult_";
        public const string ResponsePrefix = "response_";
        public const string ResultPrefix = "result_";
    }

    /// <summary>
    /// JavaScript property and method names
    /// </summary>
    public static class JavaScript
    {
        public const string CheapBlazorObject = "cheapBlazor";
        public const string CheapBlazorInteropService = "cheapBlazorInteropService";
        public const string ShowNotificationMethod = "cheapBlazor.showNotification";
        public const string GetClipboardTextMethod = "cheapBlazor.getClipboardText";
        public const string SetClipboardTextMethod = "cheapBlazor.setClipboardText";
        public const string EvalFunction = "eval";
        public const string ChromeWebViewPostMessage = "window.chrome.webview.postMessage";
    }

    /// <summary>
    /// Security-related constants
    /// </summary>
    public static class Security
    {
        public static readonly string[] DangerousScriptPatterns =
        {
            "</script>", "<script", "javascript:", "eval(", "Function(",
            "document.write", "document.cookie", "localStorage.", "sessionStorage.",
            "window.location", "location.href", "location.replace"
        };

        public static readonly string[] AllowedUrlSchemes = { "http", "https", "mailto" };
    }

    /// <summary>
    /// File extension patterns
    /// </summary>
    public static class FileExtensions
    {
        public const string CsHtml = ".cshtml";
        public const string JavaScript = ".js";
        public const string WildcardPrefix = "*.";
    }

    /// <summary>
    /// Search patterns for file operations
    /// </summary>
    public static class SearchPatterns
    {
        public const string AllFiles = "*";
    }

    /// <summary>
    /// Assembly and reflection constants
    /// </summary>
    public static class Reflection
    {
        public const string AssemblyFieldName = "_assembly";
        public const string UnknownVersion = "unknown";
    }

    /// <summary>
    /// Framework and library names
    /// </summary>
    public static class Framework
    {
        public const string Name = "CheapAvaloniaBlazor";
    }

    /// <summary>
    /// HTML and markup constants
    /// </summary>
    public static class Html
    {
        public const string ErrorPrefix = "ERROR: ";
    }

    /// <summary>
    /// Settings persistence constants
    /// </summary>
    public static class Settings
    {
        public const string DefaultFileName = "settings.json";
    }

    /// <summary>
    /// Desktop notification related constants
    /// </summary>
    public static class Notifications
    {
        public const int DefaultExpirationSeconds = 5;
        public const int OverlayWidth = 400;
        public const int OverlayHeight = 600;
        public const int DefaultMaxItems = 3;
    }

    /// <summary>
    /// Native menu bar related constants
    /// </summary>
    public static class MenuBar
    {
        /// <summary>
        /// First Win32 menu item command ID. Avoids collision with system-defined IDs.
        /// </summary>
        public const int FirstMenuItemId = 1001;

        /// <summary>
        /// Maximum Win32 menu item command ID. Must stay below 0xF000 to avoid
        /// collision with system command IDs (SC_CLOSE=0xF060, SC_MINIMIZE=0xF020, etc).
        /// </summary>
        public const int MaxMenuItemId = 0xEFFF;
    }

    /// <summary>
    /// Multi-window and modal dialog constants
    /// </summary>
    public static class Window
    {
        /// <summary>
        /// Window ID assigned to the main (primary) Photino window.
        /// </summary>
        public const string MainWindowId = "main";

        /// <summary>
        /// Query parameter name appended to child window URLs to identify the window.
        /// </summary>
        public const string WindowIdQueryParam = "_windowId";

        /// <summary>
        /// Query parameter name for the component type in the WindowHost route.
        /// </summary>
        public const string ComponentTypeQueryParam = "_type";

        /// <summary>
        /// Blazor route for the library's DynamicComponent host page.
        /// </summary>
        public const string WindowHostRoute = "/_cheapblazor/window";

        /// <summary>
        /// Default width for child windows.
        /// </summary>
        public const int DefaultChildWidth = 800;

        /// <summary>
        /// Default height for child windows.
        /// </summary>
        public const int DefaultChildHeight = 600;

        /// <summary>
        /// Maximum time (ms) to wait for a child window's native handle to become available.
        /// </summary>
        public const int HandleReadyTimeoutMs = 10_000;

        /// <summary>
        /// Win32 WM_CLOSE message constant for thread-safe window close via PostMessage.
        /// </summary>
        public const uint WM_CLOSE = 0x0010;

        /// <summary>
        /// Delay (ms) after child window Invoke to allow Blazor circuit initialization.
        /// </summary>
        public const int ChildWindowPostCreateDelayMs = 100;

        /// <summary>
        /// Maximum number of distinct component types that can be registered for window hosting.
        /// Prevents unbounded growth of the whitelist in pathological scenarios.
        /// </summary>
        public const int MaxRegisteredComponentTypes = 256;
    }

    /// <summary>
    /// Drag-and-drop message types for Photino ↔ JavaScript communication
    /// </summary>
    public static class DragDrop
    {
        public const string DragEnterMessage = "cheapblazor:dragenter";
        public const string DragLeaveMessage = "cheapblazor:dragleave";
        public const string FileDropMessage = "cheapblazor:filedrop";
    }

    /// <summary>
    /// Blazor framework asset constants
    /// </summary>
    public static class BlazorFramework
    {
        public const string BlazorWebJsFileName = "blazor.web.js";
        public const string FrameworkDirectory = "_framework";
        public const string InternalAssetsPackageName = "microsoft.aspnetcore.app.internal.assets";
    }

    /// <summary>
    /// System tray related constants
    /// </summary>
    public static class SystemTray
    {
        public const string DefaultTooltip = "Blazor Desktop App";
        public const string ShowMenuText = "Show";
        public const string ExitMenuText = "Exit";
        public const string DefaultShowMenuId = "tray_show";
        public const string DefaultExitMenuId = "tray_exit";
    }

}
