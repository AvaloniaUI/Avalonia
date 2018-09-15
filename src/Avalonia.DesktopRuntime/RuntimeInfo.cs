using System;
using System.Runtime.InteropServices;
using Avalonia.Platform;


namespace Avalonia.Shared.PlatformSupport
{
    internal partial class StandardRuntimePlatform
    {
        private static readonly Lazy<RuntimePlatformInfo> Info = new Lazy<RuntimePlatformInfo>(() =>
        {
            OperatingSystemType os;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                os = OperatingSystemType.OSX;
            else if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                os = OperatingSystemType.Linux;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                os = OperatingSystemType.WinNT;
            else
                throw new Exception("Unknown OS platform " + RuntimeInformation.OSDescription);

            return new RuntimePlatformInfo
            {
#if NETCOREAPP2_0
                IsCoreClr = true,
#elif NET461
                IsDotNetFramework = false,
#endif
                IsDesktop = true,
                IsMono = false,
                IsMobile = false,
                IsUnix = os != OperatingSystemType.WinNT,
                OperatingSystem = os,
            };
        });


        public RuntimePlatformInfo GetRuntimeInfo() => Info.Value;
    }
}
