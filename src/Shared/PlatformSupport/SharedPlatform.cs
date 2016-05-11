using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Avalonia.Platform;

namespace Avalonia.Shared.PlatformSupport
{
    static class SharedPlatform
    {
        public static void Register(Assembly assembly = null)
        {
            AvaloniaLocator.CurrentMutable
                .Bind<IPclPlatformWrapper>().ToSingleton<PclPlatformWrapper>()
                .Bind<IAssetLoader>().ToConstant(new AssetLoader(assembly));
        }
    }
}
