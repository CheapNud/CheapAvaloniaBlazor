using System.Reflection;

namespace CheapAvaloniaBlazor.Services;

/// <summary>
/// Extracts the embedded JavaScript bridge file to a physical location for reliable serving
/// </summary>
public static class JavaScriptBridgeExtractor
{
    private static bool _extracted = false;
    private static string? _extractedPath = null;

    /// <summary>
    /// Extracts the cheap-blazor-interop.js file from embedded resources to wwwroot
    /// </summary>
    public static string ExtractJavaScriptBridge(string wwwrootPath, DiagnosticLogger logger)
    {
        if (_extracted && _extractedPath != null && File.Exists(_extractedPath))
        {
            logger.LogVerbose("JavaScript bridge already extracted to: {ExtractedPath}", _extractedPath);
            return _extractedPath;
        }

        try
        {
            // Get the assembly containing the embedded resource
            var assembly = Assembly.GetExecutingAssembly();

            // Find the embedded resource
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(r => r.Contains(Constants.Resources.JavaScriptBridgeResourcePattern));

            if (resourceName == null)
            {
                logger.LogError($"{Constants.Resources.JavaScriptBridgeFileName} not found in embedded resources!");
                logger.LogDiagnostic("Available embedded resources:");
                foreach (var res in assembly.GetManifestResourceNames())
                {
                    logger.LogDiagnostic("  - {ResourceName}", res);
                }
                throw new FileNotFoundException($"{Constants.Resources.JavaScriptBridgeFileName} not found in embedded resources");
            }

            logger.LogDiagnostic("Found embedded resource: {ResourceName}", resourceName);

            // Create wwwroot directory if it doesn't exist
            if (!Directory.Exists(wwwrootPath))
            {
                Directory.CreateDirectory(wwwrootPath);
                logger.LogDiagnostic("Created directory: {WwwrootPath}", wwwrootPath);
            }

            // Extract to physical file
            var targetPath = Path.Combine(wwwrootPath, Constants.Resources.JavaScriptBridgeFileName);

            using (var resourceStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (resourceStream == null)
                {
                    throw new InvalidOperationException($"Could not load stream for resource: {resourceName}");
                }

                using (var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write))
                {
                    resourceStream.CopyTo(fileStream);
                }
            }

            _extractedPath = targetPath;
            _extracted = true;

            logger.LogInformation("JavaScript bridge extracted successfully to: {TargetPath}", targetPath);
            logger.LogDiagnostic("Extraction details - Source: {ResourceName}, Size: {Size} bytes",
                resourceName, new FileInfo(targetPath).Length);

            return targetPath;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to extract JavaScript bridge");
            throw;
        }
    }
}
