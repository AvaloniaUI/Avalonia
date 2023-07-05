using System;
using System.Threading;
using Avalonia.Compatibility;
using Avalonia.Metadata;
using Avalonia.Platform.Internal;

namespace Avalonia.Platform
{
    [PrivateApi]
    public class StandardRuntimePlatform : IRuntimePlatform
    {
        private static readonly RuntimePlatformInfo s_info = new()
        {
            IsDesktop = OperatingSystemEx.IsWindows() || OperatingSystemEx.IsMacOS() || OperatingSystemEx.IsLinux(),
            IsMobile = OperatingSystemEx.IsAndroid() || OperatingSystemEx.IsIOS()
        };
        
        public virtual RuntimePlatformInfo GetRuntimeInfo() => s_info;
    }
}
