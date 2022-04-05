using System;
using System.Globalization;
#if !BUILDTASK
using Avalonia.Animation.Animators;
#endif
using Avalonia.Utilities;

namespace Avalonia
{
    /// <summary>
    /// Represents the radii of a rectangle's corners.
    /// </summary>
#if !BUILDTASK
    public
#endif
    readonly struct CornerRadius : IEquatable<CornerRadius>
    {
        private readonly bool _isCircular;

        static CornerRadius()
        {
#if !BUILDTASK
            Animation.Animation.RegisterAnimator<CornerRadiusAnimator>(prop => typeof(CornerRadius).IsAssignableFrom(prop.PropertyType));
#endif
        }

        public CornerRadius(double uniformRadius)
        {
            TopLeft = TopRight = BottomLeft = BottomRight = uniformRadius;

            TopLeftSize     = new Size(TopLeft, TopLeft);
            TopRightSize    = new Size(TopRight, TopRight);
            BottomRightSize = new Size(BottomRight, BottomRight);
            BottomLeftSize  = new Size(BottomLeft, BottomLeft);

            _isCircular = true;
        }

        public CornerRadius(Size ellipticalRadius)
        {
            TopLeftSize     = ellipticalRadius;
            TopRightSize    = ellipticalRadius;
            BottomRightSize = ellipticalRadius;
            BottomLeftSize  = ellipticalRadius;

            TopLeft     = (TopLeftSize.Width     + TopLeftSize.Height)     / 2.0;
            TopRight    = (TopRightSize.Width    + TopRightSize.Height)    / 2.0;
            BottomRight = (BottomRightSize.Width + BottomRightSize.Height) / 2.0;
            BottomLeft  = (BottomLeftSize.Width  + BottomLeftSize.Height)  / 2.0;

            _isCircular = (ellipticalRadius.Width == ellipticalRadius.Height);
        }

        public CornerRadius(double top, double bottom)
        {
            TopLeft    = TopRight    = top;
            BottomLeft = BottomRight = bottom;

            TopLeftSize     = new Size(TopLeft, TopLeft);
            TopRightSize    = new Size(TopRight, TopRight);
            BottomRightSize = new Size(BottomRight, BottomRight);
            BottomLeftSize  = new Size(BottomLeft, BottomLeft);

            _isCircular = true;
        }

        public CornerRadius(
            double topLeft,
            double topRight,
            double bottomRight,
            double bottomLeft)
        {
            TopLeft     = topLeft;
            TopRight    = topRight;
            BottomRight = bottomRight;
            BottomLeft  = bottomLeft;

            TopLeftSize     = new Size(TopLeft, TopLeft);
            TopRightSize    = new Size(TopRight, TopRight);
            BottomRightSize = new Size(BottomRight, BottomRight);
            BottomLeftSize  = new Size(BottomLeft, BottomLeft);

            _isCircular = true;
        }

        public CornerRadius(
            Size topLeft,
            Size topRight,
            Size bottomRight,
            Size bottomLeft)
        {
            TopLeftSize     = topLeft;
            TopRightSize    = topRight;
            BottomRightSize = bottomRight;
            BottomLeftSize  = bottomLeft;

            TopLeft     = (TopLeftSize.Width     + TopLeftSize.Height)     / 2.0;
            TopRight    = (TopRightSize.Width    + TopRightSize.Height)    / 2.0;
            BottomRight = (BottomRightSize.Width + BottomRightSize.Height) / 2.0;
            BottomLeft  = (BottomLeftSize.Width  + BottomLeftSize.Height)  / 2.0;

            _isCircular = (TopLeftSize.Width     == TopLeftSize.Height &&
                           TopRightSize.Width    == TopRightSize.Height &&
                           BottomRightSize.Width == BottomRightSize.Height &&
                           BottomLeftSize.Width  == BottomLeftSize.Height);
        }

        public CornerRadius(
            double topLeftRadiusX,
            double topLeftRadiusY,
            double topRightRadiusX,
            double topRightRadiusY,
            double bottomRightRadiusX,
            double bottomRightRadiusY,
            double bottomLeftRadiusX,
            double bottomLeftRadiusY)
        {
            TopLeftSize     = new Size(topLeftRadiusX, topLeftRadiusY);
            TopRightSize    = new Size(topRightRadiusX, topRightRadiusY);
            BottomRightSize = new Size(bottomRightRadiusX, bottomRightRadiusY);
            BottomLeftSize  = new Size(bottomLeftRadiusX, bottomLeftRadiusY);

            TopLeft     = (TopLeftSize.Width     + TopLeftSize.Height)     / 2.0;
            TopRight    = (TopRightSize.Width    + TopRightSize.Height)    / 2.0;
            BottomRight = (BottomRightSize.Width + BottomRightSize.Height) / 2.0;
            BottomLeft  = (BottomLeftSize.Width  + BottomLeftSize.Height)  / 2.0;

            _isCircular = (TopLeftSize.Width     == TopLeftSize.Height &&
                           TopRightSize.Width    == TopRightSize.Height &&
                           BottomRightSize.Width == BottomRightSize.Height &&
                           BottomLeftSize.Width  == BottomLeftSize.Height);
        }

        /// <summary>
        /// Gets the circular radius of the top left corner.
        /// </summary>
        public double TopLeft { get; }

        public Size TopLeftSize { get; }

        /// <summary>
        /// Gets the circular radius of the top right corner.
        /// </summary>
        /// <remarks>
        /// This is only valid when <see cref="IsCircular"/> is true; otherwise,
        /// the elliptical radii in <see cref="TopRightSize"/> should be used.
        /// When the corner radius is elliptical this is set to the average of the
        /// width and height components.
        /// </remarks>
        public double TopRight { get; }

        /// <summary>
        /// Gets the elliptical components of the top right corner radius.
        /// </summary>
        public Size TopRightSize { get; }

        /// <summary>
        /// Gets the circular radius of the bottom right corner.
        /// </summary>
        public double BottomRight { get; }

        public Size BottomRightSize { get; }

        /// <summary>
        /// Gets the circular radius of the bottom left corner.
        /// </summary>
        public double BottomLeft { get; }

        public Size BottomLeftSize { get; }

        /// <summary>
        /// Gets a value indicating whether all corner radii are set to 0.
        /// </summary>
        public bool IsEmpty
        {
            get => TopLeftSize.IsDefault &&
                   TopRightSize.IsDefault &&
                   BottomRightSize.IsDefault &&
                   BottomLeftSize.IsDefault;
        }

        /// <summary>
        /// Gets a value indicating whether all corner radii are circular and have equal width and height.
        /// </summary>
        public bool IsCircular => _isCircular;

        /// <summary>
        /// Gets a value indicating whether one or more corner radii are elliptical and do not have equal
        /// width and height.
        /// </summary>
        public bool IsElliptical => !_isCircular;

        /// <summary>
        /// Gets a value indicating whether all corner radii are equal.
        /// </summary>
        public bool IsUniform
        {
            get => _isCircular &&
                   TopLeft.Equals(TopRight) &&
                   BottomLeft.Equals(BottomRight) &&
                   TopRight.Equals(BottomRight);
        }

        /// <summary>
        /// Returns a boolean indicating whether the corner radius is equal to the other given corner radius.
        /// </summary>
        /// <param name="other">The other corner radius to test equality against.</param>
        /// <returns>True if this corner radius is equal to other; False otherwise.</returns>
        public bool Equals(CornerRadius other)
        {
            // ReSharper disable CompareOfFloatsByEqualityOperator
            return TopLeftSize == other.TopLeftSize &&
                   TopRightSize == other.TopRightSize &&
                   BottomRightSize == other.BottomRightSize &&
                   BottomLeftSize == other.BottomLeftSize;
            // ReSharper restore CompareOfFloatsByEqualityOperator
        }

        /// <summary>
        /// Returns a boolean indicating whether the given Object is equal to this corner radius instance.
        /// </summary>
        /// <param name="obj">The Object to compare against.</param>
        /// <returns>True if the Object is equal to this corner radius; False otherwise.</returns>
        public override bool Equals(object? obj) => obj is CornerRadius other && Equals(other);

        public override int GetHashCode()
        {
            return TopLeft.GetHashCode() ^ TopRight.GetHashCode() ^ BottomLeft.GetHashCode() ^ BottomRight.GetHashCode();
        }

        public override string ToString()
        {
            return $"{TopLeft},{TopRight},{BottomRight},{BottomLeft}";
        }

        public static CornerRadius Parse(string s)
        {
            const string exceptionMessage = "Invalid CornerRadius.";

            using (var tokenizer = new StringTokenizer(s, CultureInfo.InvariantCulture, exceptionMessage))
            {
                if (tokenizer.TryReadDouble(out var a))
                {
                    if (tokenizer.TryReadDouble(out var b))
                    {
                        if (tokenizer.TryReadDouble(out var c))
                        {
                            return new CornerRadius(a, b, c, tokenizer.ReadDouble());
                        }

                        return new CornerRadius(a, b);
                    }

                    return new CornerRadius(a);
                }

                throw new FormatException(exceptionMessage);
            }
        }

        public static bool operator ==(CornerRadius left, CornerRadius right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CornerRadius left, CornerRadius right)
        {
            return !(left == right);
        }
    }
}
