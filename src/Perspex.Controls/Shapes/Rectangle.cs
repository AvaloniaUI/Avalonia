// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Media;

namespace Perspex.Controls.Shapes
{
    public class Rectangle : Shape
    {
        private Geometry _geometry;

        private Size _geometrySize;

        public override Geometry DefiningGeometry
        {
            get
            {
                if (_geometry == null || _geometrySize != this.Bounds.Size)
                {
                    var rect = new Rect(this.Bounds.Size).Deflate(this.StrokeThickness);
                    _geometry = new RectangleGeometry(rect);
                    _geometrySize = this.Bounds.Size;
                }

                return _geometry;
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(this.StrokeThickness, this.StrokeThickness);
        }
    }
}
