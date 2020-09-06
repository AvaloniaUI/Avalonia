using Avalonia.Media;

namespace Avalonia.Controls.Shapes
{
    public class Line : Shape
    {
        public static readonly StyledProperty<Point> StartPointProperty =
            AvaloniaProperty.Register<Line, Point>(nameof(StartPoint));

        public static readonly StyledProperty<Point> EndPointProperty =
            AvaloniaProperty.Register<Line, Point>(nameof(EndPoint));

        static Line()
        {
            StrokeThicknessProperty.OverrideDefaultValue<Line>(1);
            AffectsGeometry<Line>(StartPointProperty, EndPointProperty);
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

        protected override Geometry CreateDefiningGeometry()
        {
            return new LineGeometry(StartPoint, EndPoint);
        }
    }
}
