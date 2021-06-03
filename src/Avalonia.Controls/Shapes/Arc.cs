using System;
using Avalonia.Media;

namespace Avalonia.Controls.Shapes
{
    public class Arc : Shape
    {
        ArcSegment _arcSegment;
        PathFigure _arcFigure;
        PathGeometry _arcGeometry;

        PathGeometry _emptyGeometry = new PathGeometry()
        {
            Figures = 
            {
                new PathFigure()
                {
                    StartPoint = new Point(0, 0)
                }
            }
        };

        
        /// <summary>
        /// Defines the <see cref="StartAngle"/> property.
        /// </summary>
        public static readonly StyledProperty<double> StartAngleProperty =
            AvaloniaProperty.Register<Arc, double>(nameof(StartAngle), 0.0);

        /// <summary>
        /// Defines the <see cref="EndAngle"/> property.
        /// </summary>
        public static readonly StyledProperty<double> EndAngleProperty =
            AvaloniaProperty.Register<Arc, double>(nameof(EndAngle), 0.0);

        static Arc()
        {
            StrokeThicknessProperty.OverrideDefaultValue<Arc>(1);
            AffectsGeometry<Arc>(BoundsProperty, StrokeThicknessProperty, StartAngleProperty, EndAngleProperty);
        }

        public Arc() : base()
        {
            _arcSegment = new ArcSegment()
            {
                SweepDirection = SweepDirection.Clockwise
            };

            _arcFigure = new PathFigure()
            {
                Segments = 
                {
                    _arcSegment
                },
                IsClosed = false
            };

            _arcGeometry = new PathGeometry()
            {
                Figures = 
                {
                    _arcFigure
                }
            };
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
        /// Gets or sets the angle at which the arc ends, in degrees.
        /// </summary>
        public double EndAngle
        {
            get { return GetValue(EndAngleProperty); }
            set { SetValue(EndAngleProperty, value); }
        }
        
        protected override Geometry CreateDefiningGeometry()
        {
            double angle1 = DegreesToRad(StartAngle);
            double angle2 = DegreesToRad(EndAngle);
            
            double startAngle = Math.Min(angle1, angle2);
            double endAngle = Math.Max(angle1, angle2);

            double normStart = RadToNormRad(startAngle);
            double normEnd = RadToNormRad(endAngle);

            var rect = new Rect(Bounds.Size);

            if ((normStart == normEnd) && (startAngle != endAngle)) //complete ring
            {
                return new EllipseGeometry(rect.Deflate(StrokeThickness / 2));
            }
            else if ((normStart == normEnd) && (startAngle == endAngle)) //zero-degree arc
            {
                return _emptyGeometry;
            }
            else //partial arc
            {
                var deflatedRect = rect.Deflate(StrokeThickness / 2);

                double centerX = rect.Center.X;
                double centerY = rect.Center.Y;

                double radiusX = deflatedRect.Width / 2;
                double radiusY = deflatedRect.Height / 2;
                
                double angleGap = RadToNormRad(endAngle - startAngle);

                Point startPoint = GetRingPoint(radiusX, radiusY, centerX, centerY, startAngle);
                Point endPoint = GetRingPoint(radiusX, radiusY, centerX, centerY, endAngle);
                
                _arcFigure.StartPoint = startPoint;
                
                _arcSegment.Point = endPoint;
                _arcSegment.IsLargeArc = angleGap >= Math.PI;
                _arcSegment.Size = new Size(radiusX, radiusY);
                
                return _arcGeometry;
            }
        }

        public override void Render(DrawingContext ctx)
        {
            double angle1 = DegreesToRad(StartAngle);
            double angle2 = DegreesToRad(EndAngle);
            
            double startAngle = Math.Min(angle1, angle2);
            double endAngle = Math.Max(angle1, angle2);

            double normStart = RadToNormRad(startAngle);
            double normEnd = RadToNormRad(endAngle);


            if ((normStart == normEnd) && (startAngle == endAngle))
            {
                /*start and end angles are identical, so we don't call base.Render().
                Doing so would cause the geometry to be drawn, which we don't want for
                a zero-length arc, as this could result in something being drawn, which
                would likely be interpreted by the user as "there's something there"
                even though there's supposed to be "nothing there" when the angles
                are identical. We can't just have CreateDefiningGeometry() return
                null in this scenario either, as the base Shape class seems to use the
                geometry for layouting, which needs to not work differently just because
                the arc is empty.*/
            }
            else
            {
                /*We're going around the implied circle by more than 0.0 degrees, so
                we let the base Shape class draw our geometry so it will actually be
                visible.*/
                base.Render(ctx);
            }
        }

        static double DegreesToRad(double inAngle) =>
            inAngle * Math.PI / 180;
        
        static double RadToNormRad(double inAngle) => (0 + (inAngle % (Math.PI * 2)) + (Math.PI * 2)) % (Math.PI * 2);

        static Point GetRingPoint(double radiusX, double radiusY, double centerX, double centerY, double angle) =>
            new Point((radiusX * Math.Cos(angle)) + centerX, (radiusY * Math.Sin(angle)) + centerY);
    }
}
