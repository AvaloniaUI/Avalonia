namespace Avalonia.Media
{
    public class GlyphRunDrawing : Drawing
    {
        public static readonly StyledProperty<IBrush> ForegroundProperty =
            AvaloniaProperty.Register<GlyphRunDrawing, IBrush>(nameof(Foreground));

        public static readonly StyledProperty<GlyphRun> GlyphRunProperty =
            AvaloniaProperty.Register<GlyphRunDrawing, GlyphRun>(nameof(GlyphRun));

        public static readonly StyledProperty<Point> BaselineOriginProperty =
            AvaloniaProperty.Register<GlyphRunDrawing, Point>(nameof(BaselineOrigin));

        public IBrush Foreground
        {
            get => GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public GlyphRun GlyphRun
        {
            get => GetValue(GlyphRunProperty);
            set => SetValue(GlyphRunProperty, value);
        }

        public Point BaselineOrigin
        {
            get => GetValue(BaselineOriginProperty);
            set => SetValue(BaselineOriginProperty, value);
        }

        public override void Draw(DrawingContext context)
        {
            if (GlyphRun == null)
            {
                return;
            }

            context.DrawGlyphRun(Foreground, GlyphRun, BaselineOrigin);
        }

        public override Rect GetBounds()
        {
            return GlyphRun?.Bounds ?? default;
        }
    }
}
