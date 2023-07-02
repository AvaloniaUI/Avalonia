using System;
using System.Collections.Generic;
using Avalonia.OpenGL;
using Avalonia.Native.Interop;
using System.Drawing;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Avalonia.Native
{
    class AvaloniaNativeGlPlatformGraphics : IPlatformGraphics
    {
        private readonly IAvnGlDisplay _display;

        public AvaloniaNativeGlPlatformGraphics(IAvnGlDisplay display)
        {
            _display = display;
            var context = display.CreateContext(null);
            
            int major, minor;
            GlInterface glInterface;
            using (context.MakeCurrent())
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

            GlDisplay = new GlDisplay(display, glInterface, context.SampleCount, context.StencilSize);
            SharedContext =(GlContext)CreateContext();
        }

        

        public bool UsesSharedContext => true;
        public IPlatformGraphicsContext CreateContext() => new GlContext(GlDisplay,
            null, _display.CreateContext(null), _version);

        public IPlatformGraphicsContext GetSharedContext() => SharedContext;

        public bool CanShareContexts => true;
        public bool CanCreateContexts => true;
        internal GlDisplay GlDisplay;
        private readonly GlVersion _version;
        internal GlContext SharedContext { get; }
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

        public GlContext CreateSharedContext(GlContext share) =>
            new GlContext(this, share, _display.CreateContext(share.Context), share.Version);

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
        public IDisposable MakeCurrent()
        {
            if (IsLost)
                throw new PlatformGraphicsContextLostException();
            return Context.MakeCurrent();
        }

        public bool IsLost => Context == null;
        public IDisposable EnsureCurrent() => MakeCurrent();

        public bool IsSharedWith(IGlContext context)
        {
            var c = (GlContext)context;
            return c == this
                   || c._sharedWith == this
                   || _sharedWith == context
                   || _sharedWith != null && _sharedWith == c._sharedWith;
        }

        public bool CanCreateSharedContext => true;

        public IGlContext CreateSharedContext(IEnumerable<GlVersion> preferredVersions = null) =>
            _display.CreateSharedContext(_sharedWith ?? this);

        public void Dispose()
        {
            Context.Dispose();
            Context = null;
        }

        public object TryGetFeature(Type featureType) => null;
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
        public GlPlatformSurface(IAvnWindowBase window)
        {
            _window = window;
        }
        
        public IGlPlatformSurfaceRenderTarget CreateGlRenderTarget(IGlContext context)
        {
            if (!Dispatcher.UIThread.CheckAccess())
                throw new RenderTargetNotReadyException();
            var avnContext = (GlContext)context;
            return new GlPlatformSurfaceRenderTarget(_window.CreateGlRenderTarget(avnContext.Context), avnContext);
        }

    }
}
