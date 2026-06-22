using System;
using Avalonia.Platform;

namespace Avalonia.Skia
{
    /// <summary>
    /// Adapts <see cref="ISkiaGpuRenderTarget"/> to be used within our rendering pipeline.
    /// </summary>
    internal class SkiaGpuRenderTarget : IRenderTarget
    {
        private readonly ISkiaGpu _skiaGpu;
        private readonly ISkiaGpuRenderTarget _renderTarget;

        public SkiaGpuRenderTarget(ISkiaGpu skiaGpu, ISkiaGpuRenderTarget renderTarget)
        {
            _skiaGpu = skiaGpu;
            _renderTarget = renderTarget;
        }

        public void Dispose()
        {
            _renderTarget.Dispose();
        }

        public IDrawingContextImpl CreateDrawingContext(bool useScaledDrawing) => throw new InvalidOperationException(
            "This legacy API is only supported by framebuffer render targets and should be removed from there as well, don't use");

        public IDrawingContextImpl CreateDrawingContext(IRenderTarget.RenderTargetSceneInfo sceneInfo,
            out RenderTargetDrawingContextProperties properties)
        {
            properties = default;
            var session = _renderTarget.BeginRenderingSession(sceneInfo);

            var nfo = new DrawingContextImpl.CreateInfo
            {
                GrContext = session.GrContext,
                Surface = session.SkSurface,
                Dpi = SkiaPlatform.DefaultDpi * session.ScaleFactor,
                ScaleDrawingToDpi = false,
                Gpu = _skiaGpu,
                CurrentSession =  session
            };

            return new DrawingContextImpl(nfo, session);
        }
        
        public PlatformRenderTargetState PlatformRenderTargetState => _renderTarget.State;
        public RenderTargetProperties Properties { get; }


    }
}
