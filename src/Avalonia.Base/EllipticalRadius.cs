using System;
using System.Globalization;

namespace Avalonia
{
    /// <summary>
    /// Represents the elliptical X/Y components of a single corner radius.
    /// </summary>
#if !BUILDTASK
    public
#endif
    readonly struct EllipticalRadius : IEquatable<EllipticalRadius>
    {
        public EllipticalRadius(double uniformRadius)
        {
            RadiusX = uniformRadius;
            RadiusY = uniformRadius;
        }

        public EllipticalRadius(double radiusX, double radiusY)
        {
            RadiusX = radiusX;
            RadiusY = radiusY;
        }

        /// <summary>
        /// Gets the X-axis component of the radius.
        /// Radius here is represented by an ellipse so this is 1/2 the X-axis width of the ellipse.
        /// </summary>
        public double RadiusX { get; }

        /// <summary>
        /// Gets the Y-axis component of the radius.
        /// Radius here is represented by an ellipse so this is 1/2 the Y-axis height of the ellipse.
        /// </summary>
        public double RadiusY { get; }

        /// <summary>
        /// Gets the average of the X and Y radius components.
        /// This should be used in places that always require uniform, circular radius.
        /// </summary>
        public double Average => ((RadiusX + RadiusY) / 2.0);

        /// <summary>
        /// Gets a value indicating whether the radius is set to 0.
        /// </summary>
        public bool IsEmpty
        {
            get => RadiusX == 0.0 && RadiusY == 0.0;
        }

        /// <summary>
        /// Gets a value indicating whether the radius is circular with equal X and Y components.
        /// </summary>
        public bool IsCircular
        {
            get => RadiusX == RadiusY;
        }

        /// <summary>
        /// Gets a value indicating whether the X and Y radius components are uniform (equal).
        /// In practice this is the same as <see cref="IsCircular"/>.
        /// </summary>
        public bool IsUniform  => IsCircular;

        /// <summary>
        /// Returns a boolean indicating whether the corner radius is equal to the other given corner radius.
        /// </summary>
        /// <param name="other">The other corner radius to test equality against.</param>
        /// <returns>True if this corner radius is equal to other; False otherwise.</returns>
        public bool Equals(EllipticalRadius other)
        {
            return RadiusX == other.RadiusX &&
                   RadiusY == other.RadiusY;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is EllipticalRadius other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return RadiusX.GetHashCode() ^ RadiusY.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"({RadiusX.ToString(CultureInfo.InvariantCulture)},{RadiusY.ToString(CultureInfo.InvariantCulture)})";
        }

        public static bool operator ==(EllipticalRadius left, EllipticalRadius right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EllipticalRadius left, EllipticalRadius right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Implicit conversion from an <see cref="EllipticalRadius"/> to a <see cref="Size"/>.
        /// </summary>
        /// <param name="ellipticalRadius">The <see cref="EllipticalRadius"/> to convert.</param>
        public static implicit operator Size(EllipticalRadius ellipticalRadius)
        {
            return new Size(ellipticalRadius.RadiusX, ellipticalRadius.RadiusY);
        }

        /// <summary>
        /// Implicit conversion from a <see cref="Size"/> to an <see cref="EllipticalRadius"/>.
        /// </summary>
        /// <param name="size">The <see cref="Size"/> to convert.</param>
        public static implicit operator EllipticalRadius(Size size)
        {
            return new EllipticalRadius(size.Width, size.Height);
        }
    }
}
