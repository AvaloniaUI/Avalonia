using Avalonia.Media;

namespace Avalonia
{
    public interface IDropShadowImageFilter : IBoundsAffectingImageFilter
    {
        Point Offset { get; }
        Point Blur { get; }
        Color Color { get; }
    }

    public class DropShadowImageFilter : ImageFilter, IMutableImageFilter, IDropShadowImageFilter
    {
        static DropShadowImageFilter()
        {
            AffectsRender<DropShadowImageFilter>(OffsetProperty, BlurProperty, ColorProperty);
        }
        
        public static readonly StyledProperty<Point> OffsetProperty =
            AvaloniaProperty.Register<DropShadowImageFilter, Point>(nameof(Offset));

        public Point Offset
        {
            get => GetValue(OffsetProperty);
            set => SetValue(OffsetProperty, value);
        }

        public static readonly StyledProperty<Point> BlurProperty =
            AvaloniaProperty.Register<DropShadowImageFilter, Point>(nameof(Blur));

        public Point Blur
        {
            get => GetValue(BlurProperty);
            set => SetValue(BlurProperty, value);
        }

        public static readonly StyledProperty<Color> ColorProperty =
            AvaloniaProperty.Register<DropShadowImageFilter, Color>(nameof(Color));

        public Color Color
        {
            get => GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        public override IImageFilter ToImmutable()
        {
            return new ImmutableDropShadowImageFilter(Offset, Blur, Color);
        }

        public bool Equals(IImageFilter other)
        {
            return other is IDropShadowImageFilter ds
                   && ds.Color == Color && ds.Blur == Blur && ds.Offset == Offset;
        }

        public Rect UpdateBounds(Rect bounds) => ToImmutable().UpdateBounds(bounds);
    }

    public struct ImmutableDropShadowImageFilter : IDropShadowImageFilter
    {
        public ImmutableDropShadowImageFilter(Point offset, Point blur, Color color)
        {
            Offset = offset;
            Blur = blur;
            Color = color;
        }

        public Point Offset { get; }
        public Point Blur { get; }
        public Color Color { get; }



        public override bool Equals(object obj)
        {
            return obj is IDropShadowImageFilter other && Equals(other);
        }
        
        public bool Equals(IImageFilter other)
        {
            return other is IDropShadowImageFilter ds
                   && ds.Color == Color && ds.Blur == Blur && ds.Offset == Offset;
        }

        public Rect UpdateBounds(Rect bounds)
        {
            var shadow = bounds.Translate(Offset);
            shadow = shadow.Inflate(new Thickness(Blur.X * 2, Blur.Y * 2));
            return bounds.Union(shadow);
        }
    }

    public static class ImageFilterExtensions
    {
        public static IImageFilter ToImmutable(this IImageFilter filter) =>
            filter is IMutableImageFilter mutable ? mutable.ToImmutable() : filter;
        
        public static Rect UpdateBounds(this IImageFilter filter, Rect bounds)
        {
            if (filter is IBoundsAffectingImageFilter baif)
                return baif.UpdateBounds(bounds);
            return bounds;
        }
    }
}
