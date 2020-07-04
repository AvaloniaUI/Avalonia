using System;
using System.Collections.Generic;
using Avalonia.Logging;
using Avalonia.OpenGL;

namespace Avalonia.X11.Glx
{
    class GlxGlPlatformFeature : IWindowingPlatformGlFeature
    {
        public GlxDisplay Display { get; private set; }
        public IGlContext CreateContext() => Display.CreateContext();
        public GlxContext DeferredContext { get; private set; }
        public IGlContext MainContext => DeferredContext;

        public static bool TryInitialize(X11Info x11, List<GlVersion> glProfiles)
        {
            var feature = TryCreate(x11, glProfiles);
            if (feature != null)
            {
                AvaloniaLocator.CurrentMutable.Bind<IWindowingPlatformGlFeature>().ToConstant(feature);
                return true;
            }

            return false;
        }
        
        public static GlxGlPlatformFeature TryCreate(X11Info x11, List<GlVersion> glProfiles)
        {
            try
            {
                var disp = new GlxDisplay(x11, glProfiles);
                return new GlxGlPlatformFeature
                {
                    Display = disp,
                    DeferredContext = disp.DeferredContext
                };
            }
            catch(Exception e)
            {
                Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log(null, "Unable to initialize GLX-based rendering: {0}", e);
                return null;
            }
        }
    }
}
