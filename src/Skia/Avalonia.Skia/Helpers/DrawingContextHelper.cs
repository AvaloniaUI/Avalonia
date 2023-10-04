using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Rendering;
using SkiaSharp;

namespace Avalonia.Skia.Helpers
{
    public static class DrawingContextHelper
    {
        /// <inheritdoc cref="RenderAsync(SKCanvas, Visual, Rect, Vector)"/>.
        public static Task RenderAsync(SKCanvas canvas, Visual visual)
        {
            return RenderAsync(canvas, visual, visual.Bounds, SkiaPlatform.DefaultDpi);
        }

        /// <summary>
        /// Renders Avalonia visual into a SKCanvas instance.
        /// This is useful in scenarios where canvas is not controlled by application, but received from another non avalonia api
        /// like: SKCanvas canvas = SKDocument.BeginPage(...);
        /// </summary>
        /// <param name="canvas">Skia canvas to render into.</param>
        /// <param name="visual">Avalonia visual.</param>
        /// <param name="clipRect">Clipping rectangle.</param>
        /// <param name="dpi">Dpi of drawings.</param>
        public static Task RenderAsync(SKCanvas canvas, Visual visual, Rect clipRect, Vector dpi)
        {
            using var drawingContextImpl = WrapSkiaCanvas(canvas, dpi);
            using var drawingContext = new PlatformDrawingContext(drawingContextImpl, false);
            ImmediateRenderer.Render(drawingContext, visual, clipRect);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Wrap Skia canvas in drawing context so we can use Avalonia api to render to external skia canvas
        /// this is useful in scenarios where canvas is not controlled by application, but received from another non avalonia api
        /// like: SKCanvas canvas = SKDocument.BeginPage(...);
        /// </summary>
        /// <param name="canvas">Skia canvas to render into.</param>
        /// <param name="dpi"></param>
        /// <returns>DrawingContext</returns>
        [Unstable("IDrawingContextImpl usage is not supported in Avalonia 11.0.")]
        public static IDrawingContextImpl WrapSkiaCanvas(SKCanvas canvas, Vector dpi)
        {
            var createInfo = new DrawingContextImpl.CreateInfo
            {
                Canvas = canvas,
                Dpi = dpi,
                DisableSubpixelTextRendering = true,
            };

            return new DrawingContextImpl(createInfo);
        }
        
    }
}
