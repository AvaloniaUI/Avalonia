// -----------------------------------------------------------------------
// <copyright file="Ellipse.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

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
                if (this.geometry == null || this.geometrySize != this.ActualSize)
                {
                    var rect = new Rect(0, 0, this.ActualSize.Width, this.ActualSize.Height);
                    rect = rect.Deflate(this.StrokeThickness / 2);
                    this.geometry = new EllipseGeometry(rect);
                    this.geometrySize = this.ActualSize;
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
