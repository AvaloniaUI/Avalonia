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
        public static readonly PerspexProperty<Point> StartPointProperty =
            PerspexProperty.Register<Line, Point>("StartPoint");

        public static readonly PerspexProperty<Point> EndPointProperty =
            PerspexProperty.Register<Line, Point>("EndPoint");

        private LineGeometry _geometry;
        private Point _startPoint;
        private Point _endPoint;

        static Line()
        {
            StrokeThicknessProperty.OverrideDefaultValue<Line>(1);
        }

        public Point StartPoint
        {
            get { return GetValue(StartPointProperty); }
            set { SetValue(StartPointProperty, value); }
        }

        public Point EndPoint
        {
            get { return GetValue(EndPointProperty); }
            set { SetValue(EndPointProperty, value); }
        }

        public override Geometry DefiningGeometry
        {
            get
            {
                if (_geometry == null || StartPoint != _startPoint || EndPoint != _endPoint)
                {
                    _startPoint = StartPoint;
                    _endPoint = EndPoint;
                    _geometry = new LineGeometry(_startPoint, _endPoint);
                }

                return _geometry;
            }
        }
    }
}
