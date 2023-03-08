using System.Collections.Generic;
using Avalonia.Media;

namespace Avalonia.Controls.Shapes
{
    public class Polyline: Shape
    {
        public static readonly StyledProperty<Points> PointsProperty =
            AvaloniaProperty.Register<Polyline, Points>("Points");

        static Polyline()
        {
            StrokeThicknessProperty.OverrideDefaultValue<Polyline>(1);
            AffectsGeometry<Polyline>(PointsProperty);
        }

        public Points Points
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
