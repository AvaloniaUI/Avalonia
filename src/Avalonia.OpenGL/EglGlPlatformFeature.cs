using System;
using Avalonia.Logging;

namespace Avalonia.OpenGL
{
    public class EglGlPlatformFeature : IWindowingPlatformGlFeature
    {
        private EglDisplay _display;
        public EglDisplay Display => _display;
        public IGlContext CreateContext()
        {
            return _display.CreateContext(DeferredContext);
        }
        public EglContext DeferredContext { get; private set; }
        public IGlContext MainContext => DeferredContext;

        public static void TryInitialize()
        {
            var feature = TryCreate();
            if (feature != null)
                AvaloniaLocator.CurrentMutable.Bind<IWindowingPlatformGlFeature>().ToConstant(feature);
        }

        public static EglGlPlatformFeature TryCreate() => TryCreate(() => new EglDisplay());
        public static EglGlPlatformFeature TryCreate(Func<EglDisplay> displayFactory)
        {
            try
            {
                var disp = displayFactory();
                return new EglGlPlatformFeature
                {
                    _display = disp,
                    DeferredContext = disp.CreateContext(null)
                };
            }
            catch(Exception e)
            {
                Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log(null, "Unable to initialize EGL-based rendering: {0}", e);
                return null;
            }
        }
    }
}
