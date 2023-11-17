using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Data;

namespace Avalonia.Controls.Shapes
{
    public class Polygon : Shape
    {
        public static readonly StyledProperty<IList<Point>> PointsProperty =
            AvaloniaProperty.Register<Polygon, IList<Point>>("Points");

        static Polygon()
        {
            AffectsGeometry<Polygon>(PointsProperty);
        }

        public Polygon()
        {
            SetValue(PointsProperty, new Points(), BindingPriority.Template);
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
