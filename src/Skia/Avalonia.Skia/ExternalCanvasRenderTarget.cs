using Avalonia.Platform;
using Avalonia.Rendering;

namespace Avalonia.Skia
{
    internal class ExternalCanvasRenderTarget : IRenderTarget
    {
        private IExternalCanvasSurface _surface;

        public ExternalCanvasRenderTarget(IExternalCanvasSurface canvas)
        {
            _surface = canvas;
        }

        public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
        {
            var createInfo = new DrawingContextImpl.CreateInfo
            {
                Canvas = _surface.Canvas,
                Dpi = _surface.Dpi,
                VisualBrushRenderer = visualBrushRenderer,
                DisableTextLcdRendering = true,
            };

            return new DrawingContextImpl(createInfo);
        }

        public void Dispose()
        {
        }
    }
}
