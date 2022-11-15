using System;
using System.Collections.Generic;
using Avalonia.Logging;
using Avalonia.OpenGL;
using Avalonia.Platform;

namespace Avalonia.X11.Glx
{
    class GlxPlatformOpenGlInterface : IPlatformOpenGlInterface
    {
        public GlxDisplay Display { get; private set; }
        public bool CanCreateContexts => true;
        public bool CanShareContexts => true;
        public IGlContext CreateContext() => Display.CreateContext();
        public IGlContext CreateSharedContext() => Display.CreateContext(PrimaryContext);
        public GlxContext DeferredContext { get; private set; }
        public IGlContext PrimaryContext => DeferredContext;
        IPlatformGpuContext IPlatformGpu.PrimaryContext => PrimaryContext;

        public static bool TryInitialize(X11Info x11, IList<GlVersion> glProfiles)
        {
            var feature = TryCreate(x11, glProfiles);
            if (feature != null)
            {
                AvaloniaLocator.CurrentMutable.Bind<IPlatformOpenGlInterface>().ToConstant(feature);
                return true;
            }

            return false;
        }
        
        public static GlxPlatformOpenGlInterface TryCreate(X11Info x11, IList<GlVersion> glProfiles)
        {
            try
            {
                var disp = new GlxDisplay(x11, glProfiles);
                return new GlxPlatformOpenGlInterface
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
