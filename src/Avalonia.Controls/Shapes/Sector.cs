using System;
using Avalonia.Media;
using Avalonia.Utilities;

namespace Avalonia.Controls.Shapes
{
    /// <summary>
    /// Represents a circular or elliptical sector (a pie-shaped closed region of a circle or ellipse).
    /// </summary>
    public class Sector : Shape
    {
        /// <summary>
        /// Defines the <see cref="StartAngle"/> property.
        /// </summary>
        public static readonly StyledProperty<double> StartAngleProperty =
            AvaloniaProperty.Register<Sector, double>(nameof(StartAngle), 0.0d);

        /// <summary>
        /// Defines the <see cref="SweepAngle"/> property.
        /// </summary>
        public static readonly StyledProperty<double> SweepAngleProperty =
            AvaloniaProperty.Register<Sector, double>(nameof(SweepAngle), 0.0d);

        /// <summary>
        /// Gets or sets the angle at which the sector's arc starts, in degrees.
        /// </summary>
        public double StartAngle
        {
            get => GetValue(StartAngleProperty);
            set => SetValue(StartAngleProperty, value);
        }

        /// <summary>
        /// Gets or sets the angle, in degrees, added to the <see cref="StartAngle"/> defining where the sector's arc ends.
        /// A positive value is clockwise, negative is counter-clockwise.
        /// </summary>
        public double SweepAngle
        {
            get => GetValue(SweepAngleProperty);
            set => SetValue(SweepAngleProperty, value);
        }

        static Sector()
        {
            StrokeThicknessProperty.OverrideDefaultValue<Sector>(1.0d);
            AffectsGeometry<Sector>(
                BoundsProperty,
                StrokeThicknessProperty,
                StartAngleProperty,
                SweepAngleProperty);
        }

        /// <inheritdoc/>
        protected override Geometry? CreateDefiningGeometry()
        {
            Rect rect = new Rect(Bounds.Size);
            Rect deflatedRect = rect.Deflate(StrokeThickness * 0.5d);

            if (SweepAngle >= 360.0d || SweepAngle <= -360.0d)
            {
                return new EllipseGeometry(deflatedRect);
            }

            if (SweepAngle == 0.0d)
            {
                return new StreamGeometry();
            }

            (double startAngle, double endAngle) = MathUtilities.GetMinMaxFromDelta(
                MathUtilities.Deg2Rad(StartAngle),
                MathUtilities.Deg2Rad(SweepAngle));

            Point centre = new Point(rect.Width * 0.5d, rect.Height * 0.5d);
            double radiusX = deflatedRect.Width * 0.5d;
            double radiusY = deflatedRect.Height * 0.5d;
            Point startCurvePoint = MathUtilities.GetEllipsePoint(centre, radiusX, radiusY, startAngle);
            Point endCurvePoint = MathUtilities.GetEllipsePoint(centre, radiusX, radiusY, endAngle);
            Size size = new Size(radiusX, radiusY);

            var streamGeometry = new StreamGeometry();
            using (StreamGeometryContext context = streamGeometry.Open())
            {
                context.BeginFigure(startCurvePoint, isFilled: true);
                context.ArcTo(
                    endCurvePoint,
                    size,
                    rotationAngle: 0.0d,
                    isLargeArc: Math.Abs(SweepAngle) > 180.0d,
                    SweepDirection.Clockwise);
                context.LineTo(centre);
                context.EndFigure(true);
            }

            return streamGeometry;
        }
    }
}
