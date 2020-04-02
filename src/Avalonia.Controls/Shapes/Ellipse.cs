using Avalonia.Media;

namespace Avalonia.Controls.Shapes
{
    public class Ellipse : Shape
    {
        static Ellipse()
        {
            AffectsGeometry<Ellipse>(BoundsProperty, StrokeThicknessProperty);
        }

        protected override Geometry CreateDefiningGeometry()
        {
            var rect = new Rect(Bounds.Size).Deflate(StrokeThickness / 2);
            return new EllipseGeometry(rect);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(StrokeThickness, StrokeThickness);
        }
    }
}
