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
                    this.geometry = new RectangleGeometry(new Rect(0, 0, this.Bounds.Size.Width, this.Bounds.Size.Height));
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
