namespace Avalonia.Media
{
    public interface IDropShadowImageFilter : IBoundsAffectingImageFilter
    {
        Vector Offset { get; }
        Vector Blur { get; }
        Color Color { get; }
        double ShadowOpacity { get; }
    }

    public class DropShadowImageFilter : ImageFilter, IMutableImageFilter, IDropShadowImageFilter
    {
        static DropShadowImageFilter()
        {
            AffectsRender<DropShadowImageFilter>(OffsetProperty, BlurProperty, ColorProperty);
        }
        
        public static readonly StyledProperty<Vector> OffsetProperty =
            AvaloniaProperty.Register<DropShadowImageFilter, Vector>(nameof(Offset));

        public Vector Offset
        {
            get => GetValue(OffsetProperty);
            set => SetValue(OffsetProperty, value);
        }

        public static readonly StyledProperty<Vector> BlurProperty =
            AvaloniaProperty.Register<DropShadowImageFilter, Vector>(nameof(Blur));

        public Vector Blur
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



        public static readonly StyledProperty<double> ShadowOpacityProperty =
            AvaloniaProperty.Register<DropShadowImageFilter, double>(nameof(ShadowOpacity), 1);

        public double ShadowOpacity
        {
            get => GetValue(ShadowOpacityProperty);
            set => SetValue(ShadowOpacityProperty, value);
        }


        public override IImageFilter ToImmutable()
        {
            return new ImmutableDropShadowImageFilter(Offset, Blur, Color, ShadowOpacity);
        }

        public bool Equals(IImageFilter other)
        {
            return other is IDropShadowImageFilter ds
                   && ds.Color == Color && ds.Blur == Blur && ds.Offset == Offset && ds.ShadowOpacity == ShadowOpacity;
        }

        public Rect UpdateBounds(Rect bounds) => ToImmutable().UpdateBounds(bounds);
    }

    public struct ImmutableDropShadowImageFilter : IDropShadowImageFilter
    {
        public ImmutableDropShadowImageFilter(Vector offset, Vector blur, Color color,  double shadowOpacity)
        {
            Offset = offset;
            Blur = blur;
            Color = color;
            ShadowOpacity = shadowOpacity;
        }

        public Vector Offset { get; }
        public Vector Blur { get; }
        public Color Color { get; }
        public double ShadowOpacity { get; }



        public override bool Equals(object obj)
        {
            return obj is IDropShadowImageFilter other && Equals(other);
        }
        
        public bool Equals(IImageFilter other)
        {
            return other is IDropShadowImageFilter ds
                   && ds.Color == Color && ds.Blur == Blur && ds.Offset == Offset && ds.ShadowOpacity == ShadowOpacity;
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
