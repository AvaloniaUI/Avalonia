using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Platform;

namespace Avalonia
{
    public static class SkiaApplicationExtensions
    {
        public static TApp UseSkia<TApp>(this TApp app) where TApp : Application
        {
            Avalonia.Skia.SkiaPlatform.Initialize();
            return app;
        }
    }
}

namespace Avalonia.Skia
{
    public static class SkiaPlatform
    {
        private static bool s_forceSoftwareRendering;

        public static void Initialize()
            => AvaloniaLocator.CurrentMutable.Bind<IPlatformRenderInterface>().ToConstant(new PlatformRenderInterface());

        public static bool ForceSoftwareRendering
        {
            get { return s_forceSoftwareRendering; }
            set
            {
                s_forceSoftwareRendering = value;

                // TODO: I left this property here as place holder. Do we still need the ability to Force software rendering? 
                // Is it even possible with SkiaSharp? Perhaps kekekes can answer as part of the HW accel work. 
                // 
                throw new NotImplementedException();
            }
        }
    }
}
