





namespace Perspex.Controls.Shapes
{
    using Perspex.Media;

    public class Ellipse : Shape
    {
        private Geometry geometry;

        private Size geometrySize;

        public override Geometry DefiningGeometry
        {
            get
            {
                if (this.geometry == null || this.geometrySize != this.Bounds.Size)
                {
                    var rect = new Rect(this.Bounds.Size).Deflate(this.StrokeThickness);
                    this.geometry = new EllipseGeometry(rect);
                    this.geometrySize = this.Bounds.Size;
                }

                return this.geometry;
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(this.StrokeThickness, this.StrokeThickness);
        }
    }
}
