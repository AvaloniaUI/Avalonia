using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
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
                IsCoreClr = true,
                IsDesktop = true,
                IsDotNetFramework = false,
                IsMono = false,
                IsMobile = false,
                IsUnix = os != OperatingSystemType.WinNT,
                OperatingSystem = os,
            };
        });


        public RuntimePlatformInfo GetRuntimeInfo() => Info.Value;
    }
}
