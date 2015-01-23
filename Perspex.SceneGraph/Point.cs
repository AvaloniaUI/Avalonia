// -----------------------------------------------------------------------
// <copyright file="Point.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System.Globalization;

    /// <summary>
    /// Defines a point.
    /// </summary>
    public struct Point
    {
        /// <summary>
        /// The X position.
        /// </summary>
        private double x;

        /// <summary>
        /// The Y position.
        /// </summary>
        private double y;

        /// <summary>
        /// Initializes a new instance of the <see cref="Point"/> structure.
        /// </summary>
        /// <param name="x">The X position.</param>
        /// <param name="y">The Y position.</param>
        public Point(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        /// <summary>
        /// Gets the X position.
        /// </summary>
        public double X
        {
            get { return this.x; }
        }

        /// <summary>
        /// Gets the Y position.
        /// </summary>
        public double Y
        {
            get { return this.y; }
        }

        /// <summary>
        /// Checks for equality between two <see cref="Point"/>s.
        /// </summary>
        /// <param name="left">The first point.</param>
        /// <param name="right">The second point.</param>
        /// <returns>True if the points are equal; otherwise false.</returns>
        public static bool operator ==(Point left, Point right)
        {
            return left.X == right.X && left.Y == right.Y;
        }

        /// <summary>
        /// Checks for unequality between two <see cref="Point"/>s.
        /// </summary>
        /// <param name="left">The first point.</param>
        /// <param name="right">The second point.</param>
        /// <returns>True if the points are unequal; otherwise false.</returns>
        public static bool operator !=(Point left, Point right)
        {
            return !(left == right);
        }

        public static Point operator +(Point a, Point b)
        {
            return new Point(a.x + b.x, a.y + b.y);
        }

        public static Point operator +(Point a, Vector b)
        {
            return new Point(a.x + b.X, a.y + b.Y);
        }

        public static Point operator -(Point a, Point b)
        {
            return new Point(a.x - b.x, a.y - b.y);
        }

        public static Point operator -(Point a, Vector b)
        {
            return new Point(a.x - b.X, a.y - b.Y);
        }

        public static Point operator *(Point point, Matrix matrix)
        {
            return new Point(
                (point.X * matrix.M11) + (point.Y * matrix.M21) + matrix.OffsetX,
                (point.X * matrix.M12) + (point.Y * matrix.M22) + matrix.OffsetY);
        }

        public static implicit operator Vector(Point p)
        {
            return new Vector(p.x, p.y);
        }

        public override bool Equals(object obj)
        {
            if (obj is Point)
            {
                var other = (Point)obj;
                return this.X == other.X && this.Y == other.Y;
            }

            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 23) + this.x.GetHashCode();
                hash = (hash * 23) + this.y.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Returns the string representation of the point.
        /// </summary>
        /// <returns>The string representation of the point.</returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}, {1}", this.x, this.y);
        }

        public Point WithX(double x)
        {
            return new Point(x, this.y);
        }

        public Point WithY(double y)
        {
            return new Point(this.x, y);
        }
    }
}
