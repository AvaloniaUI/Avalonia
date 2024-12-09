using Avalonia.Platform;

namespace Avalonia.Skia
{
    /// <summary>
    /// Adapts <see cref="ISkiaGpuRenderTarget"/> to be used within our rendering pipeline.
    /// </summary>
    internal class SkiaGpuRenderTarget : IRenderTarget2
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

        public IDrawingContextImpl CreateDrawingContext(PixelSize expectedPixelSize,
            out RenderTargetDrawingContextProperties properties) =>
            CreateDrawingContextCore(expectedPixelSize, false, out properties);

        public IDrawingContextImpl CreateDrawingContext(bool useScaledDrawing)
            => CreateDrawingContextCore(null, useScaledDrawing, out _);
        
        
        IDrawingContextImpl CreateDrawingContextCore(PixelSize? expectedPixelSize,
            bool useScaledDrawing,
            out RenderTargetDrawingContextProperties properties)
        {
            properties = default;
            var session =
                expectedPixelSize.HasValue && _renderTarget is ISkiaGpuRenderTarget2 target2
                    ? target2.BeginRenderingSession(expectedPixelSize.Value)
                    : _renderTarget.BeginRenderingSession();

            var nfo = new DrawingContextImpl.CreateInfo
            {
                GrContext = session.GrContext,
                Surface = session.SkSurface,
                Dpi = SkiaPlatform.DefaultDpi * session.ScaleFactor,
                ScaleDrawingToDpi = useScaledDrawing,
                Gpu = _skiaGpu,
                CurrentSession =  session
            };

            return new DrawingContextImpl(nfo, session);
        }

        public bool IsCorrupted => _renderTarget.IsCorrupted;
        public RenderTargetProperties Properties { get; }


    }
}
