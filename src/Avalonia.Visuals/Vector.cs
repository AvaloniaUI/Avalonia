// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;
using Avalonia.Animation.Animators;
using JetBrains.Annotations;

namespace Avalonia
{
    /// <summary>
    /// Defines a vector.
    /// </summary>
    public readonly struct Vector
    {
        static Vector()
        {
            Animation.Animation.RegisterAnimator<VectorAnimator>(prop => typeof(Vector).IsAssignableFrom(prop.PropertyType));
        }

        /// <summary>
        /// The X vector.
        /// </summary>
        private readonly double _x;

        /// <summary>
        /// The Y vector.
        /// </summary>
        private readonly double _y;

        /// <summary>
        /// Initializes a new instance of the <see cref="Vector"/> structure.
        /// </summary>
        /// <param name="x">The X vector.</param>
        /// <param name="y">The Y vector.</param>
        public Vector(double x, double y)
        {
            _x = x;
            _y = y;
        }

        /// <summary>
        /// Gets the X vector.
        /// </summary>
        public double X => _x;

        /// <summary>
        /// Gets the Y vector.
        /// </summary>
        public double Y => _y;

        /// <summary>
        /// Converts the <see cref="Vector"/> to a <see cref="Point"/>.
        /// </summary>
        /// <param name="a">The vector.</param>
        public static explicit operator Point(Vector a)
        {
            return new Point(a._x, a._y);
        }

        /// <summary>
        /// Calculates the dot product of two vectors
        /// </summary>
        /// <param name="a">First vector</param>
        /// <param name="b">Second vector</param>
        /// <returns>The dot product</returns>
        public static double operator *(Vector a, Vector b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        /// <summary>
        /// Scales a vector.
        /// </summary>
        /// <param name="vector">The vector</param>
        /// <param name="scale">The scaling factor.</param>
        /// <returns>The scaled vector.</returns>
        public static Vector operator *(Vector vector, double scale)
        {
            return new Vector(vector._x * scale, vector._y * scale);
        }

        /// <summary>
        /// Scales a vector.
        /// </summary>
        /// <param name="vector">The vector</param>
        /// <param name="scale">The divisor.</param>
        /// <returns>The scaled vector.</returns>
        public static Vector operator /(Vector vector, double scale)
        {
            return new Vector(vector._x / scale, vector._y / scale);
        }

        /// <summary>
        /// Length of the vector
        /// </summary>
        public double Length => Math.Sqrt(X * X + Y * Y);

        /// <summary>
        /// Negates a vector.
        /// </summary>
        /// <param name="a">The vector.</param>
        /// <returns>The negated vector.</returns>
        public static Vector operator -(Vector a)
        {
            return new Vector(-a._x, -a._y);
        }

        /// <summary>
        /// Adds two vectors.
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>A vector that is the result of the addition.</returns>
        public static Vector operator +(Vector a, Vector b)
        {
            return new Vector(a._x + b._x, a._y + b._y);
        }

        /// <summary>
        /// Subtracts two vectors.
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>A vector that is the result of the subtraction.</returns>
        public static Vector operator -(Vector a, Vector b)
        {
            return new Vector(a._x - b._x, a._y - b._y);
        }

        /// <summary>
        /// Check if two vectors are equal (bitwise).
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(Vector other)
        {
            // ReSharper disable CompareOfFloatsByEqualityOperator
            return _x == other._x && _y == other._y;
            // ReSharper restore CompareOfFloatsByEqualityOperator
        }

        /// <summary>
        /// Check if two vectors are nearly equal (numerically).
        /// </summary>
        /// <param name="other">The other vector.</param>
        /// <returns>True if vectors are nearly equal.</returns>
        [Pure]
        public bool NearlyEquals(Vector other)
        {
            const float tolerance = float.Epsilon;

            return Math.Abs(_x - other._x) < tolerance && Math.Abs(_y - other._y) < tolerance;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;

            return obj is Vector vector && Equals(vector);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_x.GetHashCode() * 397) ^ _y.GetHashCode();
            }
        }

        public static bool operator ==(Vector left, Vector right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Vector left, Vector right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Returns the string representation of the point.
        /// </summary>
        /// <returns>The string representation of the point.</returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}, {1}", _x, _y);
        }

        /// <summary>
        /// Returns a new vector with the specified X coordinate.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <returns>The new vector.</returns>
        public Vector WithX(double x)
        {
            return new Vector(x, _y);
        }

        /// <summary>
        /// Returns a new vector with the specified Y coordinate.
        /// </summary>
        /// <param name="y">The Y coordinate.</param>
        /// <returns>The new vector.</returns>
        public Vector WithY(double y)
        {
            return new Vector(_x, y);
        }
    }
}
