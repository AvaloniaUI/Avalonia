using Perspex.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perspex.Controls.Shapes
{
    public class Polyline: Shape
    {
        public static readonly PerspexProperty<IList<Point>> PointsProperty =
            PerspexProperty.Register<Polyline, IList<Point>>("Points");

        public Polyline()
        {
            StrokeThickness = 1; // Default Thickness
        }

        public IList<Point> Points
        {
            get { return GetValue(PointsProperty); }
            set { SetValue(PointsProperty, value); }
        }

        public override Geometry DefiningGeometry => new PolylineGeometry(Points, false);
    }
}
