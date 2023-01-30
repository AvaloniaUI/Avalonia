using System;
using System.Collections.Generic;
using Avalonia.Logging;
using Avalonia.OpenGL;
using Avalonia.Platform;

namespace Avalonia.X11.Glx
{
    class GlxPlatformGraphics : IPlatformGraphics
    {
        public GlxDisplay Display { get; private set; }
        public bool CanCreateContexts => true;
        public bool CanShareContexts => true;
        public bool UsesSharedContext => false;
        IPlatformGraphicsContext IPlatformGraphics.CreateContext() => Display.CreateContext();

        public IPlatformGraphicsContext GetSharedContext() => throw new NotSupportedException();

        public static bool TryInitialize(X11Info x11, IList<GlVersion> glProfiles)
        {
            var feature = TryCreate(x11, glProfiles);
            if (feature != null)
            {
                AvaloniaLocator.CurrentMutable.Bind<IPlatformGraphics>().ToConstant(feature);
                return true;
            }

            return false;
        }
        
        public static GlxPlatformGraphics TryCreate(X11Info x11, IList<GlVersion> glProfiles)
        {
            try
            {
                var disp = new GlxDisplay(x11, glProfiles);
                return new GlxPlatformGraphics
                {
                    Display = disp
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
