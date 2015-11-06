using System;
using System.Collections.Generic;
using System.Text;
using Perspex.Platform;

namespace Perspex.Skia
{
    public static class SkiaPlatform
    {
        public static void Initialize() 
            => PerspexLocator.CurrentMutable.Bind<IPlatformRenderInterface>().ToConstant(new PlatformRenderInterface());
    }
}
