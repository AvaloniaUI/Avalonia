using System;
using Avalonia.Metadata;

namespace Avalonia.Platform
{
    [PrivateApi]
    public class StandardRuntimePlatform : IRuntimePlatform
    {
        public virtual RuntimePlatformInfo GetRuntimeInfo() => new()
        {
            IsDesktop = OperatingSystem.IsWindows()
                        || OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst()
                        || OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD(),
            IsMobile = OperatingSystem.IsAndroid() || (OperatingSystem.IsIOS() && !OperatingSystem.IsMacCatalyst()),
            IsTV = OperatingSystem.IsTvOS()
        };
    }
}
