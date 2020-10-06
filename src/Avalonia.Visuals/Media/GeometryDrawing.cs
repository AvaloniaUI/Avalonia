using Avalonia.Media.Immutable;
using Avalonia.Metadata;

namespace Avalonia.Media
{
    public class GeometryDrawing : Drawing
    {
        // Adding the Pen's stroke thickness here could yield wrong results due to transforms.
        private static readonly IPen s_boundsPen = new ImmutablePen(Colors.Black.ToUint32(), 0);
    
        public static readonly StyledProperty<Geometry> GeometryProperty =
            AvaloniaProperty.Register<GeometryDrawing, Geometry>(nameof(Geometry));

        [Content]
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
            return Geometry?.GetRenderBounds(s_boundsPen) ?? new Rect();
        }
    }
}
