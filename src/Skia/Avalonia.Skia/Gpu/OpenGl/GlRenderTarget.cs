using System;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Surfaces;
using SkiaSharp;
using static Avalonia.OpenGL.GlConsts;

namespace Avalonia.Skia
{
    internal class GlRenderTarget : ISkiaGpuRenderTarget2
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
                
                SurfaceOrigin = glSession.IsYFlipped ? GRSurfaceOrigin.TopLeft : GRSurfaceOrigin.BottomLeft;
            }
            public void Dispose()
            {
                _surface.Canvas.Flush();
                _surface.Dispose();
                _backendRenderTarget.Dispose();
                GrContext.Flush();
                _glSession.Dispose();
            }
            
            public GRSurfaceOrigin SurfaceOrigin { get; }

            public GRContext GrContext { get; }
            public SKSurface SkSurface => _surface;
            public double ScaleFactor => _glSession.Scaling;
        }

        public ISkiaGpuRenderSession BeginRenderingSession(PixelSize size) => BeginRenderingSessionCore(size);
        public ISkiaGpuRenderSession BeginRenderingSession() => BeginRenderingSessionCore(null);
        
        ISkiaGpuRenderSession BeginRenderingSessionCore(PixelSize? expectedSize)
        {
            IGlPlatformSurfaceRenderingSession glSession =
                expectedSize != null && _surface is IGlPlatformSurfaceRenderTarget2 surface2
                    ? surface2.BeginDraw(expectedSize.Value)
                    : _surface.BeginDraw();

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
                    glSession.Dispose();
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

                    // Apply rotation to the canvas if supported bt the backend and it's not the native hardware orientation
                    if (glSession is ISurfaceOrientation orientation && orientation.Orientation != SurfaceOrientation.Normal)
                    { 
                        var canvas = surface.Canvas;
                        var width = size.Width;
                        var height = size.Height;
                        canvas.Translate(width / 2, height / 2);
                        canvas.RotateDegrees(orientation.Orientation switch
                        {
                            SurfaceOrientation.Rotated90 => 90,
                            SurfaceOrientation.Rotated180 => 180,
                            SurfaceOrientation.Rotated270 => -90,
                            _ => 0
                        });
                        canvas.Translate(orientation.Orientation switch
                        {
                            SurfaceOrientation.Rotated180 => new SKPoint(-width / 2, -height / 2),
                            SurfaceOrientation.Rotated90 => new SKPoint(-height / 2, -width / 2),
                            SurfaceOrientation.Rotated270 => new SKPoint(-height / 2, -width / 2),
                            _ => new SKPoint()
                        });
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
