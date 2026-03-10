using System;
using System.Runtime.InteropServices;

namespace Avalonia.Compatibility
{
    internal sealed class OperatingSystemEx
    {
        public static bool IsWindows() => OperatingSystem.IsWindows();
        public static bool IsMacOS() => OperatingSystem.IsMacOS();
        public static bool IsMacCatalyst() => OperatingSystem.IsMacCatalyst();
        public static bool IsLinux() => OperatingSystem.IsLinux();
        public static bool IsFreeBSD() => OperatingSystem.IsFreeBSD();
        public static bool IsAndroid() => OperatingSystem.IsAndroid();
        public static bool IsIOS() => OperatingSystem.IsIOS();
        public static bool IsTvOS() => OperatingSystem.IsTvOS();
        public static bool IsBrowser() => OperatingSystem.IsBrowser();
        public static bool IsOSPlatform(string platform) => OperatingSystem.IsOSPlatform(platform);
    }
}
