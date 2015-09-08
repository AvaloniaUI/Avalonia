// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Media;

namespace Perspex.Controls.Shapes
{
    public class Ellipse : Shape
    {
        private Geometry _geometry;

        private Size _geometrySize;

        public override Geometry DefiningGeometry
        {
            get
            {
                if (_geometry == null || _geometrySize != Bounds.Size)
                {
                    var rect = new Rect(Bounds.Size).Deflate(StrokeThickness);
                    _geometry = new EllipseGeometry(rect);
                    _geometrySize = Bounds.Size;
                }

                return _geometry;
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(StrokeThickness, StrokeThickness);
        }
    }
}
