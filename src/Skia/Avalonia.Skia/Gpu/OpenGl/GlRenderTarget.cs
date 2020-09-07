using System;
using System.Reactive.Disposables;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Rendering;
using SkiaSharp;
using static Avalonia.OpenGL.GlConsts;

namespace Avalonia.Skia
{
    internal class GlRenderTarget : ISkiaGpuRenderTarget
    {
        private readonly GRContext _grContext;
        private IGlPlatformSurfaceRenderTarget _surface;

        public GlRenderTarget(GRContext grContext, IGlPlatformSurface glSurface)
        {
            _grContext = grContext;
            _surface = glSurface.CreateGlRenderTarget();
        }

        public void Dispose() => _surface.Dispose();

        public bool IsCorrupted => (_surface as IGlPlatformSurfaceRenderTargetWithCorruptionInfo)?.IsCorrupted == true;

        class GlGpuSession : ISkiaGpuRenderSession
        {
            private readonly GRBackendRenderTarget _backendRenderTarget;
            private readonly SKSurface _surface;
            private readonly IGlPlatformSurfaceRenderingSession _glSession;

            public GlGpuSession(GRContext grContext,
                GRBackendRenderTarget backendRenderTarget,
                SKSurface surface,
                IGlPlatformSurfaceRenderingSession glSession)
            {
                GrContext = grContext;
                _backendRenderTarget = backendRenderTarget;
                _surface = surface;
                _glSession = glSession;
            }
            public void Dispose()
            {
                _surface.Canvas.Flush();
                _surface.Dispose();
                _backendRenderTarget.Dispose();
                GrContext.Flush();
                _glSession.Dispose();
            }

            public GRContext GrContext { get; }
            public SKSurface SkSurface => _surface;
            public double ScaleFactor => _glSession.Scaling;
        }

        public ISkiaGpuRenderSession BeginRenderingSession()
        {
            var glSession = _surface.BeginDraw();
            bool success = false;
            try
            {
                var disp = glSession.Context;
                var gl = disp.GlInterface;
                gl.GetIntegerv(GL_FRAMEBUFFER_BINDING, out var fb);

                var size = glSession.Size;
                var scaling = glSession.Scaling;
                if (size.Width <= 0 || size.Height <= 0 || scaling < 0)
                {
                    glSession.Dispose();
                    throw new InvalidOperationException(
                        $"Can't create drawing context for surface with {size} size and {scaling} scaling");
                }

                gl.Viewport(0, 0, size.Width, size.Height);
                gl.ClearStencil(0);
                gl.ClearColor(0, 0, 0, 0);
                gl.Clear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
                lock (_grContext)
                {
                    _grContext.ResetContext();

                    var renderTarget =
                        new GRBackendRenderTarget(size.Width, size.Height, disp.SampleCount, disp.StencilSize,
                            new GRGlFramebufferInfo((uint)fb, SKColorType.Rgba8888.ToGlSizedFormat()));
                    var surface = SKSurface.Create(_grContext, renderTarget,
                        glSession.IsYFlipped ? GRSurfaceOrigin.TopLeft : GRSurfaceOrigin.BottomLeft,
                        SKColorType.Rgba8888);

                    success = true;
                    return new GlGpuSession(_grContext, renderTarget, surface, glSession);
                }
            }
            finally
            {
                if (!success)
                    glSession.Dispose();
            }
        }
    }
}
