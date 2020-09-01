using Avalonia.Platform;

namespace Avalonia.Shared.PlatformSupport
{
    partial class StandardRuntimePlatform
    {
        public RuntimePlatformInfo GetRuntimeInfo()
        {
            return new RuntimePlatformInfo
            {
                IsDesktop = false,
                IsMobile = true,
                IsMono = true,
                IsUnix = true,
                OperatingSystem = OperatingSystemType.iOS
            };
        }
    }
}