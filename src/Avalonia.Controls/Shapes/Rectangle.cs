using Avalonia.Media;

namespace Avalonia.Controls.Shapes
{
    public class Rectangle : Shape
    {
        static Rectangle()
        {
            AffectsGeometry<Rectangle>(BoundsProperty, StrokeThicknessProperty);
        }

        protected override Geometry CreateDefiningGeometry()
        {
            var rect = new Rect(Bounds.Size).Deflate(StrokeThickness / 2);
            return new RectangleGeometry(rect);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(StrokeThickness, StrokeThickness);
        }
    }
}
