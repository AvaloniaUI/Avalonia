using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Text.RegularExpressions;
using Avalonia.Platform;
using Avalonia.Web.Interop;

namespace Avalonia.Web;

internal class BrowserRuntimePlatform : StandardRuntimePlatform
{
    private static readonly Lazy<RuntimePlatformInfo> Info = new(() =>
    {
        OperatingSystemType os;

        bool isBrowserMobile = false;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            os = OperatingSystemType.OSX;
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            os = OperatingSystemType.Linux;
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            os = OperatingSystemType.WinNT;
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("Android")))
            os = OperatingSystemType.Android;
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("iOS")))
            os = OperatingSystemType.iOS;
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("Browser")))
        {
            os = OperatingSystemType.Browser;
            isBrowserMobile = AvaloniaModule.IsMobile();
        }
        else
            throw new Exception("Unknown OS platform " + RuntimeInformation.OSDescription);

        // Source: https://github.com/dotnet/runtime/blob/main/src/libraries/Common/tests/TestUtilities/System/PlatformDetection.cs
        var isCoreClr = Environment.Version.Major >= 5 ||
                        RuntimeInformation.FrameworkDescription.StartsWith(".NET Core",
                            StringComparison.OrdinalIgnoreCase);
        var isMonoRuntime = Type.GetType("Mono.Runtime") != null;
        var isFramework = !isCoreClr &&
                          RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework",
                              StringComparison.OrdinalIgnoreCase);

        var result = new RuntimePlatformInfo
        {
            IsCoreClr = isCoreClr,
            IsDotNetFramework = isFramework,
            IsMono = isMonoRuntime,
            IsDesktop = os is OperatingSystemType.Linux or OperatingSystemType.OSX or OperatingSystemType.WinNT,
            IsMobile = os is OperatingSystemType.Android or OperatingSystemType.iOS,
            IsUnix = os is OperatingSystemType.Linux or OperatingSystemType.OSX or OperatingSystemType.Android,
            IsBrowser = os == OperatingSystemType.Browser,
            OperatingSystem = os,
        };

        if (result.IsBrowser)
        {
            if (isBrowserMobile)
            {
                result.IsMobile = true;
            }
            else
            {
                result.IsDesktop = true;
            }
        }


        return result;
    });

    public override RuntimePlatformInfo GetRuntimeInfo() => Info.Value;
}
