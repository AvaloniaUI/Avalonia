using System;
using Avalonia.OpenGL;
using Avalonia.Native.Interop;
using System.Drawing;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Avalonia.Native
{
    class AvaloniaNativePlatformOpenGlInterface : IPlatformOpenGlInterface
    {
        private readonly IAvnGlDisplay _display;

        public AvaloniaNativePlatformOpenGlInterface(IAvnGlDisplay display)
        {
            _display = display;
            var immediate = display.CreateContext(null);
            
            int major, minor;
            GlInterface glInterface;
            using (immediate.MakeCurrent())
            {
                var basic = new GlBasicInfoInterface(display.GetProcAddress);
                basic.GetIntegerv(GlConsts.GL_MAJOR_VERSION, out major);
                basic.GetIntegerv(GlConsts.GL_MINOR_VERSION, out minor);
                _version = new GlVersion(GlProfileType.OpenGL, major, minor);
                glInterface = new GlInterface(_version, (name) =>
                {
                    var rv = _display.GetProcAddress(name);
                    return rv;
                });
            }

            GlDisplay = new GlDisplay(display, glInterface, immediate.SampleCount, immediate.StencilSize);
            MainContext = new GlContext(GlDisplay, null, immediate, _version);
        }

        internal GlContext MainContext { get; }
        public IGlContext PrimaryContext => MainContext;
        IPlatformGpuContext IPlatformGpu.PrimaryContext => PrimaryContext;
        
        public bool CanShareContexts => true;
        public bool CanCreateContexts => true;
        internal GlDisplay GlDisplay;
        private readonly GlVersion _version;

        public IGlContext CreateSharedContext() => new GlContext(GlDisplay,
            MainContext, _display.CreateContext(MainContext.Context), _version);

        public IGlContext CreateContext() => new GlContext(GlDisplay,
            null, _display.CreateContext(null), _version);
    }

    class GlDisplay
    {
        private readonly IAvnGlDisplay _display;

        public GlDisplay(IAvnGlDisplay display, GlInterface glInterface, int sampleCount, int stencilSize)
        {
            _display = display;
            SampleCount = sampleCount;
            StencilSize = stencilSize;
            GlInterface = glInterface;
        }

        public GlInterface GlInterface { get; }

        public int SampleCount { get; }

        public int StencilSize { get; }

        public void ClearContext() => _display.LegacyClearCurrentContext();
    }

    class GlContext : IGlContext
    {
        private readonly GlDisplay _display;
        private readonly GlContext _sharedWith;
        public IAvnGlContext Context { get; private set; }

        public GlContext(GlDisplay display, GlContext sharedWith, IAvnGlContext context, GlVersion version)
        {
            _display = display;
            _sharedWith = sharedWith;
            Context = context;
            Version = version;
        }

        public GlVersion Version { get; }
        public GlInterface GlInterface => _display.GlInterface;
        public int SampleCount => _display.SampleCount;
        public int StencilSize => _display.StencilSize;
        public IDisposable MakeCurrent() => Context.MakeCurrent();
        public IDisposable EnsureCurrent() => MakeCurrent();

        public bool IsSharedWith(IGlContext context)
        {
            var c = (GlContext)context;
            return c == this
                   || c._sharedWith == this
                   || _sharedWith == context
                   || _sharedWith != null && _sharedWith == c._sharedWith;
        }


        public void Dispose()
        {
            Context.Dispose();
            Context = null;
        }
    }


    class GlPlatformSurfaceRenderTarget : IGlPlatformSurfaceRenderTarget
    {
        private IAvnGlSurfaceRenderTarget _target;
        private readonly IGlContext _context;

        public GlPlatformSurfaceRenderTarget(IAvnGlSurfaceRenderTarget target, IGlContext context)
        {
            _target = target;
            _context = context;
        }

        public IGlPlatformSurfaceRenderingSession BeginDraw()
        {
            var feature = (AvaloniaNativePlatformOpenGlInterface)AvaloniaLocator.Current.GetService<IPlatformOpenGlInterface>();
            return new GlPlatformSurfaceRenderingSession(_context, _target.BeginDrawing());
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

        public GlPlatformSurfaceRenderingSession(IGlContext context, IAvnGlSurfaceRenderingSession session)
        {
            Context = context;
            _session = session;
        }

        public IGlContext Context { get; }

        public PixelSize Size
        {
            get
            {
                var s = _session.PixelSize;
                return new PixelSize(s.Width, s.Height);
            }
        }

        public double Scaling => _session.Scaling;


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
        private readonly IGlContext _context;

        public GlPlatformSurface(IAvnWindowBase window, IGlContext context)
        {
            _window = window;
            _context = context;
        }
        public IGlPlatformSurfaceRenderTarget CreateGlRenderTarget()
        {
            return new GlPlatformSurfaceRenderTarget(_window.CreateGlRenderTarget(), _context);
        }

    }
}
