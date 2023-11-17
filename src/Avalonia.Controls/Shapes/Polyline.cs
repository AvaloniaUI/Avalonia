using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Data;

namespace Avalonia.Controls.Shapes
{
    public class Polyline : Shape
    {
        public static readonly StyledProperty<IList<Point>> PointsProperty =
            AvaloniaProperty.Register<Polyline, IList<Point>>("Points");

        static Polyline()
        {
            StrokeThicknessProperty.OverrideDefaultValue<Polyline>(1);
            AffectsGeometry<Polyline>(PointsProperty);
        }

        public Polyline()
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
            return new PolylineGeometry(Points, false);
        }
    }
}
