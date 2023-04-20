using Avalonia.Media;

namespace Avalonia.Controls.Shapes
{
    public class Polygon : Shape
    {
        public static readonly StyledProperty<Points?> PointsProperty =
            AvaloniaProperty.Register<Polygon, Points?>("Points");

        static Polygon()
        {
            AffectsGeometry<Polygon>(PointsProperty);
        }

        public Points? Points
        {
            get { return GetValue(PointsProperty); }
            set { SetValue(PointsProperty, value); }
        }

        protected override Geometry CreateDefiningGeometry()
        {
            return new PolylineGeometry(Points, true);
        }
    }
}
