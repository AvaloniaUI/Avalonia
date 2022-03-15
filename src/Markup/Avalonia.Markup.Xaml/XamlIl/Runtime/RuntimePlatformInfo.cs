using System;
using System.Runtime.InteropServices;


namespace Avalonia.Markup.Xaml.XamlIl.Runtime;

public class RuntimePlatformInfo
{
    private static OSPlatform IOS { get; } = OSPlatform.Create("IOS");
    
    private static OSPlatform Android { get; } = OSPlatform.Create("ANDROID");
    
    private static OSPlatform Browser { get; } = OSPlatform.Create("BROWSER");
    
    public static RuntimePlatformInfo Instance { get; } = new();
    
    private RuntimePlatformInfo()
    {
    }

    public bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    public bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    public bool IsIOS => RuntimeInformation.IsOSPlatform(IOS);

    public bool IsAndroid => RuntimeInformation.IsOSPlatform(Android);

    public bool IsBrowser => RuntimeInformation.IsOSPlatform(Browser);

    public bool IsDesktop(Size primaryScreenSize) => primaryScreenSize.Width > 1280;

    public bool IsLaptopOrDesktop(Size primaryScreenSize) =>
        primaryScreenSize.Width > 1024 && primaryScreenSize.Width <= 1280;

    public bool IsTablet(Size primaryScreenSize) =>
        primaryScreenSize.Width > 768 && primaryScreenSize.Width <= 1024;

    public bool IsMobileLandscape(Size primaryScreenSize) =>
        primaryScreenSize.Width > 480 && primaryScreenSize.Width <= 768;

    public bool IsMobile(Size primaryScreenSize) =>
        primaryScreenSize.Width <= 480;
}
