using Avalonia.Media;

namespace Avalonia.Controls.Shapes
{
    /// <summary>
    /// Represents a rectangle with optional rounded corners.
    /// </summary>
    public class Rectangle : Shape
    {
        private const double PiOver2 = 1.57079633; // 90 deg to rad

        /// <summary>
        /// Defines the <see cref="RadiusX"/> property.
        /// </summary>
        public static readonly StyledProperty<double> RadiusXProperty =
            AvaloniaProperty.Register<Rectangle, double>(nameof(RadiusX));

        /// <summary>
        /// Defines the <see cref="RadiusY"/> property.
        /// </summary>
        public static readonly StyledProperty<double> RadiusYProperty =
            AvaloniaProperty.Register<Rectangle, double>(nameof(RadiusY));

        static Rectangle()
        {
            AffectsGeometry<Rectangle>(
                BoundsProperty,
                RadiusXProperty,
                RadiusYProperty,
                StrokeThicknessProperty);
        }

        /// <summary>
        /// Gets or sets the radius on the X-axis used to round the corners of the rectangle.
        /// Corner radii are represented by an ellipse so this is the X-axis width of the ellipse.
        /// </summary>
        public double RadiusX
        {
            get => GetValue(RadiusXProperty);
            set => SetValue(RadiusXProperty, value);
        }

        /// <summary>
        /// Gets or sets the radius on the Y-axis used to round the corners of the rectangle.
        /// Corner radii are represented by an ellipse so this is the Y-axis height of the ellipse.
        /// </summary>
        public double RadiusY
        {
            get => GetValue(RadiusYProperty);
            set => SetValue(RadiusYProperty, value);
        }

        /// <inheritdoc/>
        protected override Geometry CreateDefiningGeometry()
        {
            // TODO: If RectangleGeometry ever supports RadiusX/Y like in WPF,
            // this code can be removed/combined with that implementation

            double x = RadiusX;
            double y = RadiusY;

            if (x == 0 && y == 0)
            {
                // Optimization when there are no corner radii
                var rect = new Rect(Bounds.Size).Deflate(StrokeThickness / 2);
                return new RectangleGeometry(rect);
            }
            else
            {
                var rect = new Rect(Bounds.Size).Deflate(StrokeThickness / 2);
                var geometry = new StreamGeometry();
                var arcSize = new Size(x, y);

                using (StreamGeometryContext context = geometry.Open())
                {
                    // The rectangle is constructed as follows:
                    //
                    //   (origin)
                    //   Corner 4            Corner 1
                    //   Top/Left  Line 1    Top/Right
                    //      \_   __________   _/
                    //          |          |
                    //   Line 4 |          | Line 2
                    //       _  |__________|  _
                    //      /      Line 3      \
                    //   Corner 3            Corner 2
                    //   Bottom/Left         Bottom/Right
                    //
                    // - Lines 1,3 follow the deflated rectangle bounds minus RadiusX
                    // - Lines 2,4 follow the deflated rectangle bounds minus RadiusY
                    // - All corners are constructed using elliptical arcs 

                    // Line 1 + Corner 1
                    context.BeginFigure(new Point(rect.Left + x, rect.Top), true);
                    context.LineTo(new Point(rect.Right - x, rect.Top));
                    context.ArcTo(
                        new Point(rect.Right, rect.Top + y),
                        arcSize,
                        rotationAngle: PiOver2,
                        isLargeArc: false,
                        SweepDirection.Clockwise);

                    // Line 2 + Corner 2
                    context.LineTo(new Point(rect.Right, rect.Bottom - y));
                    context.ArcTo(
                        new Point(rect.Right - x, rect.Bottom),
                        arcSize,
                        rotationAngle: PiOver2,
                        isLargeArc: false,
                        SweepDirection.Clockwise);

                    // Line 3 + Corner 3
                    context.LineTo(new Point(rect.Left + x, rect.Bottom));
                    context.ArcTo(
                        new Point(rect.Left, rect.Bottom - y),
                        arcSize,
                        rotationAngle: PiOver2,
                        isLargeArc: false,
                        SweepDirection.Clockwise);

                    // Line 4 + Corner 4
                    context.LineTo(new Point(rect.Left, rect.Top + y));
                    context.ArcTo(
                        new Point(rect.Left + x, rect.Top),
                        arcSize,
                        rotationAngle: PiOver2,
                        isLargeArc: false,
                        SweepDirection.Clockwise);

                    context.EndFigure(true);
                }

                return geometry;
            }
        }

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(StrokeThickness, StrokeThickness);
        }
    }
}
