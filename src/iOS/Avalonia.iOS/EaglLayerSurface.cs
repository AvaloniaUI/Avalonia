
using System;
using System.Threading;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Surfaces;
using CoreAnimation;

namespace Avalonia.iOS
{
    class EaglLayerSurface : IGlPlatformSurface
    {
        private readonly CAEAGLLayer _layer;

        public EaglLayerSurface(CAEAGLLayer layer)
        {
            _layer = layer;
        }

        class RenderSession : IGlPlatformSurfaceRenderingSession
        {
            private readonly GlContext _ctx;
            private readonly IDisposable _restoreContext;
            private readonly SizeSynchronizedLayerFbo _fbo;

            public RenderSession(GlContext ctx, IDisposable restoreContext, SizeSynchronizedLayerFbo fbo)
            {
                _ctx = ctx;
                _restoreContext = restoreContext;
                _fbo = fbo;
                Size = new PixelSize(_fbo.Width, _fbo.Height);
                Scaling = _fbo.Scaling;
                Context = ctx;
            }

            public void Dispose()
            {
                _ctx.GlInterface.Finish();
                _fbo.Present();
                _restoreContext.Dispose();
            }

            public IGlContext Context { get; }
            public PixelSize Size { get; }
            public double Scaling { get; }
            public bool IsYFlipped { get; }
        }

        class RenderTarget : IGlPlatformSurfaceRenderTarget
        {
            private readonly GlContext _ctx;
            private readonly SizeSynchronizedLayerFbo _fbo;

            public RenderTarget(GlContext ctx, SizeSynchronizedLayerFbo fbo)
            {
                _ctx = ctx;
                _fbo = fbo;
            }

            public void Dispose()
            {
                CheckThread();
                using (_ctx.MakeCurrent())
                    _fbo.Dispose();
            }

            public IGlPlatformSurfaceRenderingSession BeginDraw()
            {
                CheckThread();
                var restoreContext = _ctx.MakeCurrent();
                _fbo.Bind();
                return new RenderSession(_ctx, restoreContext, _fbo);
            }
        }

        static void CheckThread()
        {
            if (Platform.Timer.TimerThread != Thread.CurrentThread)
                throw new InvalidOperationException("Invalid thread, go away");
        }
        
        public IGlPlatformSurfaceRenderTarget CreateGlRenderTarget()
        {
            CheckThread();
            var ctx = Platform.GlFeature.Context;
            using (ctx.MakeCurrent())
            {
                var fbo = new SizeSynchronizedLayerFbo(ctx.Context, ctx.GlInterface, _layer);
                if (!fbo.Sync())
                    throw new InvalidOperationException("Unable to create render target");
                return new RenderTarget(ctx, fbo);
            }
        }
    }
}
