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
        public static readonly PerspexProperty<PointPair> PointPairProperty =
            PerspexProperty.Register<Line, PointPair>("PointPair");

        private LineGeometry _geometry;
        private PointPair _pointPair;

        public Line()
        {
            StrokeThickness = 1;
        }

        public PointPair PointPair
        {
            get { return GetValue(PointPairProperty); }
            set { SetValue(PointPairProperty, value); }
        }

        public override Geometry DefiningGeometry
        {
            get
            {
                if (_geometry == null || _pointPair == null || PointPair.P1 != _pointPair.P1 || PointPair.P2 != _pointPair.P2)
                {
                    _pointPair = PointPair;
                    _geometry = new LineGeometry(_pointPair);
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
