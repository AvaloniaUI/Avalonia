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
        /// Defines the <see cref="CornerRadius"/> property.
        /// </summary>
        public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
            AvaloniaProperty.Register<Rectangle, CornerRadius>(nameof(CornerRadius));

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
                CornerRadiusProperty,
                RadiusXProperty,
                RadiusYProperty,
                StrokeThicknessProperty);
        }

        /// <summary>
        /// Gets or sets the radii used to round the corners of the rectangle.
        /// Radii may be either circular or elliptical and can change for each corner.
        /// </summary>
        /// <remarks>
        /// This property should be used in place of <see cref="RadiusX"/> and <see cref="RadiusY"/>
        /// in new code. It will always take precedence over the other two which exist for backwards
        /// compatibility.
        /// </remarks>
        public CornerRadius CornerRadius
        {
            get => GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        /// <summary>
        /// Gets or sets the radius on the X-axis used to round the corners of the rectangle.
        /// Corner radii are represented by an ellipse so this is 1/2 the X-axis width of the ellipse.
        /// </summary>
        /// <remarks>
        /// This property exists for compatibility with other XAML frameworks and will be overridden
        /// by setting <see cref="CornerRadius"/>. Please use <see cref="CornerRadius"/> instead for
        /// new code.
        /// </remarks>
        public double RadiusX
        {
            get => GetValue(RadiusXProperty);
            set => SetValue(RadiusXProperty, value);
        }

        /// <summary>
        /// Gets or sets the radius on the Y-axis used to round the corners of the rectangle.
        /// Corner radii are represented by an ellipse so this is 1/2 the Y-axis height of the ellipse.
        /// </summary>
        /// <remarks>
        /// This property exists for compatibility with other XAML frameworks and will be overridden
        /// by setting <see cref="CornerRadius"/>. Please use <see cref="CornerRadius"/> instead for
        /// new code.
        /// </remarks>
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

            CornerRadius radii;

            // Determine which corner radius properties to use
            if (IsSet(CornerRadiusProperty) &&
                CornerRadius.IsEmpty == false)
            {
                radii = CornerRadius;
            }
            else
            {
                radii = new CornerRadius(new EllipticalRadius(RadiusX, RadiusY));
            }

            if (radii.IsEmpty)
            {
                // Optimization when there are no corner radii
                var rect = new Rect(Bounds.Size).Deflate(StrokeThickness / 2);
                return new RectangleGeometry(rect);
            }
            else
            {
                var rect = new Rect(Bounds.Size).Deflate(StrokeThickness / 2);
                var geometry = new StreamGeometry();

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

                    context.BeginFigure(new Point(rect.Left + radii.TopLeftComponents.RadiusX, rect.Top), true);

                    // Line 1 + Corner 1
                    context.LineTo(new Point(rect.Right - radii.TopRightComponents.RadiusX, rect.Top));
                    context.ArcTo(
                        new Point(rect.Right, rect.Top + radii.TopRightComponents.RadiusY),
                        size: radii.TopRightComponents,
                        rotationAngle: PiOver2,
                        isLargeArc: false,
                        SweepDirection.Clockwise);

                    // Line 2 + Corner 2
                    context.LineTo(new Point(rect.Right, rect.Bottom - radii.BottomRightComponents.RadiusY));
                    context.ArcTo(
                        new Point(rect.Right - radii.BottomRightComponents.RadiusX, rect.Bottom),
                        size: radii.BottomRightComponents,
                        rotationAngle: PiOver2,
                        isLargeArc: false,
                        SweepDirection.Clockwise);

                    // Line 3 + Corner 3
                    context.LineTo(new Point(rect.Left + radii.BottomLeftComponents.RadiusX, rect.Bottom));
                    context.ArcTo(
                        new Point(rect.Left, rect.Bottom - radii.BottomLeftComponents.RadiusY),
                        size: radii.BottomLeftComponents,
                        rotationAngle: PiOver2,
                        isLargeArc: false,
                        SweepDirection.Clockwise);

                    // Line 4 + Corner 4
                    context.LineTo(new Point(rect.Left, rect.Top + radii.TopLeftComponents.RadiusY));
                    context.ArcTo(
                        new Point(rect.Left + radii.TopLeftComponents.RadiusX, rect.Top),
                        size: radii.TopLeftComponents,
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
