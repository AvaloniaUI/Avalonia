using System;
using System.Runtime.InteropServices;

namespace Avalonia.Compatibility
{
    internal sealed class OperatingSystemEx
    {
#if NET6_0_OR_GREATER
        public static bool IsWindows() => OperatingSystem.IsWindows();
        public static bool IsMacOS() => OperatingSystem.IsMacOS();
        public static bool IsLinux() => OperatingSystem.IsLinux();
        public static bool IsAndroid() => OperatingSystem.IsAndroid();
        public static bool IsIOS() => OperatingSystem.IsIOS();
        public static bool IsBrowser() => OperatingSystem.IsBrowser();
        public static bool IsOSPlatform(string platform) => OperatingSystem.IsOSPlatform(platform);
#else
        public static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool IsMacOS() => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        public static bool IsLinux() => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        public static bool IsAndroid() => IsOSPlatform("ANDROID");
        public static bool IsIOS() => IsOSPlatform("IOS");
        public static bool IsBrowser() => IsOSPlatform("BROWSER");
        public static bool IsOSPlatform(string platform) => RuntimeInformation.IsOSPlatform(OSPlatform.Create(platform));
#endif
    }
}
