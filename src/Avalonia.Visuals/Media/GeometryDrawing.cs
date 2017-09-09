using Avalonia.Media.Imaging;

namespace Avalonia.Media
{
    public class GeometryDrawing : AvaloniaObject, IMutableDrawing
    {
        public static readonly StyledProperty<Geometry> GeometryProperty =
            AvaloniaProperty.Register<GeometryDrawing, Geometry>(nameof(Geometry));

        public static readonly StyledProperty<IBrush> BrushProperty =
            AvaloniaProperty.Register<GeometryDrawing, IBrush>(nameof(Brush), Brushes.Transparent);

        public static readonly StyledProperty<Pen> PenProperty =
            AvaloniaProperty.Register<GeometryDrawing, Pen>(nameof(Pen));

        public Geometry Geometry
        {
            get => GetValue(GeometryProperty);
            set => SetValue(GeometryProperty, value);
        }

        public IBrush Brush
        {
            get => GetValue(BrushProperty);
            set => SetValue(BrushProperty, value);
        }

        public Pen Pen
        {
            get => GetValue(PenProperty);
            set => SetValue(PenProperty, value);
        }

        double IImage.Width => GetBounds().Width;

        double IImage.Height => GetBounds().Height;

        public void Draw(DrawingContext context)
        {
            context.DrawGeometry(Brush, Pen, Geometry);
        }

        public Rect GetBounds()
        {
            // adding the Pen's stroke thickness here could yield wrong results due to transforms
            return Geometry?.GetRenderBounds(0) ?? new Rect();
        }

        IDrawing IMutableDrawing.ToImmutable() =>
            new Immutable(Geometry, Brush?.ToImmutable(), Pen?.ToImmutable(), GetBounds());

        private class Immutable : IDrawing
        {
            private readonly Geometry _geometry;
            private readonly IBrush _brush;
            private readonly Pen _pen;
            private readonly Rect _bounds;

            public Immutable(Geometry geometry, IBrush brush, Pen pen, Rect bounds)
            {
                _geometry = geometry;
                _brush = brush;
                _pen = pen;
                _bounds = bounds;
            }

            public double Width => _bounds.Width;

            public double Height => _bounds.Height;

            public void Draw(DrawingContext context)
            {
                context.DrawGeometry(_brush, _pen, _geometry);
            }

            public Rect GetBounds() => _bounds;
        }
    }
}