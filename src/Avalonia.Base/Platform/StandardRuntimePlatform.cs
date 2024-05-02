using Avalonia.Compatibility;
using Avalonia.Metadata;

namespace Avalonia.Platform
{
    [PrivateApi]
    public class StandardRuntimePlatform : IRuntimePlatform
    {
        public virtual RuntimePlatformInfo GetRuntimeInfo() => new()
        {
            IsDesktop = OperatingSystemEx.IsWindows()
                        || OperatingSystemEx.IsMacOS() || OperatingSystemEx.IsMacCatalyst()
                        || OperatingSystemEx.IsLinux() || OperatingSystemEx.IsFreeBSD(),
            IsMobile = OperatingSystemEx.IsAndroid() || (OperatingSystemEx.IsIOS() && !OperatingSystemEx.IsMacCatalyst()),
            IsTV = OperatingSystemEx.IsTvOS()
        };
    }
}
