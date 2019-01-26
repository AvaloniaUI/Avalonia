using System;
using Avalonia.Logging;
using Avalonia.OpenGL;

namespace Avalonia.X11.Glx
{
    class GlxGlPlatformFeature : IWindowingPlatformGlFeature
    {
        public GlxDisplay Display { get; private set; }
        public IGlContext ImmediateContext { get; private set; }
        public GlxContext DeferredContext { get; private set; }

        public static bool TryInitialize(X11Info x11)
        {
            var feature = TryCreate(x11);
            if (feature != null)
            {
                AvaloniaLocator.CurrentMutable.Bind<IWindowingPlatformGlFeature>().ToConstant(feature);
                return true;
            }

            return false;
        }
        
        public static GlxGlPlatformFeature TryCreate(X11Info x11)
        {
            try
            {
                var disp = new GlxDisplay(x11);
                return new GlxGlPlatformFeature
                {
                    Display = disp,
                    ImmediateContext = disp.ImmediateContext,
                    DeferredContext = disp.DeferredContext
                };
            }
            catch(Exception e)
            {
                Logger.Error("OpenGL", null, "Unable to initialize GLX-based rendering: {0}", e);
                return null;
            }
        }
    }
}
