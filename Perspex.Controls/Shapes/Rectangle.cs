// -----------------------------------------------------------------------
// <copyright file="Rectangle.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Shapes
{
    using Perspex.Media;

    public class Rectangle : Shape
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
                    this.geometry = new RectangleGeometry(rect);
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
