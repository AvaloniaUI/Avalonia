using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Text.RegularExpressions;
using Avalonia.Browser.Interop;
using Avalonia.Platform;

namespace Avalonia.Browser;

internal static class BrowserRuntimePlatformServices
{
    public static AppBuilder UseBrowserRuntimePlatformSubsystem(this AppBuilder builder)
    {
        builder.UseRuntimePlatformSubsystem(() => Register(builder.ApplicationType?.Assembly), nameof(BrowserRuntimePlatform));
        return builder;
    }
    
    public static void Register(Assembly? assembly = null)
    {
        AssetLoader.RegisterResUriParsers();
        AvaloniaLocator.CurrentMutable
            .Bind<IRuntimePlatform>().ToSingleton<BrowserRuntimePlatform>()
            .Bind<IAssetLoader>().ToConstant(new StandardAssetLoader(assembly));
    }
}

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
