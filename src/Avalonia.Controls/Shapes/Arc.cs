using System;
using Avalonia.Media;
using Avalonia.Utilities;

namespace Avalonia.Controls.Shapes
{
    /// <summary>
    /// Represents a circular or elliptical arc (a segment of a curve).
    /// </summary>
    public class Arc : Shape
    {
        /// <summary>
        /// Defines the <see cref="StartAngle"/> property.
        /// </summary>
        public static readonly StyledProperty<double> StartAngleProperty =
            AvaloniaProperty.Register<Arc, double>(nameof(StartAngle), 0.0);

        /// <summary>
        /// Defines the <see cref="SweepAngle"/> property.
        /// </summary>
        public static readonly StyledProperty<double> SweepAngleProperty =
            AvaloniaProperty.Register<Arc, double>(nameof(SweepAngle), 0.0);

        static Arc()
        {
            StrokeThicknessProperty.OverrideDefaultValue<Arc>(1.0d);
            AffectsGeometry<Arc>(
                BoundsProperty,
                StrokeThicknessProperty,
                StartAngleProperty,
                SweepAngleProperty);
        }

        /// <summary>
        /// Gets or sets the angle at which the arc starts, in degrees.
        /// </summary>
        public double StartAngle
        {
            get => GetValue(StartAngleProperty);
            set => SetValue(StartAngleProperty, value);
        }

        /// <summary>
        /// Gets or sets the angle, in degrees, added to the <see cref="StartAngle"/> defining where the arc ends.
        /// A positive value is clockwise, negative is counter-clockwise.
        /// </summary>
        public double SweepAngle
        {
            get => GetValue(SweepAngleProperty);
            set => SetValue(SweepAngleProperty, value);
        }

        /// <inheritdoc/>
        protected override Geometry CreateDefiningGeometry()
        {
            var angle1 = MathUtilities.Deg2Rad(StartAngle);
            var angle2 = angle1 + MathUtilities.Deg2Rad(SweepAngle);

            var startAngle = Math.Min(angle1, angle2);
            var sweepAngle = Math.Max(angle1, angle2);

            var normStart = RadToNormRad(startAngle);
            var normEnd = RadToNormRad(sweepAngle);

            var rect = new Rect(Bounds.Size);

            if ((normStart == normEnd) && (startAngle != sweepAngle)) // Complete ring.
            {
                return new EllipseGeometry(rect.Deflate(StrokeThickness / 2));
            }
            else if (SweepAngle == 0)
            {
                return new StreamGeometry();
            }
            else // Partial arc.
            {
                var deflatedRect = rect.Deflate(StrokeThickness / 2);

                var centerX = rect.Center.X;
                var centerY = rect.Center.Y;

                var radiusX = deflatedRect.Width / 2;
                var radiusY = deflatedRect.Height / 2;

                var angleGap = RadToNormRad(sweepAngle - startAngle);

                var startPoint = GetRingPoint(radiusX, radiusY, centerX, centerY, startAngle);
                var endPoint = GetRingPoint(radiusX, radiusY, centerX, centerY, sweepAngle);

                var arcGeometry = new StreamGeometry();

                using (StreamGeometryContext context = arcGeometry.Open())
                {
                    context.BeginFigure(startPoint, false);
                    context.ArcTo(
                        endPoint,
                        new Size(radiusX, radiusY),
                        rotationAngle: angleGap,
                        isLargeArc: angleGap >= Math.PI,
                        SweepDirection.Clockwise);
                    context.EndFigure(false);
                }

                return arcGeometry;
            }
        }

        private static double RadToNormRad(double inAngle) => ((inAngle % (Math.PI * 2)) + (Math.PI * 2)) % (Math.PI * 2);

        private static Point GetRingPoint(double radiusX, double radiusY, double centerX, double centerY, double angle) =>
            new Point((radiusX * Math.Cos(angle)) + centerX, (radiusY * Math.Sin(angle)) + centerY);
    }
}
