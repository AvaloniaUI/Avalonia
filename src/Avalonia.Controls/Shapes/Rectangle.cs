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
            var rect = new Rect(Bounds.Size).Deflate(StrokeThickness / 2);

            if (x == 0 && y == 0)
            {
                // Optimization when there are no corner radii
                return new RectangleGeometry(rect);
            }
            else
            {
                var geometry = new StreamGeometry();
                using (StreamGeometryContext context = geometry.Open())
                {
                    GeometryBuilder.DrawRoundedCornersRectangle(context, rect, x, y);
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
