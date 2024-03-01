using Avalonia.Media;
using Avalonia.Rendering.Utilities;
using Avalonia.Utilities;
using SharpDX.Direct2D1;

namespace Avalonia.Direct2D1.Media
{
    internal sealed class ImageBrushImpl : BrushImpl
    {
        private readonly OptionalDispose<Bitmap1> _bitmap;

        public ImageBrushImpl(
            ITileBrush brush,
            SharpDX.Direct2D1.RenderTarget target,
            BitmapImpl bitmap,
            Rect destinationRect)
        {
            var dpi = new Vector(target.DotsPerInch.Width, target.DotsPerInch.Height);
            var calc = new TileBrushCalculator(brush, bitmap.PixelSize.ToSizeWithDpi(dpi), destinationRect.Size);

            Vector brushOffset = default;
            if (brush.DestinationRect.Unit == RelativeUnit.Relative)
                brushOffset = new Vector(destinationRect.X, destinationRect.Y);
            
            if (!calc.NeedsIntermediate)
            {
                _bitmap = bitmap.GetDirect2DBitmap(target);
                PlatformBrush = new BitmapBrush(
                    target,
                    _bitmap.Value,
                    GetBitmapBrushProperties(brush),
                    GetBrushProperties(brush, calc.DestinationRect, brushOffset));
            }
            else
            {
                using (var intermediate = RenderIntermediate(target, bitmap, calc))
                {
                    PlatformBrush = new BitmapBrush(
                        target,
                        intermediate.Bitmap,
                        GetBitmapBrushProperties(brush),
                        GetBrushProperties(brush, calc.DestinationRect, brushOffset));
                }
            }
        }

        public override void Dispose()
        {
            _bitmap.Dispose();
            base.Dispose();
        }

        private static BitmapBrushProperties GetBitmapBrushProperties(ITileBrush brush)
        {
            var tileMode = brush.TileMode;

            return new BitmapBrushProperties
            {
                ExtendModeX = GetExtendModeX(tileMode),
                ExtendModeY = GetExtendModeY(tileMode),
            };
        }

        private static BrushProperties GetBrushProperties(ITileBrush brush, Rect destinationRect, Vector offset)
        {
            var tileTransform =
                brush.TileMode != TileMode.None ?
                Matrix.CreateTranslation(destinationRect.X, destinationRect.Y) :
                Matrix.Identity;

            if (offset != default)
                tileTransform = Matrix.CreateTranslation(offset);

            return new BrushProperties
            {
                Opacity = (float)brush.Opacity,
                Transform = tileTransform.ToDirect2D(),
            };
        }

        private static ExtendMode GetExtendModeX(TileMode tileMode)
        {
            return (tileMode & TileMode.FlipX) != 0 ? ExtendMode.Mirror : ExtendMode.Wrap;
        }

        private static ExtendMode GetExtendModeY(TileMode tileMode)
        {
            return (tileMode & TileMode.FlipY) != 0 ? ExtendMode.Mirror : ExtendMode.Wrap;
        }

        private BitmapRenderTarget RenderIntermediate(
            SharpDX.Direct2D1.RenderTarget target,
            BitmapImpl bitmap,
            TileBrushCalculator calc)
        {
            var result = new BitmapRenderTarget(
                target,
                CompatibleRenderTargetOptions.None,
                calc.IntermediateSize.ToSharpDX());

            using (var context = new RenderTarget(result).CreateDrawingContext())
            {
                var dpi = new Vector(target.DotsPerInch.Width, target.DotsPerInch.Height);
                var rect = new Rect(bitmap.PixelSize.ToSizeWithDpi(dpi));

                context.Clear(Colors.Transparent);
                context.PushClip(calc.IntermediateClip);
                context.Transform = calc.IntermediateTransform;
                context.DrawBitmap(bitmap, 1, rect, rect);
                context.PopClip();
            }

            return result;
        }
    }
}
