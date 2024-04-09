using System;
using System.Globalization;
using System.Numerics;

# if NET6_0_OR_GREATER
using Silk.NET.Maths;
# endif
#if !BUILDTASK
using Avalonia.Animation.Animators;
#endif
using Avalonia.Utilities;

#nullable enable

namespace Avalonia
{

#if NET6_0_OR_GREATER
    /// <summary>
    /// Defines a vector.
    /// </summary>
#if !BUILDTASK
    public
#endif

    readonly struct Vector : IEquatable<Vector>
    {

        private readonly Vector2D<double> _inner;



        /// <summary>
        /// Initializes a new instance of the <see cref="Vector"/> structure.
        /// </summary>
        /// <param name="x">The X component.</param>
        /// <param name="y">The Y component.</param>
        public Vector(double x, double y)
        {
            _inner = new Vector2D<double>(x, y);
        }

        public Vector(Vector2D<double> v)
        {
            _inner = v;
        }

        

        /// <summary>
        /// Gets the X component.
        /// </summary>
        public double X => _inner.X;

        /// <summary>
        /// Gets the Y component.
        /// </summary>
        public double Y => _inner.Y;

        /// <summary>
        /// Converts the <see cref="Vector"/> to a <see cref="Point"/>.
        /// </summary>
        /// <param name="a">The vector.</param>
        public static explicit operator Point(Vector a)
        {
            return new Point(a._inner.X, a._inner.Y);
        }


        /// <summary>
        /// Convert the <see cref="Vector2D<double/>"/> to a <see cref="Vector"/>
        /// </summary>
        /// <param name="a"></param>
        public static explicit operator Vector(Vector2D<double> a)
        {
            return new Vector(a);
        }

        /// <summary>
        /// Calculates the dot product of two vectors.
        /// </summary>
        /// <param name="a">First vector.</param>
        /// <param name="b">Second vector.</param>
        /// <returns>The dot product.</returns>
        public static double operator *(Vector a, Vector b)
            => Dot(a, b);

        /// <summary>
        /// Scales a vector.
        /// </summary>
        /// <param name="vector">The vector.</param>
        /// <param name="scale">The scaling factor.</param>
        /// <returns>The scaled vector.</returns>
        public static Vector operator *(Vector vector, double scale)
            => Multiply(vector, scale);

        /// <summary>
        /// Scales a vector.
        /// </summary>
        /// <param name="vector">The vector.</param>
        /// <param name="scale">The scaling factor.</param>
        /// <returns>The scaled vector.</returns>
        public static Vector operator *(double scale, Vector vector)
            => Multiply(vector, scale);

        /// <summary>
        /// Scales a vector.
        /// </summary>
        /// <param name="vector">The vector.</param>
        /// <param name="scale">The divisor.</param>
        /// <returns>The scaled vector.</returns>
        public static Vector operator /(Vector vector, double scale)
            => Divide(vector, scale);

        /// <summary>
        /// Parses a <see cref="Vector"/> string.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>The <see cref="Vector"/>.</returns>
        public static Vector Parse(string s)
        {
            using (var tokenizer = new StringTokenizer(s, CultureInfo.InvariantCulture, exceptionMessage: "Invalid Vector."))
            {
                return new Vector(
                    tokenizer.ReadDouble(),
                    tokenizer.ReadDouble()
                );
            }
        }

        /// <summary>
        /// Length of the vector.
        /// </summary>
        public double Length => Math.Sqrt(SquaredLength);

        /// <summary>
        /// Squared Length of the vector.
        /// </summary>
        public double SquaredLength =>  Vector2D.Dot(_inner,_inner);

        /// <summary>
        /// Negates a vector.
        /// </summary>
        /// <param name="a">The vector.</param>
        /// <returns>The negated vector.</returns>
        public static Vector operator -(Vector a)
            => Negate(a);

        /// <summary>
        /// Adds two vectors.
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>A vector that is the result of the addition.</returns>
        public static Vector operator +(Vector a, Vector b)
            => Add(a, b);

        /// <summary>
        /// Subtracts two vectors.
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>A vector that is the result of the subtraction.</returns>
        public static Vector operator -(Vector a, Vector b)
            => Subtract(a, b);

        /// <summary>
        /// Check if two vectors are equal (bitwise).
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(Vector other)
        {
            // ReSharper disable CompareOfFloatsByEqualityOperator
            return _inner==other._inner;
            // ReSharper restore CompareOfFloatsByEqualityOperator
        }

        /// <summary>
        /// Check if two vectors are nearly equal (numerically).
        /// </summary>
        /// <param name="other">The other vector.</param>
        /// <returns>True if vectors are nearly equal.</returns>
        public bool NearlyEquals(Vector other)
        {
            return MathUtilities.AreClose(_inner.X, other._inner.X) &&
                   MathUtilities.AreClose(_inner.Y, other._inner.Y);
        }

        public override bool Equals(object? obj) => obj is Vector other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (_inner.X.GetHashCode() * 397) ^ _inner.Y.GetHashCode();
            }
        }

        public static bool operator ==(Vector left, Vector right)
        {
            return left._inner.Equals(right._inner);
        }

        public static bool operator !=(Vector left, Vector right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Returns the string representation of the vector.
        /// </summary>
        /// <returns>The string representation of the vector.</returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}, {1}", _inner.X, _inner.Y);
        }

        /// <summary>
        /// Returns a new vector with the specified X component.
        /// </summary>
        /// <param name="x">The X component.</param>
        /// <returns>The new vector.</returns>
        public Vector WithX(double x)
        {
            return new Vector(x, _inner.Y);
        }

        /// <summary>
        /// Returns a new vector with the specified Y component.
        /// </summary>
        /// <param name="y">The Y component.</param>
        /// <returns>The new vector.</returns>
        public Vector WithY(double y)
        {
            return new Vector(_inner.X, y);
        }

        /// <summary>
        /// Returns a normalized version of this vector.
        /// </summary>
        /// <returns>The normalized vector.</returns>
        public Vector Normalize()
            => Normalize(this);

        /// <summary>
        /// Returns a negated version of this vector.
        /// </summary>
        /// <returns>The negated vector.</returns>
        public Vector Negate()
            => Negate(this);

        /// <summary>
        /// Returns the dot product of two vectors.
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>The dot product.</returns>
        public static double Dot(Vector a, Vector b)
            => Vector2D.Dot( a._inner, b._inner);

        /// <summary>
        /// Returns the cross product of two vectors.
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>The cross product.</returns>
        public static double Cross(Vector a, Vector b)
            => a._inner.X * b._inner.Y - a._inner.Y * b._inner.X;

        /// <summary>
        /// Normalizes the given vector.
        /// </summary>
        /// <param name="vector">The vector</param>
        /// <returns>The normalized vector.</returns>
        public static Vector Normalize(Vector vector)
            => new(vector._inner/vector.Length);

        /// <summary>
        /// Divides the first vector by the second.
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>The scaled vector.</returns>
        public static Vector Divide(Vector a, Vector b)
            => new Vector(Vector2D.Divide(a._inner, b._inner));

        /// <summary>
        /// Divides the vector by the given scalar.
        /// </summary>
        /// <param name="vector">The vector</param>
        /// <param name="scalar">The scalar value</param>
        /// <returns>The scaled vector.</returns>
        public static Vector Divide(Vector vector, double scalar)
            => new Vector(vector._inner/scalar);

        /// <summary>
        /// Multiplies the first vector by the second.
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>The scaled vector.</returns>
        public static Vector Multiply(Vector a, Vector b)
            => new Vector(Vector2D.Multiply(a._inner,b._inner));

        /// <summary>
        /// Multiplies the vector by the given scalar.
        /// </summary>
        /// <param name="vector">The vector</param>
        /// <param name="scalar">The scalar value</param>
        /// <returns>The scaled vector.</returns>
        public static Vector Multiply(Vector vector, double scalar)
            => new Vector(vector._inner * scalar);

        /// <summary>
        /// Adds the second to the first vector
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>The summed vector.</returns>
        public static Vector Add(Vector a, Vector b)
            => new Vector(a._inner + b._inner);

        /// <summary>
        /// Subtracts the second from the first vector
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>The difference vector.</returns>
        public static Vector Subtract(Vector a, Vector b)
            => new Vector(a._inner - b._inner);

        /// <summary>
        /// Negates the vector
        /// </summary>
        /// <param name="vector">The vector to negate.</param>
        /// <returns>The scaled vector.</returns>
        public static Vector Negate(Vector vector)
            => new Vector(-vector._inner);

        /// <summary>
        /// Returns the vector (0.0, 0.0).
        /// </summary>
        public static Vector Zero
            => new Vector(0, 0);

        /// <summary>
        /// Returns the vector (1.0, 1.0).
        /// </summary>
        public static Vector One
            => new Vector(1, 1);

        /// <summary>
        /// Returns the vector (1.0, 0.0).
        /// </summary>
        public static Vector UnitX
            => new Vector(1, 0);

        /// <summary>
        /// Returns the vector (0.0, 1.0).
        /// </summary>
        public static Vector UnitY
            => new Vector(0, 1);

        /// <summary>
        /// Deconstructs the vector into its X and Y components.
        /// </summary>
        /// <param name="x">The X component.</param>
        /// <param name="y">The Y component.</param>
        public void Deconstruct(out double x, out double y)
        {
            x = this._inner.X;
            y = this._inner.Y;
        }

        internal Vector2 ToVector2() => new Vector2((float)X, (float)Y);

        internal Vector(Vector2 v) : this(v.X, v.Y)
        {

        }

        /// <summary>
        /// Returns a vector whose elements are the absolute values of each of the specified vector's elements.
        /// </summary>
        /// <returns></returns>
        public Vector Abs() => new(Vector2D.Abs(_inner));

        /// <summary>
        /// Restricts a vector between a minimum and a maximum value.
        /// </summary>
        public static Vector Clamp(Vector value, Vector min, Vector max) =>
            new(Vector2D.Clamp<double>(value._inner, min._inner, max._inner));

        /// <summary>
        /// Returns a vector whose elements are the maximum of each of the pairs of elements in two specified vectors
        /// </summary>
        public static Vector Max(Vector left, Vector right) =>
            new(Vector2D.Max<double>(left._inner, right._inner));

        /// <summary>
        /// Returns a vector whose elements are the minimum of each of the pairs of elements in two specified vectors
        /// </summary>
        public static Vector Min(Vector left, Vector right) =>
            new(Math.Min(left.X, right.X), Math.Min(left.Y, right.Y));

        /// <summary>
        /// Computes the Euclidean distance between the two given points.
        /// </summary>
        /// <param name="value1">The first point.</param>
        /// <param name="value2">The second point.</param>
        /// <returns>The Euclidean distance.</returns>
        public static double Distance(Vector value1, Vector value2) => Math.Sqrt(DistanceSquared(value1, value2));

        /// <summary>
        /// Returns the Euclidean distance squared between two specified points
        /// </summary>
        /// <param name="value1">The first point.</param>
        /// <param name="value2">The second point.</param>
        /// <returns>The Euclidean distance squared.</returns>
        public static double DistanceSquared(Vector value1, Vector value2)
        {
            var difference = value1 - value2;
            return Dot(difference, difference);
        }

        public static implicit operator Vector(Vector2 v) => new(v);
    }
#endif
}
