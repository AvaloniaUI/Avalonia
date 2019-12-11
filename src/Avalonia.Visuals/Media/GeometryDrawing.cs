namespace Avalonia.Media
{
    public class GeometryDrawing : Drawing
    {
        public static readonly StyledProperty<Geometry> GeometryProperty =
            AvaloniaProperty.Register<GeometryDrawing, Geometry>(nameof(Geometry));

        public Geometry Geometry
        {
            get => GetValue(GeometryProperty);
            set => SetValue(GeometryProperty, value);
        }

        public static readonly StyledProperty<IBrush> BrushProperty =
            AvaloniaProperty.Register<GeometryDrawing, IBrush>(nameof(Brush), Brushes.Transparent);

        public IBrush Brush
        {
            get => GetValue(BrushProperty);
            set => SetValue(BrushProperty, value);
        }

        public static readonly StyledProperty<Pen> PenProperty =
            AvaloniaProperty.Register<GeometryDrawing, Pen>(nameof(Pen));

        public IPen Pen
        {
            get => GetValue(PenProperty);
            set => SetValue(PenProperty, value);
        }

        public override void Draw(DrawingContext context)
        {
            if (Geometry != null)
            {
                context.DrawGeometry(Brush, Pen, Geometry);
            }
        }

        public override Rect GetBounds()
        {
            // adding the Pen's stroke thickness here could yield wrong results due to transforms
            var pen = new Pen(Brushes.Black, 0);
            return Geometry?.GetRenderBounds(pen) ?? new Rect();
        }
    }
}
