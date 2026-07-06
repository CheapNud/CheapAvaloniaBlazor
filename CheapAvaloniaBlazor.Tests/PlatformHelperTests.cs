namespace CheapAvaloniaBlazor.Tests;

public class PlatformHelperTests
{
    [Fact]
    public void Linux_startup_issues_are_empty_on_non_linux_platforms()
    {
        if (OperatingSystem.IsLinux())
        {
            // On Linux the probes hit the real system (display, GTK, WebKitGTK, glibc) —
            // the result depends on the environment, so only assert it never throws.
            _ = PlatformHelper.GetLinuxStartupIssues();
            return;
        }

        Assert.Empty(PlatformHelper.GetLinuxStartupIssues());
    }
}
