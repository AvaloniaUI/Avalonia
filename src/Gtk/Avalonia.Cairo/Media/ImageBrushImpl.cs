using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.Utilities;
using global::Cairo;

namespace Avalonia.Cairo.Media
{
    public class ImageBrushImpl : BrushImpl
    {
        public ImageBrushImpl(
            ITileBrush brush,
            IBitmapImpl bitmap,
            Size targetSize)
        {
            var calc = new TileBrushCalculator(brush, new Size(bitmap.PixelWidth, bitmap.PixelHeight), targetSize);

            using (var intermediate = new ImageSurface(Format.ARGB32, (int)calc.IntermediateSize.Width, (int)calc.IntermediateSize.Height))
            {
                using (var context = new RenderTarget(intermediate).CreateDrawingContext(null))
                {
                    var rect = new Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight);

                    context.Clear(Colors.Transparent);
                    context.PushClip(calc.IntermediateClip);
                    context.Transform = calc.IntermediateTransform;
                    context.DrawImage(bitmap, 1, rect, rect);
                    context.PopClip();
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

