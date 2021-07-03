using System;
using Avalonia.Media;

namespace Avalonia.Controls.Shapes
{
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
            StrokeThicknessProperty.OverrideDefaultValue<Arc>(1);
            AffectsGeometry<Arc>(BoundsProperty, StrokeThicknessProperty, StartAngleProperty, SweepAngleProperty);
        }

        /// <summary>
        /// Gets or sets the angle at which the arc starts, in degrees.
        /// </summary>
        public double StartAngle
        {
            get { return GetValue(StartAngleProperty); }
            set { SetValue(StartAngleProperty, value); }
        }

        /// <summary>
        /// Gets or sets the angle, in degrees, added to the <see cref="StartAngle"/> defining where the arc ends. A positive value is clockwise, negative is counter-clockwise.
        /// </summary>
        public double SweepAngle
        {
            get { return GetValue(SweepAngleProperty); }
            set { SetValue(SweepAngleProperty, value); }
        }
        
        protected override Geometry CreateDefiningGeometry()
        {
            double angle1 = DegreesToRad(StartAngle);
            double angle2 = angle1 + DegreesToRad(SweepAngle);
            
            double startAngle = Math.Min(angle1, angle2);
            double sweepAngle = Math.Max(angle1, angle2);

            double normStart = RadToNormRad(startAngle);
            double normEnd = RadToNormRad(sweepAngle);

            var rect = new Rect(Bounds.Size);

            if ((normStart == normEnd) && (startAngle != sweepAngle)) // complete ring
            {
                return new EllipseGeometry(rect.Deflate(StrokeThickness / 2));
            }
            else if (SweepAngle == 0)
            {
                return new StreamGeometry();
            }
            else // partial arc
            {
                var deflatedRect = rect.Deflate(StrokeThickness / 2);

                double centerX = rect.Center.X;
                double centerY = rect.Center.Y;

                double radiusX = deflatedRect.Width / 2;
                double radiusY = deflatedRect.Height / 2;
                
                double angleGap = RadToNormRad(sweepAngle - startAngle);

                Point startPoint = GetRingPoint(radiusX, radiusY, centerX, centerY, startAngle);
                Point endPoint = GetRingPoint(radiusX, radiusY, centerX, centerY, sweepAngle);

                StreamGeometry arcGeometry = new StreamGeometry();
                using (var ctx = arcGeometry.Open())
                {
                    ctx.BeginFigure(startPoint, false);
                    ctx.ArcTo(endPoint, new Size(radiusX, radiusY), angleGap, angleGap >= Math.PI, SweepDirection.Clockwise);
                    ctx.EndFigure(false);
                }
                return arcGeometry;
            }
        }

        static double DegreesToRad(double inAngle) =>
            inAngle * Math.PI / 180;
        
        static double RadToNormRad(double inAngle) => (0 + (inAngle % (Math.PI * 2)) + (Math.PI * 2)) % (Math.PI * 2);

        static Point GetRingPoint(double radiusX, double radiusY, double centerX, double centerY, double angle) =>
            new Point((radiusX * Math.Cos(angle)) + centerX, (radiusY * Math.Sin(angle)) + centerY);
    }
}
