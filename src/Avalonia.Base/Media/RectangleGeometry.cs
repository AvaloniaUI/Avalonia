using Avalonia.Platform;

namespace Avalonia.Media
{
    /// <summary>
    /// Represents the geometry of a rectangle.
    /// </summary>
    public class RectangleGeometry : Geometry
    {
        /// <summary>
        /// Defines the <see cref="RadiusX"/> property.
        /// </summary>
        public static readonly StyledProperty<double> RadiusXProperty =
            AvaloniaProperty.Register<RectangleGeometry, double>(nameof(RadiusX));

        /// <summary>
        /// Defines the <see cref="RadiusY"/> property.
        /// </summary>
        public static readonly StyledProperty<double> RadiusYProperty =
            AvaloniaProperty.Register<RectangleGeometry, double>(nameof(RadiusY));

        /// <summary>
        /// Defines the <see cref="Rect"/> property.
        /// </summary>
        public static readonly StyledProperty<Rect> RectProperty =
            AvaloniaProperty.Register<RectangleGeometry, Rect>(nameof(Rect));

        static RectangleGeometry()
        {
            AffectsGeometry(
                RadiusXProperty,
                RadiusYProperty,
                RectProperty);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RectangleGeometry"/> class.
        /// </summary>
        public RectangleGeometry()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RectangleGeometry"/> class.
        /// </summary>
        /// <param name="rect">The rectangle bounds.</param>
        public RectangleGeometry(Rect rect)
        {
            Rect = rect;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RectangleGeometry"/> class.
        /// </summary>
        /// <param name="rect">The rectangle bounds.</param>
        /// <param name="radiusX">The radius on the X-axis used to round the corners of the rectangle.</param>
        /// <param name="radiusY">The radius on the Y-axis used to round the corners of the rectangle.</param>
        public RectangleGeometry(Rect rect, double radiusX, double radiusY)
        {
            Rect = rect;
            RadiusX = radiusX;
            RadiusY = radiusY;
        }

        /// <summary>
        /// Gets or sets the radius on the X-axis used to round the corners of the rectangle.
        /// Corner radii are represented by an ellipse so this is the X-axis width of the ellipse.
        /// </summary>
        /// <remarks>
        /// In order for this property to be used, <see cref="Rect"/> must not be set
        /// (equal to the default <see cref="Avalonia.Rect"/> value).
        /// </remarks>
        public double RadiusX
        {
            get => GetValue(RadiusXProperty);
            set => SetValue(RadiusXProperty, value);
        }

        /// <summary>
        /// Gets or sets the radius on the Y-axis used to round the corners of the rectangle.
        /// Corner radii are represented by an ellipse so this is the Y-axis height of the ellipse.
        /// </summary>
        /// <remarks>
        /// In order for this property to be used, <see cref="Rect"/> must not be set
        /// (equal to the default <see cref="Avalonia.Rect"/> value).
        /// </remarks>
        public double RadiusY
        {
            get => GetValue(RadiusYProperty);
            set => SetValue(RadiusYProperty, value);
        }

        /// <summary>
        /// Gets or sets the bounds of the rectangle.
        /// </summary>
        public Rect Rect
        {
            get => GetValue(RectProperty);
            set => SetValue(RectProperty, value);
        }

        /// <inheritdoc/>
        public override Geometry Clone() => new RectangleGeometry(Rect);

        private protected sealed override IGeometryImpl? CreateDefiningGeometry()
        {
            double radiusX = RadiusX;
            double radiusY = RadiusY;
            var factory = AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>();

            if (radiusX == 0 && radiusY == 0)
            {
                // Optimization when there are no corner radii
                return factory.CreateRectangleGeometry(Rect);
            }
            else
            {
                var geometry = factory.CreateStreamGeometry();
                using (var ctx = new StreamGeometryContext(geometry.Open()))
                {
                    GeometryBuilder.DrawRoundedCornersRectangle(ctx, Rect, radiusX, radiusY);
                }

                return geometry;
            }
        }
    }
}
