using System;
using Avalonia.Logging;
using Avalonia.Platform;

namespace Avalonia.OpenGL.Egl
{
    public sealed class EglPlatformGraphics : IPlatformGraphics
    {
        private readonly EglDisplay _display;
        public bool UsesSharedContext => false;
        public IPlatformGraphicsContext CreateContext() => _display.CreateContext(null);
        public IPlatformGraphicsContext GetSharedContext() => throw new NotSupportedException();
        
        public EglPlatformGraphics(EglDisplay display)
        {
            _display = display;
        }
        
        public static EglPlatformGraphics? TryCreate() => TryCreate(() => new EglDisplay(new EglDisplayCreationOptions
        {
            Egl = new EglInterface(),
            // Those are expected to be supported by most EGL implementations
            SupportsMultipleContexts = true,
            SupportsContextSharing = true
        }));
        
        public static EglPlatformGraphics? TryCreate(Func<EglDisplay> displayFactory)
        {
            try
            {
                return new EglPlatformGraphics(displayFactory());
            }
            catch(Exception e)
            {
                Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log(null, "Unable to initialize EGL-based rendering: {0}", e);
                return null;
            }
        }
    }
}
