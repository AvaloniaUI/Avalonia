using Avalonia.Platform;
using Avalonia.Rendering;

namespace Avalonia.Skia
{
    /// <summary>
    /// Adapts <see cref="ISkiaGpuRenderTarget"/> to be used within our rendering pipeline.
    /// </summary>
    internal class SkiaGpuRenderTarget : IRenderTargetWithCorruptionInfo
    {
        private readonly ISkiaGpuRenderTarget _renderTarget;

        public SkiaGpuRenderTarget(ISkiaGpuRenderTarget renderTarget)
        {
            _renderTarget = renderTarget;
        }

        public void Dispose()
        {
            _renderTarget.Dispose();
        }

        public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
        {
            var session = _renderTarget.BeginRenderingSession();

            var nfo = new DrawingContextImpl.CreateInfo
            {
                GrContext = session.GrContext,
                Surface = session.SkSurface,
                Dpi = SkiaPlatform.DefaultDpi * session.ScaleFactor,
                VisualBrushRenderer = visualBrushRenderer,
                DisableTextLcdRendering = true
            };

            return new DrawingContextImpl(nfo, session);
        }

        public bool IsCorrupted => _renderTarget.IsCorrupted;
    }
}
