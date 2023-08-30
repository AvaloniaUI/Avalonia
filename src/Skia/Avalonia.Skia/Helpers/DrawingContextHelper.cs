using System.Diagnostics.CodeAnalysis;
using Avalonia.Media;
using Avalonia.Platform;
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
        /// <returns>DrawingContext</returns>
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
        
        public static bool TryCreateDashEffect(IPen? pen, [NotNullWhen(true)] out SKPathEffect? effect)
        {
            if (pen?.DashStyle?.Dashes != null && pen.DashStyle.Dashes.Count > 0)
            {
                var srcDashes = pen.DashStyle.Dashes;

                var count = srcDashes.Count % 2 == 0 ? srcDashes.Count : srcDashes.Count * 2;

                var dashesArray = new float[count];

                for (var i = 0; i < count; ++i)
                {
                    dashesArray[i] = (float)srcDashes[i % srcDashes.Count] * (float)pen.Thickness;
                }

                var offset = (float)(pen.DashStyle.Offset * pen.Thickness);
                effect = SKPathEffect.CreateDash(dashesArray, offset);
                return true;
            }

            effect = null;
            return false;
        }
    }
}
