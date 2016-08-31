using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Avalonia.Platform;

namespace Avalonia.Shared.PlatformSupport
{
    static class StandardRuntimePlatformServices
    {
        public static void Register(Assembly assembly = null)
        {
            AvaloniaLocator.CurrentMutable
                .Bind<IRuntimePlatform>().ToSingleton<StandardRuntimePlatform>()
                .Bind<IAssetLoader>().ToConstant(new AssetLoader(assembly));
        }
    }
}
