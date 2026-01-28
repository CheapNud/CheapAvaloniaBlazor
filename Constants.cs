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

}
