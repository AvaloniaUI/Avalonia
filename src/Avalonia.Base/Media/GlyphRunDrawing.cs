namespace Avalonia.Media
{
    public sealed class GlyphRunDrawing : Drawing
    {
        public static readonly StyledProperty<IBrush?> ForegroundProperty =
            AvaloniaProperty.Register<GlyphRunDrawing, IBrush?>(nameof(Foreground));

        public static readonly StyledProperty<GlyphRun?> GlyphRunProperty =
            AvaloniaProperty.Register<GlyphRunDrawing, GlyphRun?>(nameof(GlyphRun));

        public IBrush? Foreground
        {
            get => GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public GlyphRun? GlyphRun
        {
            get => GetValue(GlyphRunProperty);
            set => SetValue(GlyphRunProperty, value);
        }

        internal override void DrawCore(DrawingContext context)
        {
            if (GlyphRun == null)
            {
                return;
            }

            context.DrawGlyphRun(Foreground, GlyphRun);
        }

        public override Rect GetBounds()
        {
            return GlyphRun != null ? GlyphRun.Bounds : default;
        }
    }
}
