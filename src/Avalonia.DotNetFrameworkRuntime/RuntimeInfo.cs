using System;
using System.Runtime.InteropServices;
using Avalonia.Platform;

namespace Avalonia.Shared.PlatformSupport
{
    internal partial class StandardRuntimePlatform
    {
        private static readonly Lazy<RuntimePlatformInfo> Info = new Lazy<RuntimePlatformInfo>(() =>
        {
            var isMono = Type.GetType("Mono.Runtime") != null;
            var isUnix = Environment.OSVersion.Platform == PlatformID.Unix ||
                         Environment.OSVersion.Platform == PlatformID.MacOSX;
            return new RuntimePlatformInfo
            {
                IsCoreClr = false,
                IsDesktop = true,
                IsDotNetFramework = !isMono,
                IsMono = isMono,
                IsMobile = false,
                IsUnix = isUnix,
                OperatingSystem = isUnix ? DetectUnix() : OperatingSystemType.WinNT,
            };
        });

        [DllImport("libc")]
        static extern int uname(IntPtr buf);

        static OperatingSystemType DetectUnix()
        {
            var buffer = Marshal.AllocHGlobal(0x1000);
            uname(buffer);
            var unixName = Marshal.PtrToStringAnsi(buffer);
            Marshal.FreeHGlobal(buffer);
            if(unixName=="Darwin")
                return OperatingSystemType.OSX;
            if (unixName == "Linux")
                return OperatingSystemType.Linux;
            return OperatingSystemType.Unknown;
        }

        public RuntimePlatformInfo GetRuntimeInfo() => Info.Value;
    }
}
