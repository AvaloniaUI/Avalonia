using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Perspex.Platform;

namespace Perspex.Shared.PlatformSupport
{
    static class SharedPlatform
    {
        public static void Register(Assembly assembly = null)
        {
            PerspexLocator.CurrentMutable
                .Bind<IPclPlatformWrapper>().ToSingleton<PclPlatformWrapper>()
                .Bind<IAssetLoader>().ToConstant(new AssetLoader(assembly));
        }
    }
}
