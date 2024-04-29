using Avalonia.Media;

namespace Avalonia.Controls.Shapes
{
    /// <summary>
    /// Represents a rectangle with optional rounded corners.
    /// </summary>
    public class Rectangle : Shape
    {
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

        /// <inheritdoc cref="RectangleGeometry.RadiusX"/>
        public double RadiusX
        {
            get => GetValue(RadiusXProperty);
            set => SetValue(RadiusXProperty, value);
        }

        /// <inheritdoc cref="RectangleGeometry.RadiusY"/>
        public double RadiusY
        {
            get => GetValue(RadiusYProperty);
            set => SetValue(RadiusYProperty, value);
        }

        /// <inheritdoc/>
        protected override Geometry CreateDefiningGeometry()
        {
            var rect = new Rect(Bounds.Size).Deflate(StrokeThickness / 2);

            return new RectangleGeometry(rect, RadiusX, RadiusY);
        }

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(StrokeThickness, StrokeThickness);
        }
    }
}
