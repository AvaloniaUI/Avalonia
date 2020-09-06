using System.Collections.Generic;
using Avalonia.Media;

namespace Avalonia.Controls.Shapes
{
    public class Polyline: Shape
    {
        public static readonly StyledProperty<IList<Point>> PointsProperty =
            AvaloniaProperty.Register<Polyline, IList<Point>>("Points");

        static Polyline()
        {
            StrokeThicknessProperty.OverrideDefaultValue<Polyline>(1);
            AffectsGeometry<Polyline>(PointsProperty);
        }

        public IList<Point> Points
        {
            get { return GetValue(PointsProperty); }
            set { SetValue(PointsProperty, value); }
        }

        protected override Geometry CreateDefiningGeometry()
        {
            return new PolylineGeometry(Points, false);
        }
    }
}
