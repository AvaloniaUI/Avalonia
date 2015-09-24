using System;
using System.Collections.Generic;
using System.Text;
using Perspex.Platform;

namespace Perspex.Shared.PlatformSupport
{
    static class SharedPlatform
    {
        public static void Register()
        {
            PerspexLocator.CurrentMutable
                .Bind<IPclPlatformWrapper>().ToSingleton<PclPlatformWrapper>()
                .Bind<IAssetLoader>().ToSingleton<AssetLoader>();
        }
    }
}
