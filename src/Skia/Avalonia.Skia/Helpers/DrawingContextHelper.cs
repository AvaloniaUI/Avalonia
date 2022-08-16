using Avalonia.Platform;
using Avalonia.Rendering;
using SkiaSharp;

namespace Avalonia.Skia.Helpers
{
    public static class DrawingContextHelper
    {
        /// <summary>
        /// Wrap Skia canvas in drawing context so we can use Avalonia api to render to external skia canvas
        /// this is useful in scenarios where canvas is not controlled by application, but received from another non avalonia api
        /// like: SKCanvas canvas = SKDocument.BeginPage(...);
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="dpi"></param>
        /// <param name="visualBrushRenderer"></param>
        /// <returns>DrawingContext</returns>
        public static IDrawingContextImpl WrapSkiaCanvas(SKCanvas canvas, Vector dpi, IVisualBrushRenderer visualBrushRenderer = null)
        {
            var createInfo = new DrawingContextImpl.CreateInfo
            {
                Canvas = canvas,
                Dpi = dpi,
                VisualBrushRenderer = visualBrushRenderer,
                DisableTextLcdRendering = true,
            };

            return new DrawingContextImpl(createInfo);
        }
        
    }
}
