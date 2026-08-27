using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace ControlCatalog.Pages
{
    /// <summary>
    /// Fills its bounds twice with the same color values, once read as Display P3 and once read as
    /// sRGB, so that the inner square only becomes visible when the content is really presented in a
    /// gamut wider than sRGB.
    /// </summary>
    public class WideGamutSwatch : Control
    {
        public static readonly StyledProperty<Color> ColorProperty =
            AvaloniaProperty.Register<WideGamutSwatch, Color>(nameof(Color), Colors.Red);

        static WideGamutSwatch()
        {
            AffectsRender<WideGamutSwatch>(ColorProperty);
        }

        public Color Color
        {
            get => GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        public override void Render(DrawingContext context)
        {
            context.Custom(new SwatchDrawOperation(new Rect(Bounds.Size), Color));
        }

        private class SwatchDrawOperation : ICustomDrawOperation
        {
            private static readonly SKColorSpace s_displayP3 =
                SKColorSpace.CreateRgb(SKColorSpaceTransferFn.Srgb, SKColorSpaceXyz.DisplayP3);

            private readonly Color _color;

            public SwatchDrawOperation(Rect bounds, Color color)
            {
                Bounds = bounds;
                _color = color;
            }

            public Rect Bounds { get; }
            public bool HitTest(Point p) => false;
            public bool Equals(ICustomDrawOperation? other) => false;
            public void Dispose() { }

            public void Render(ImmediateDrawingContext context)
            {
                var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
                if (leaseFeature is null)
                {
                    // Without Skia there is nothing to tag a color with, so just show the sRGB color.
                    context.FillRectangle(new ImmutableSolidColorBrush(_color), Bounds);
                    return;
                }

                using var lease = leaseFeature.Lease();
                var canvas = lease.SkCanvas;

                var outer = new SKRect(
                    (float)Bounds.X, (float)Bounds.Y,
                    (float)Bounds.Right, (float)Bounds.Bottom);
                var inner = outer;
                inner.Inflate((float)-Bounds.Width / 4f, (float)-Bounds.Height / 4f);

                // A single pixel which carries the color values in the wider Display P3 primaries.
                // Blown up over the whole patch this asks for a color that sRGB can not represent:
                // a color managed surface keeps it, anything else converts it back down.
                var wideInfo = new SKImageInfo(1, 1, SKColorType.Rgba8888, SKAlphaType.Opaque, s_displayP3);
                using var wide = SKImage.FromPixelCopy(wideInfo, new byte[] { _color.R, _color.G, _color.B, 255 });
                canvas.DrawImage(wide, outer, new SKSamplingOptions(SKFilterMode.Nearest));

                // The very same values, but read as sRGB like any ordinary brush would be.
                using var paint = new SKPaint { Color = new SKColor(_color.R, _color.G, _color.B) };
                canvas.DrawRect(inner, paint);
            }
        }
    }
}
