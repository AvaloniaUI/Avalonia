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

        public IDrawingContextImpl CreateDrawingContext()
        {
            var session = _renderTarget.BeginRenderingSession();

            var nfo = new DrawingContextImpl.CreateInfo
            {
                GrContext = session.GrContext,
                Surface = session.SkSurface,
                Dpi = SkiaPlatform.DefaultDpi * session.ScaleFactor,
                Gpu = _skiaGpu,
                CurrentSession =  session
            };

            return new DrawingContextImpl(nfo, session);
        }

        public bool IsCorrupted => _renderTarget.IsCorrupted;
    }
}
