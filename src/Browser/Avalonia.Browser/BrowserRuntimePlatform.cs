using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Text.RegularExpressions;
using Avalonia.Browser.Interop;
using Avalonia.Platform;

namespace Avalonia.Browser;

internal class BrowserRuntimePlatform : StandardRuntimePlatform
{
    private static readonly Lazy<RuntimePlatformInfo> Info = new(() =>
    {
        var isMobile = AvaloniaModule.IsMobile();
        var result = new RuntimePlatformInfo
        {
            IsMobile = isMobile,
            IsDesktop = !isMobile
        };
        
        return result;
    });

    public override RuntimePlatformInfo GetRuntimeInfo() => Info.Value;
}
