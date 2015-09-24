using System;
using System.Collections.Generic;
using System.Text;
using Perspex.Platform;
using Splat;

namespace Perspex.Shared.PlatformSupport
{
    static class SharedPlatform
    {
        public static void Register()
        {
            var locator = Locator.CurrentMutable;
            locator.Register(() => new PclPlatformWrapper(), typeof(IPclPlatformWrapper));
            locator.RegisterConstant(new AssetLoader(), typeof(IAssetLoader));
        }
    }
}
