using System;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Platform;
using SkiaSharp;
using static Avalonia.OpenGL.GlConsts;

namespace Avalonia.Skia
{
    internal class GlRenderTarget : ISkiaGpuRenderTarget
    {
        private readonly GRContext _grContext;
        private IGlPlatformSurfaceRenderTarget _surface;
        private static readonly SKSurfaceProperties _surfaceProperties = new SKSurfaceProperties(SKPixelGeometry.RgbHorizontal);
        public GlRenderTarget(GRContext grContext, IGlContext glContext, IGlPlatformSurface glSurface)
        {
            _grContext = grContext;
            using (glContext.EnsureCurrent())
                _surface = glSurface.CreateGlRenderTarget(glContext);
        }

        public void Dispose() => _surface.Dispose();

        public PlatformRenderTargetState State => _surface.State;
        public RenderTargetProperties Properties => _surface.Properties;

        class GlGpuSession : ISkiaGpuRenderSessionWithPartialPresentation, IPartialPresentationFeature
        {
            private readonly GRBackendRenderTarget _backendRenderTarget;
            private readonly SKSurface _surface;
            private readonly IGlPlatformSurfaceRenderingSession _glSession;
            private readonly IPartialPresentationFeature? _partialPresentation;
            private bool _succeeded;

            public GlGpuSession(GRContext grContext,
                GRBackendRenderTarget backendRenderTarget,
                SKSurface surface,
                IGlPlatformSurfaceRenderingSession glSession)
            {
                GrContext = grContext;
                _backendRenderTarget = backendRenderTarget;
                _surface = surface;
                _glSession = glSession;
                _partialPresentation =
                    (glSession as IGlPlatformSurfaceRenderingSessionWithPartialPresentation)?.PartialPresentation;
                
                SurfaceOrigin = glSession.IsYFlipped ? GRSurfaceOrigin.TopLeft : GRSurfaceOrigin.BottomLeft;
            }
            public void Dispose()
            {
                try
                {
                    using (_backendRenderTarget)
                    using (_surface)
                        _surface.Canvas.Flush();
                    GrContext.Flush();
                    // Flush can fail during disposal, after the caller finished drawing.
                    if (_succeeded)
                        _partialPresentation?.Succeeded();
                }
                finally
                {
                    _glSession.Dispose();
                }
            }

            public IPartialPresentationFeature? PartialPresentation => _partialPresentation != null ? this : null;

            public IPartialPresentationBuffer? Buffer => _partialPresentation?.Buffer;

            public void SetDirtyRects(ReadOnlySpan<PixelRect> frameDirtyRects, ReadOnlySpan<PixelRect> bufferDirtyRects) =>
                _partialPresentation!.SetDirtyRects(frameDirtyRects, bufferDirtyRects);

            public void Succeeded() => _succeeded = true;
            
            public GRSurfaceOrigin SurfaceOrigin { get; }

            public GRContext GrContext { get; }
            public SKSurface SkSurface => _surface;
            public double ScaleFactor => _glSession.Scaling;
        }
        
        public ISkiaGpuRenderSession BeginRenderingSession(IRenderTarget.RenderTargetSceneInfo sceneInfo)
        {
            var glSession = _surface.BeginDraw(sceneInfo);
            
            bool success = false;
            try
            {
                var disp = glSession.Context;
                var gl = disp.GlInterface;
                gl.GetIntegerv(GL_FRAMEBUFFER_BINDING, out var fb);

                var size = glSession.Size;
                var colorType = SKColorType.Rgba8888;
                var scaling = glSession.Scaling;
                if (size.Width <= 0 || size.Height <= 0 || scaling < 0)
                {
                    throw new InvalidOperationException(
                        $"Can't create drawing context for surface with {size} size and {scaling} scaling");
                }

                lock (_grContext)
                {
                    _grContext.ResetContext();

                    var samples = disp.SampleCount;
                    var maxSamples = _grContext.GetMaxSurfaceSampleCount(colorType);
                    if (samples > maxSamples)
                        samples = maxSamples;

                    var glInfo = new GRGlFramebufferInfo((uint)fb, colorType.ToGlSizedFormat());
                    var renderTarget = new GRBackendRenderTarget(size.Width, size.Height, samples, disp.StencilSize, glInfo);
                    var surface = SKSurface.Create(_grContext, renderTarget,
                        glSession.IsYFlipped ? GRSurfaceOrigin.TopLeft : GRSurfaceOrigin.BottomLeft,
                        colorType, _surfaceProperties);
                    if (surface == null)
                    {
                        renderTarget.Dispose();
                        throw new InvalidOperationException("Unable to wrap the OpenGL framebuffer.");
                    }

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
