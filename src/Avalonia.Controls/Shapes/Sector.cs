using System;
using Avalonia.Media;
using Avalonia.Utilities;

namespace Avalonia.Controls.Shapes
{
    public class Sector : Shape
    {
        public static readonly StyledProperty<double> StartAngleProperty = AvaloniaProperty.Register<Sector, double>(nameof(StartAngle), 0.0d);
        public static readonly StyledProperty<double> AngleProperty = AvaloniaProperty.Register<Sector, double>(nameof(Angle), 0.0d);

        public double StartAngle
        {
            get => GetValue(StartAngleProperty);
            set => SetValue(StartAngleProperty, value);
        }

        public double Angle
        {
            get => GetValue(AngleProperty);
            set => SetValue(AngleProperty, value);
        }

        static Sector()
        {
            StrokeThicknessProperty.OverrideDefaultValue<Sector>(1.0d);
            AffectsGeometry<Sector>(BoundsProperty, StrokeThicknessProperty, StartAngleProperty, AngleProperty);
        }

        protected override Geometry? CreateDefiningGeometry()
        {
            Rect rect = new Rect(Bounds.Size);
            Rect deflatedRect = rect.Deflate(StrokeThickness * 0.5d);

            if (Angle >= 360.0d || Angle <= -360.0d)
            {
                return new EllipseGeometry(deflatedRect);
            }

            if (Angle == 0.0d)
            {
                return new StreamGeometry();
            }

            (double startAngle, double endAngle) = MathUtilities.GetMinMaxFromDelta(MathUtilities.Deg2Rad(StartAngle), MathUtilities.Deg2Rad(Angle));

            Point centre = new Point(rect.Width * 0.5d, rect.Height * 0.5d);
            double radiusX = deflatedRect.Width * 0.5d;
            double radiusY = deflatedRect.Height * 0.5d;
            Point startCurvePoint = MathUtilities.GetEllipsePoint(centre, radiusX, radiusY, startAngle);
            Point endCurvePoint = MathUtilities.GetEllipsePoint(centre, radiusX, radiusY, endAngle);
            Size size = new Size(radiusX, radiusY);

            StreamGeometry streamGeometry = new StreamGeometry();
            using StreamGeometryContext streamGeometryContext = streamGeometry.Open();

            streamGeometryContext.BeginFigure(startCurvePoint, false);
            streamGeometryContext.ArcTo(endCurvePoint, size, 0.0d, Math.Abs(Angle) > 180.0d, SweepDirection.Clockwise);
            streamGeometryContext.LineTo(centre);
            streamGeometryContext.EndFigure(true);

            return streamGeometry;
        }
    }
}
