using System.Collections.Generic;
using Avalonia.Media;

namespace Avalonia.Controls.Shapes
{
    public class Polygon : Shape
    {
        public static readonly StyledProperty<IList<Point>> PointsProperty =
            AvaloniaProperty.Register<Polygon, IList<Point>>("Points", new Points());

        static Polygon()
        {
            AffectsGeometry<Polygon>(PointsProperty);
        }

        public IList<Point> Points
        {
            get => GetValue(PointsProperty);
            set => SetValue(PointsProperty, value);
        }

        protected override Geometry CreateDefiningGeometry()
        {
            return new PolylineGeometry(Points, true);
        }
    }
}
