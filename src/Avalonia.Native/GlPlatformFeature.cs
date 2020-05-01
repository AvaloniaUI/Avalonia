using System;
using Avalonia.OpenGL;
using Avalonia.Native.Interop;
using System.Drawing;
using Avalonia.Threading;

namespace Avalonia.Native
{
    class GlPlatformFeature : IWindowingPlatformGlFeature
    {
        public GlPlatformFeature(IAvnGlDisplay display)
        {
            var immediate = display.CreateContext(null);
            var deferred = display.CreateContext(immediate);
            GlDisplay = new GlDisplay(display, immediate.SampleCount, immediate.StencilSize);
            
            ImmediateContext = new GlContext(Display, immediate);
            DeferredContext = new GlContext(Display, deferred);
        }

        public IGlContext ImmediateContext { get; }
        internal GlContext DeferredContext { get; }
        internal GlDisplay GlDisplay;
        public GlDisplay Display => GlDisplay;
    }

    class GlDisplay : IGlDisplay
    {
        private readonly IAvnGlDisplay _display;

        public GlDisplay(IAvnGlDisplay display, int sampleCount, int stencilSize)
        {
            _display = display;
            SampleCount = sampleCount;
            StencilSize = stencilSize;
            GlInterface = new GlInterface((name, optional) =>
            {
                var rv = _display.GetProcAddress(name);
                if (rv == IntPtr.Zero && !optional)
                    throw new OpenGlException($"{name} not found in system OpenGL");
                return rv;
            });
        }

        public GlDisplayType Type => GlDisplayType.OpenGL2;

        public GlInterface GlInterface { get; }

        public int SampleCount { get; }

        public int StencilSize { get; }

        public void ClearContext() => _display.LegacyClearCurrentContext();
    }

    class GlContext : IGlContext
    {
        public IAvnGlContext Context { get; }

        public GlContext(GlDisplay display, IAvnGlContext context)
        {
            Display = display;
            Context = context;
        }

        public IGlDisplay Display { get; }

        public void MakeCurrent()
        {
            Context.LegacyMakeCurrent();
        }
    }


    class GlPlatformSurfaceRenderTarget : IGlPlatformSurfaceRenderTarget
    {
        private IAvnGlSurfaceRenderTarget _target;
        public GlPlatformSurfaceRenderTarget(IAvnGlSurfaceRenderTarget target)
        {
            _target = target;
        }

        public IGlPlatformSurfaceRenderingSession BeginDraw()
        {
            var feature = (GlPlatformFeature)AvaloniaLocator.Current.GetService<IWindowingPlatformGlFeature>();
            return new GlPlatformSurfaceRenderingSession(feature.Display, _target.BeginDrawing());
        }

        public void Dispose()
        {
            _target?.Dispose();
            _target = null;
        }
    }

    class GlPlatformSurfaceRenderingSession : IGlPlatformSurfaceRenderingSession
    {
        private IAvnGlSurfaceRenderingSession _session;

        public GlPlatformSurfaceRenderingSession(GlDisplay display, IAvnGlSurfaceRenderingSession session)
        {
            Display = display;
            _session = session;
        }

        public IGlDisplay Display { get; }

        public PixelSize Size
        {
            get
            {
                var s = _session.GetPixelSize();
                return new PixelSize(s.Width, s.Height);
            }
        }

        public double Scaling => _session.GetScaling();


        public bool IsYFlipped => true;
        
        public void Dispose()
        {
            _session?.Dispose();
            _session = null;
        }
    }

    class GlPlatformSurface : IGlPlatformSurface
    {
        private readonly IAvnWindowBase _window;

        public GlPlatformSurface(IAvnWindowBase window)
        {
            _window = window;
        }
        public IGlPlatformSurfaceRenderTarget CreateGlRenderTarget()
        {
            return new GlPlatformSurfaceRenderTarget(_window.CreateGlRenderTarget());
        }

    }
}
