using System;
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
        
        /// <summary>
        /// Unsupported - Wraps a GPU Backed SkiaSurface in an Avalonia DrawingContext.
        /// </summary>
        [Obsolete]
        public static IDrawingContextImpl WrapSkiaSurface(this SKSurface surface, GRContext grContext, Vector dpi, params IDisposable[] disposables)
        {
            var createInfo = new DrawingContextImpl.CreateInfo
            {
                GrContext = grContext,
                Surface = surface,
                Dpi = dpi,
                DisableTextLcdRendering = false,
            };

            return new DrawingContextImpl(createInfo, disposables);
        }
        
        /// <summary>
        /// Unsupported - Wraps a non-GPU Backed SkiaSurface in an Avalonia DrawingContext.
        /// </summary>
        [Obsolete]
        public static IDrawingContextImpl WrapSkiaSurface(this SKSurface surface, Vector dpi, params IDisposable[] disposables)
        {
            var createInfo = new DrawingContextImpl.CreateInfo
            {
                Surface = surface,
                Dpi = dpi,
                DisableTextLcdRendering = false,
            };

            return new DrawingContextImpl(createInfo, disposables);
        }
        
        [Obsolete]
        public static void DrawTo(this ISkiaDrawingContextImpl source, ISkiaDrawingContextImpl destination, SKPaint paint = null)
        {
            destination.SkCanvas.DrawSurface(source.SkSurface, new SKPoint(0, 0), paint);
        }
    }
}
