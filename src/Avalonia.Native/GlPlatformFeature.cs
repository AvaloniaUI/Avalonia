using System;
using Avalonia.OpenGL;
using Avalonia.Native.Interop;
using System.Drawing;
using System.Reactive.Disposables;
using Avalonia.Threading;

namespace Avalonia.Native
{
    class GlPlatformFeature : IWindowingPlatformGlFeature
    {
        private GlContext _immediateContext;
        public GlPlatformFeature(IAvnGlFeature feature)
        {
            Display = new GlDisplay(feature.ObtainDisplay());
            _immediateContext = new GlContext(Display, feature.ObtainImmediateContext());
        }

        public GlDisplay Display { get; }
        IGlDisplay IWindowingPlatformGlFeature.Display => Display;

        public IGlContext CreateContext()
        {
            if (_immediateContext != null)
            {
                var rv = _immediateContext;
                _immediateContext = null;
                return rv;
            }

            throw new PlatformNotSupportedException(
                "OSX backend haven't switched to the new model yet, so there are no custom contexts, sorry");
        }
    }

    class GlDisplay : IGlDisplay
    {
        private readonly IAvnGlDisplay _display;

        public GlDisplay(IAvnGlDisplay display)
        {
            _display = display;
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

        public int SampleCount => _display.GetSampleCount();

        public int StencilSize => _display.GetStencilSize();

        public void ClearContext() => _display.ClearContext();
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

        public IDisposable MakeCurrent()
        {
            Context.MakeCurrent();
            // HACK: OSX backend haven't switched to the new model, so there are only 2 pre-created contexts now
            return Disposable.Empty;
        }

        public void Dispose()
        {
            // HACK: OSX backend haven't switched to the new model, so there are only 2 pre-created contexts now
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
