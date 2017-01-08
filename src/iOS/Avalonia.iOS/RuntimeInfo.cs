using Avalonia.Platform;
namespace Avalonia.Shared.PlatformSupport
{
    internal partial class StandardRuntimePlatform
    {
        public RuntimePlatformInfo GetRuntimeInfo() => new RuntimePlatformInfo
        {
            IsCoreClr = false,
            IsDesktop = false,
            IsMobile = true,
            IsDotNetFramework = false,
            IsMono = true,
            IsUnix = true,
            OperatingSystem = OperatingSystemType.Android
        };
    }
}