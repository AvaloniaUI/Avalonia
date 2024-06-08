namespace Tmds.DBus.Protocol;
#if NET6_0_OR_GREATER
using System.Runtime.Versioning;
#endif

static class PlatformDetection
{
#if NET6_0_OR_GREATER
    [SupportedOSPlatformGuard("windows")]
#endif
    public static bool IsWindows() =>
#if NET6_0_OR_GREATER
        // IsWindows is marked with the NonVersionable attribute.
        // This allows R2R to inline it and eliminate platform-specific branches.
        OperatingSystem.IsWindows();
#else
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#endif
}