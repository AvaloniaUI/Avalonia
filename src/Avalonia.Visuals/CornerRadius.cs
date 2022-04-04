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

            TopLeftRadiusX     = TopLeftRadiusY     = TopLeft;
            TopRightRadiusX    = TopRightRadiusY    = TopRight;
            BottomRightRadiusX = BottomRightRadiusY = BottomRight;
            BottomLeftRadiusX  = BottomLeftRadiusY  = BottomLeft;

            _isCircular = true;
        }

        public CornerRadius(double top, double bottom)
        {
            TopLeft    = TopRight    = top;
            BottomLeft = BottomRight = bottom;

            TopLeftRadiusX     = TopLeftRadiusY     = TopLeft;
            TopRightRadiusX    = TopRightRadiusY    = TopRight;
            BottomRightRadiusX = BottomRightRadiusY = BottomRight;
            BottomLeftRadiusX  = BottomLeftRadiusY  = BottomLeft;

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

            TopLeftRadiusX     = TopLeftRadiusY     = TopLeft;
            TopRightRadiusX    = TopRightRadiusY    = TopRight;
            BottomRightRadiusX = BottomRightRadiusY = BottomRight;
            BottomLeftRadiusX  = BottomLeftRadiusY  = BottomLeft;

            _isCircular = true;
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
            TopLeftRadiusX     = topLeftRadiusX;
            TopLeftRadiusY     = topLeftRadiusY;
            TopRightRadiusX    = topRightRadiusX;
            TopRightRadiusY    = topRightRadiusY;
            BottomRightRadiusX = bottomRightRadiusX;
            BottomRightRadiusY = bottomRightRadiusY;
            BottomLeftRadiusX  = bottomLeftRadiusX;
            BottomLeftRadiusY  = bottomLeftRadiusY;

            TopLeft     = (TopLeftRadiusX     + TopLeftRadiusY)     / 2.0;
            TopRight    = (TopRightRadiusX    + TopRightRadiusY)    / 2.0;
            BottomRight = (BottomRightRadiusX + BottomRightRadiusY) / 2.0;
            BottomLeft  = (BottomLeftRadiusX  + BottomLeftRadiusY)  / 2.0;

            _isCircular = (TopLeftRadiusX     == TopLeftRadiusY &&
                           TopRightRadiusX    == TopRightRadiusY &&
                           BottomRightRadiusX == BottomRightRadiusY &&
                           BottomLeftRadiusX  == BottomLeftRadiusY);
        }

        /// <summary>
        /// Gets the circular radius of the top left corner.
        /// </summary>
        public double TopLeft { get; }

        public double TopLeftRadiusX { get; }
        public double TopLeftRadiusY { get; }

        /// <summary>
        /// Gets the circular radius of the top right corner.
        /// </summary>
        public double TopRight { get; }

        public double TopRightRadiusX { get; }
        public double TopRightRadiusY { get; }

        /// <summary>
        /// Gets the circular radius of the bottom right corner.
        /// </summary>
        public double BottomRight { get; }

        public double BottomRightRadiusX { get; }
        public double BottomRightRadiusY { get; }

        /// <summary>
        /// Gets the circular radius of the bottom left corner.
        /// </summary>
        public double BottomLeft { get; }

        public double BottomLeftRadiusX { get; }
        public double BottomLeftRadiusY { get; }

        /// <summary>
        /// Gets a value indicating whether all corner radii are set to 0.
        /// </summary>
        public bool IsEmpty => TopLeft.Equals(0) && IsUniform;

        public bool IsCircular => _isCircular;

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
            return TopLeft == other.TopLeft &&
                   TopRight == other.TopRight &&
                   BottomRight == other.BottomRight &&
                   BottomLeft == other.BottomLeft &&
                   TopLeftRadiusX == other.TopLeftRadiusX &&
                   TopLeftRadiusY == other.TopLeftRadiusY &&
                   TopRightRadiusX == other.TopRightRadiusX &&
                   TopRightRadiusY == other.TopRightRadiusY &&
                   BottomRightRadiusX == other.BottomRightRadiusX &&
                   BottomRightRadiusY == other.BottomRightRadiusY &&
                   BottomLeftRadiusX == other.BottomLeftRadiusX &&
                   BottomLeftRadiusY == other.BottomLeftRadiusY;
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
