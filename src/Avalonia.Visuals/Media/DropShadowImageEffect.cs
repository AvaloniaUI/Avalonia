namespace Avalonia.Media
{
    public interface IDropShadowImageEffect : IBoundsAffectingImageEffect
    {
        Vector Offset { get; }
        Vector Blur { get; }
        Color Color { get; }
        double ShadowOpacity { get; }
    }

    public class DropShadowImageEffect : ImageEffect, IMutableImageEffect, IDropShadowImageEffect
    {
        static DropShadowImageEffect()
        {
            AffectsRender<DropShadowImageEffect>(OffsetProperty, BlurProperty, ColorProperty);
        }
        
        public static readonly StyledProperty<Vector> OffsetProperty =
            AvaloniaProperty.Register<DropShadowImageEffect, Vector>(nameof(Offset));

        public Vector Offset
        {
            get => GetValue(OffsetProperty);
            set => SetValue(OffsetProperty, value);
        }

        public static readonly StyledProperty<Vector> BlurProperty =
            AvaloniaProperty.Register<DropShadowImageEffect, Vector>(nameof(Blur));

        public Vector Blur
        {
            get => GetValue(BlurProperty);
            set => SetValue(BlurProperty, value);
        }

        public static readonly StyledProperty<Color> ColorProperty =
            AvaloniaProperty.Register<DropShadowImageEffect, Color>(nameof(Color));

        public Color Color
        {
            get => GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }



        public static readonly StyledProperty<double> ShadowOpacityProperty =
            AvaloniaProperty.Register<DropShadowImageEffect, double>(nameof(ShadowOpacity), 1);

        public double ShadowOpacity
        {
            get => GetValue(ShadowOpacityProperty);
            set => SetValue(ShadowOpacityProperty, value);
        }


        public override IImageEffect ToImmutable()
        {
            return new ImmutableDropShadowImageEffect(Offset, Blur, Color, ShadowOpacity);
        }

        public bool Equals(IImageEffect other)
        {
            return other is IDropShadowImageEffect ds
                   && ds.Color == Color && ds.Blur == Blur && ds.Offset == Offset && ds.ShadowOpacity == ShadowOpacity;
        }

        public Rect UpdateBounds(Rect bounds) => ToImmutable().UpdateBounds(bounds);
    }

    public struct ImmutableDropShadowImageEffect : IDropShadowImageEffect
    {
        public ImmutableDropShadowImageEffect(Vector offset, Vector blur, Color color,  double shadowOpacity)
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
            return obj is IDropShadowImageEffect other && Equals(other);
        }
        
        public bool Equals(IImageEffect other)
        {
            return other is IDropShadowImageEffect ds
                   && ds.Color == Color && ds.Blur == Blur && ds.Offset == Offset && ds.ShadowOpacity == ShadowOpacity;
        }

        public Rect UpdateBounds(Rect bounds)
        {
            var shadow = bounds.Translate(Offset);
            shadow = shadow.Inflate(new Thickness(Blur.X * 2, Blur.Y * 2));
            return bounds.Union(shadow);
        }
    }

    public static class ImageEffectExtensions
    {
        public static IImageEffect ToImmutable(this IImageEffect filter) =>
            filter is IMutableImageEffect mutable ? mutable.ToImmutable() : filter;
        
        public static Rect UpdateBounds(this IImageEffect filter, Rect bounds)
        {
            if (filter is IBoundsAffectingImageEffect baif)
                return baif.UpdateBounds(bounds);
            return bounds;
        }
    }
}
