// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Media;
using Avalonia.Rendering.Utilities;
using Cairo;

namespace Avalonia.Cairo.Media
{
    public class DrawingBrushImpl : BrushImpl
    {
        public DrawingBrushImpl(
            ITileBrush brush,
            IDrawing drawing,
            Size targetSize)
        {
            var bounds = drawing.GetBounds();
            var calc = new TileBrushCalculator(brush, new Size(bounds.Width, bounds.Height), targetSize);

            using (var intermediate = new ImageSurface(Format.ARGB32, (int)calc.IntermediateSize.Width, (int)calc.IntermediateSize.Height))
            {
                var context = new RenderTarget(intermediate).CreateDrawingContext(null);
                context.Clear(Colors.Transparent);
                using (var drawingContext = new Avalonia.Media.DrawingContext(context))
                {
                    drawingContext.PushClip(calc.IntermediateClip);
                    drawingContext.PushPreTransform(calc.IntermediateTransform);
                    drawing.Draw(drawingContext);
                }

                var result = new SurfacePattern(intermediate);

                if ((brush.TileMode & TileMode.FlipXY) != 0)
                {
                    // TODO: Currently always FlipXY as that's all cairo supports natively. 
                    // Support separate FlipX and FlipY by drawing flipped images to intermediate
                    // surface.
                    result.Extend = Extend.Reflect;
                }
                else
                {
                    result.Extend = Extend.Repeat;
                }

                if (brush.TileMode != TileMode.None)
                {
                    var matrix = result.Matrix;
                    matrix.InitTranslate(-calc.DestinationRect.X, -calc.DestinationRect.Y);
                    result.Matrix = matrix;
                }

                PlatformBrush = result;
            }
        }
    }
}