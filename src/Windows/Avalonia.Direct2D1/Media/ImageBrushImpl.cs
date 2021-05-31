using Avalonia.Media;
using Avalonia.Rendering.Utilities;
using Avalonia.Utilities;
using Vortice.Direct2D1;

namespace Avalonia.Direct2D1.Media
{
    public sealed class ImageBrushImpl : BrushImpl
    {
        private readonly OptionalDispose<ID2D1Bitmap> _bitmap;

        private readonly Visuals.Media.Imaging.BitmapInterpolationMode _bitmapInterpolationMode;

        public ImageBrushImpl(
            ITileBrush brush,
            ID2D1RenderTarget target,
            BitmapImpl bitmap,
            Size targetSize)
        {
            var dpi = new Vector(target.Dpi.X, target.Dpi.Y);
            var calc = new TileBrushCalculator(brush, bitmap.PixelSize.ToSizeWithDpi(dpi), targetSize);

            if (!calc.NeedsIntermediate)
            {
                _bitmap = bitmap.GetDirect2DBitmap(target);
                PlatformBrush = target.CreateBitmapBrush(
                    _bitmap.Value,
                    GetBitmapBrushProperties(brush),
                    GetBrushProperties(brush, calc.DestinationRect));
            }
            else
            {
                using (var intermediate = RenderIntermediate(target, bitmap, calc))
                {
                    PlatformBrush = target.CreateBitmapBrush(
                        intermediate.Bitmap,
                        GetBitmapBrushProperties(brush),
                        GetBrushProperties(brush, calc.DestinationRect));
                }
            }

            _bitmapInterpolationMode = brush.BitmapInterpolationMode;
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

        private static BrushProperties GetBrushProperties(ITileBrush brush, Rect destinationRect)
        {
            var tileTransform =
                brush.TileMode != TileMode.None ?
                Matrix.CreateTranslation(destinationRect.X, destinationRect.Y) :
                Matrix.Identity;

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

        private ID2D1BitmapRenderTarget RenderIntermediate(
            ID2D1RenderTarget target,
            BitmapImpl bitmap,
            TileBrushCalculator calc)
        {
            var result = target.CreateCompatibleRenderTarget(
                calc.IntermediateSize.ToSharpDX(),
                null,
                null,
                CompatibleRenderTargetOptions.None
                );

            using (var context = new RenderTarget(result).CreateDrawingContext(null))
            {
                var dpi = new Vector(target.Dpi.X, target.Dpi.Y);
                var rect = new Rect(bitmap.PixelSize.ToSizeWithDpi(dpi));

                context.Clear(Colors.Transparent);
                context.PushClip(calc.IntermediateClip);
                context.Transform = calc.IntermediateTransform;
                
                context.DrawBitmap(RefCountable.CreateUnownedNotClonable(bitmap), 1, rect, rect, _bitmapInterpolationMode);
                context.PopClip();
            }

            return result;
        }
    }
}
