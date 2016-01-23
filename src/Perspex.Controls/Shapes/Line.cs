using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Perspex.Media;

namespace Perspex.Controls.Shapes
{
    public class Line : Shape
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
                    _geometry = new LineGeometry(rect.TopLeft, rect.BottomRight);
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
