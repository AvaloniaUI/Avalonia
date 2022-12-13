using System;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Avalonia.Utilities;

namespace Avalonia
{
    /// <summary>
    /// A 3x3 matrix.
    /// </summary>
    /// <remarks>Matrix layout:
    ///         | 1st col | 2nd col | 3r col |
    /// 1st row | scaleX  | skewY  | perspX  |
    /// 2nd row | skewX  | scaleY  | perspY  |
    /// 3rd row | transX  | transY  | perspZ  |
    /// 
    /// Note: Skia.SkMatrix uses a transposed layout (where for example skewX/skewY and persp0/transX are swapped).
    /// </remarks>
#if !BUILDTASK
    public
#endif
    readonly struct Matrix : IEquatable<Matrix>
    {
        private readonly double _m11;
        private readonly double _m12;
        private readonly double _m13;
        private readonly double _m21;
        private readonly double _m22;
        private readonly double _m23;
        private readonly double _m31;
        private readonly double _m32;
        private readonly double _m33;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Matrix"/> struct (equivalent to a 2x3 Matrix without perspective).
        /// </summary>
        /// <param name="scaleX">The first element of the first row.</param>
        /// <param name="skewY">The second element of the first row.</param>
        /// <param name="skewX">The first element of the second row.</param>
        /// <param name="scaleY">The second element of the second row.</param>
        /// <param name="offsetX">The first element of the third row.</param>
        /// <param name="offsetY">The second element of the third row.</param>
        public Matrix(
            double scaleX,
            double skewY,
            double skewX,
            double scaleY,
            double offsetX,
            double offsetY) : this( scaleX, skewY, 0, skewX, scaleY, 0, offsetX, offsetY, 1)
        {
        }



        /// <summary>
        /// Initializes a new instance of the <see cref="Matrix"/> struct.
        /// </summary>
        /// <param name="scaleX">The first element of the first row.</param>
        /// <param name="skewY">The second element of the first row.</param>
        /// <param name="perspX">The third element of the first row.</param>
        /// <param name="skewX">The first element of the second row.</param>
        /// <param name="scaleY">The second element of the second row.</param>
        /// <param name="perspY">The third element of the second row.</param>
        /// <param name="offsetX">The first element of the third row.</param>
        /// <param name="offsetY">The second element of the third row.</param>
        /// <param name="perspZ">The third element of the third row.</param>
        public Matrix(
            double scaleX,
            double skewY,
            double perspX,
            double skewX,
            double scaleY,
            double perspY, 
            double offsetX,
            double offsetY,
            double perspZ)
        {
            _m11 = scaleX;
            _m12 = skewY;
            _m13 = perspX;
            _m21 = skewX;
            _m22 = scaleY;
            _m23 = perspY;
            _m31 = offsetX;
            _m32 = offsetY;
            _m33 = perspZ;
        }

        /// <summary>
        /// Returns the multiplicative identity matrix.
        /// </summary>
        public static Matrix Identity { get; } = new Matrix(
            1.0, 0.0, 0.0,
            0.0, 1.0, 0.0,
            0.0, 0.0, 1.0);

        /// <summary>
        /// Returns whether the matrix is the identity matrix.
        /// </summary>
        public bool IsIdentity => Equals(Identity);

        /// <summary>
        /// HasInverse Property - returns true if this matrix is invertible, false otherwise.
        /// </summary>
        public bool HasInverse => !MathUtilities.IsZero(GetDeterminant());

        /// <summary>
        /// The first element of the first row (scaleX).
        /// </summary>
        public double M11 => _m11;

        /// <summary>
        /// The second element of the first row (skewY).
        /// </summary>
        public double M12 => _m12;

        /// <summary>
        /// The third element of the first row (perspX: input x-axis perspective factor).
        /// </summary>
        public double M13 => _m13;

        /// <summary>
        /// The first element of the second row (skewX).
        /// </summary>
        public double M21 => _m21;

        /// <summary>
        /// The second element of the second row (scaleY).
        /// </summary>
        public double M22 => _m22;

        /// <summary>
        /// The third element of the second row (perspY: input y-axis perspective factor).
        /// </summary>
        public double M23 => _m23;

        /// <summary>
        /// The first element of the third row (offsetX/translateX).
        /// </summary>
        public double M31 => _m31;

        /// <summary>
        /// The second element of the third row (offsetY/translateY).
        /// </summary>
        public double M32 => _m32;

        /// <summary>
        /// The third element of the third row (perspZ: perspective scale factor).
        /// </summary>
        public double M33 => _m33;

        /// <summary>
        /// Multiplies two matrices together and returns the resulting matrix.
        /// </summary>
        /// <param name="value1">The first source matrix.</param>
        /// <param name="value2">The second source matrix.</param>
        /// <returns>The product matrix.</returns>
        public static Matrix operator *(Matrix value1, Matrix value2)
        {
            return new Matrix(
                (value1.M11 * value2.M11) + (value1.M12 * value2.M21) + (value1.M13 * value2.M31),
                (value1.M11 * value2.M12) + (value1.M12 * value2.M22) + (value1.M13 * value2.M32),
                (value1.M11 * value2.M13) + (value1.M12 * value2.M23) + (value1.M13 * value2.M33),
                (value1.M21 * value2.M11) + (value1.M22 * value2.M21) + (value1.M23 * value2.M31),
                (value1.M21 * value2.M12) + (value1.M22 * value2.M22) + (value1.M23 * value2.M32),
                (value1.M21 * value2.M13) + (value1.M22 * value2.M23) + (value1.M23 * value2.M33),
                (value1.M31 * value2.M11) + (value1.M32 * value2.M21) + (value1.M33 * value2.M31),
                (value1.M31 * value2.M12) + (value1.M32 * value2.M22) + (value1.M33 * value2.M32), 
                (value1.M31 * value2.M13) + (value1.M32 * value2.M23) + (value1.M33 * value2.M33));
        }

        /// <summary>
        /// Negates the given matrix by multiplying all values by -1.
        /// </summary>
        /// <param name="value">The source matrix.</param>
        /// <returns>The negated matrix.</returns>
        public static Matrix operator -(Matrix value)
        {
            return value.Invert();
        }

        /// <summary>
        /// Returns a boolean indicating whether the given matrices are equal.
        /// </summary>
        /// <param name="value1">The first source matrix.</param>
        /// <param name="value2">The second source matrix.</param>
        /// <returns>True if the matrices are equal; False otherwise.</returns>
        public static bool operator ==(Matrix value1, Matrix value2)
        {
            return value1.Equals(value2);
        }

        /// <summary>
        /// Returns a boolean indicating whether the given matrices are not equal.
        /// </summary>
        /// <param name="value1">The first source matrix.</param>
        /// <param name="value2">The second source matrix.</param>
        /// <returns>True if the matrices are not equal; False if they are equal.</returns>
        public static bool operator !=(Matrix value1, Matrix value2)
        {
            return !value1.Equals(value2);
        }

        /// <summary>
        /// Creates a rotation matrix using the given rotation in radians.
        /// </summary>
        /// <param name="radians">The amount of rotation, in radians.</param>
        /// <returns>A rotation matrix.</returns>
        public static Matrix CreateRotation(double radians)
        {
            double cos = Math.Cos(radians);
            double sin = Math.Sin(radians);
            return new Matrix(cos, sin, -sin, cos, 0, 0);
        }

        /// <summary>
        /// Creates a skew matrix from the given axis skew angles in radians.
        /// </summary>
        /// <param name="xAngle">The amount of skew along the X-axis, in radians.</param>
        /// <param name="yAngle">The amount of skew along the Y-axis, in radians.</param>
        /// <returns>A rotation matrix.</returns>
        public static Matrix CreateSkew(double xAngle, double yAngle)
        {
            double tanX = Math.Tan(xAngle);
            double tanY = Math.Tan(yAngle);
            return new Matrix(1.0, tanY, tanX, 1.0, 0.0, 0.0);
        }

        /// <summary>
        /// Creates a scale matrix from the given X and Y components.
        /// </summary>
        /// <param name="xScale">Value to scale by on the X-axis.</param>
        /// <param name="yScale">Value to scale by on the Y-axis.</param>
        /// <returns>A scaling matrix.</returns>
        public static Matrix CreateScale(double xScale, double yScale)
        {
            return new Matrix(xScale, 0, 0, yScale, 0, 0);
        }

        /// <summary>
        /// Creates a scale matrix from the given vector scale.
        /// </summary>
        /// <param name="scales">The scale to use.</param>
        /// <returns>A scaling matrix.</returns>
        public static Matrix CreateScale(Vector scales)
        {
            return CreateScale(scales.X, scales.Y);
        }

        /// <summary>
        /// Creates a translation matrix from the given vector.
        /// </summary>
        /// <param name="position">The translation position.</param>
        /// <returns>A translation matrix.</returns>
        public static Matrix CreateTranslation(Vector position)
        {
            return CreateTranslation(position.X, position.Y);
        }

        /// <summary>
        /// Creates a translation matrix from the given X and Y components.
        /// </summary>
        /// <param name="xPosition">The X position.</param>
        /// <param name="yPosition">The Y position.</param>
        /// <returns>A translation matrix.</returns>
        public static Matrix CreateTranslation(double xPosition, double yPosition)
        {
            return new Matrix(1.0, 0.0, 0.0, 1.0, xPosition, yPosition);
        }

        /// <summary>
        /// Converts an angle in degrees to radians.
        /// </summary>
        /// <param name="angle">The angle in degrees.</param>
        /// <returns>The angle in radians.</returns>
        public static double ToRadians(double angle)
        {
            return angle * 0.0174532925;
        }
        
        /// <summary>
        /// Appends another matrix as post-multiplication operation.
        /// Equivalent to this * value;
        /// </summary>
        /// <param name="value">A matrix.</param>
        /// <returns>Post-multiplied matrix.</returns>
        public Matrix Append(Matrix value)
        {
            return this * value;
        }

        /// <summary>
        /// Prepends another matrix as pre-multiplication operation.
        /// Equivalent to value * this;
        /// </summary>
        /// <param name="value">A matrix.</param>
        /// <returns>Pre-multiplied matrix.</returns>
        public Matrix Prepend(Matrix value)
        {
            return value * this;
        }

        /// <summary>
        /// Calculates the determinant for this matrix.
        /// </summary>
        /// <returns>The determinant.</returns>
        /// <remarks>
        /// The determinant is calculated by expanding the matrix with a third column whose
        /// values are (0,0,1).
        /// </remarks>
        public double GetDeterminant()
        {
            // implemented using "Laplace expansion":
            return _m11 * (_m22 * _m33 - _m23 * _m32)
                 - _m12 * (_m21 * _m33 - _m23 * _m31)
                 + _m13 * (_m21 * _m32 - _m22 * _m31);
        }

        /// <summary>
        ///  Transforms the point with the matrix
        /// </summary>
        /// <param name="p">The point to be transformed</param>
        /// <returns>The transformed point</returns>
        public Point Transform(Point p)
        {
            Point transformedResult;
            
            // If this matrix contains a non-affine transform with need to extend
            // the point to a 3D vector and flatten it back for 2d display
            // by multiplying X and Y with the inverse of the Z axis.
            // The code below also works with affine transformations, but for performance (and compatibility)
            // reasons we will use the more complex calculation only if necessary
            if (ContainsPerspective())
            {
                var m44 = new Matrix4x4(
                    (float)M11, (float)M12, (float)M13, 0,
                    (float)M21, (float)M22, (float)M23, 0,
                    (float)M31, (float)M32, (float)M33, 0,
                    0, 0, 0, 1
                );
            
                var vector = new Vector3((float)p.X, (float)p.Y, 1);
                var transformedVector = Vector3.Transform(vector, m44);
                var z = 1 / transformedVector.Z;
            
                transformedResult = new Point(transformedVector.X * z, transformedVector.Y * z);
            }
            else
            {
                return new Point(
                    (p.X * M11) + (p.Y * M21) + M31,
                    (p.X * M12) + (p.Y * M22) + M32);
            }

            return transformedResult;
        }

        /// <summary>
        /// Returns a boolean indicating whether the matrix is equal to the other given matrix.
        /// </summary>
        /// <param name="other">The other matrix to test equality against.</param>
        /// <returns>True if this matrix is equal to other; False otherwise.</returns>
        public bool Equals(Matrix other)
        {
            // ReSharper disable CompareOfFloatsByEqualityOperator
            return _m11 == other.M11 &&
                   _m12 == other.M12 &&
                   _m13 == other.M13 &&
                   _m21 == other.M21 &&
                   _m22 == other.M22 &&
                   _m23 == other.M23 &&
                   _m31 == other.M31 &&
                   _m32 == other.M32 &&
                   _m33 == other.M33;
            // ReSharper restore CompareOfFloatsByEqualityOperator
        }

        /// <summary>
        /// Returns a boolean indicating whether the given Object is equal to this matrix instance.
        /// </summary>
        /// <param name="obj">The Object to compare against.</param>
        /// <returns>True if the Object is equal to this matrix; False otherwise.</returns>
        public override bool Equals(object? obj) => obj is Matrix other && Equals(other);

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return (_m11, _m12, _m13, _m21, _m22, _m23, _m31, _m32, _m33).GetHashCode();
        }

        /// <summary>
        ///  Determines if the current matrix contains perspective (non-affine) transforms (true) or only (affine) transforms that could be mapped into an 2x3 matrix (false).
        /// </summary>
        public bool ContainsPerspective()
        {

            // ReSharper disable CompareOfFloatsByEqualityOperator
            return _m13 != 0 || _m23 != 0 || _m33 != 1;
            // ReSharper restore CompareOfFloatsByEqualityOperator
        }

        /// <summary>
        /// Returns a String representing this matrix instance.
        /// </summary>
        /// <returns>The string representation.</returns>
        public override string ToString()
        {
            CultureInfo ci = CultureInfo.CurrentCulture;

            string msg;
            double[] values;

            if (ContainsPerspective())
            {
                msg = "{{ {{M11:{0} M12:{1} M13:{2}}} {{M21:{3} M22:{4} M23:{5}}} {{M31:{6} M32:{7} M33:{8}}} }}";
                values = new[] { M11, M12, M13, M21, M22, M23, M31, M32, M33 };
            }
            else
            {
                msg = "{{ {{M11:{0} M12:{1}}} {{M21:{2} M22:{3}}} {{M31:{4} M32:{5}}} }}";
                values = new[] { M11, M12, M21, M22, M31, M32 };
            }

            return string.Format(
                ci,
                msg,
                values.Select((v) => v.ToString(ci)).ToArray());
        }

        /// <summary>
        /// Attempts to invert the Matrix.
        /// </summary>
        /// <returns>The inverted matrix or <see langword="null"/> when matrix is not invertible.</returns>
        public bool TryInvert(out Matrix inverted)
        {
            double d = GetDeterminant();

            if (MathUtilities.IsZero(d))
            {
                inverted = default;
                
                return false;
            }

            var invdet = 1 / d;
            
            inverted = new Matrix(
                (_m22 * _m33 - _m32 * _m23) * invdet,
                (_m13 * _m32 - _m12 * _m33) * invdet,
                (_m12 * _m23 - _m13 * _m22) * invdet,
                (_m23 * _m31 - _m21 * _m33) * invdet,
                (_m11 * _m33 - _m13 * _m31) * invdet,
                (_m21 * _m13 - _m11 * _m23) * invdet,
                (_m21 * _m32 - _m31 * _m22) * invdet,
                (_m31 * _m12 - _m11 * _m32) * invdet,
                (_m11 * _m22 - _m21 * _m12) * invdet
                );
            
            return true;
        }

        /// <summary>
        /// Inverts the Matrix.
        /// </summary>
        /// <exception cref="InvalidOperationException">Matrix is not invertible.</exception>
        /// <returns>The inverted matrix.</returns>
        public Matrix Invert()
        {
            if (!TryInvert(out var inverted))
            {
                throw new InvalidOperationException("Transform is not invertible.");
            }

            return inverted;
        }

        /// <summary>
        /// Parses a <see cref="Matrix"/> string.
        /// </summary>
        /// <param name="s">Six or nine comma-delimited double values (m11, m12, m21, m22, offsetX, offsetY[, perspX, perspY, perspZ]) that describe the new <see cref="Matrix"/></param>
        /// <returns>The <see cref="Matrix"/>.</returns>
        public static Matrix Parse(string s)
        {
            // initialize to satisfy compiler - only used when retrieved from string.
            double v8 = 0;
            double v9 = 0;

            using (var tokenizer = new StringTokenizer(s, CultureInfo.InvariantCulture, exceptionMessage: "Invalid Matrix."))
            {
                var v1 = tokenizer.ReadDouble();
                var v2 = tokenizer.ReadDouble();
                var v3 = tokenizer.ReadDouble();
                var v4 = tokenizer.ReadDouble();
                var v5 = tokenizer.ReadDouble();
                var v6 = tokenizer.ReadDouble();
                var persp = tokenizer.TryReadDouble(out var v7);
                persp = persp && tokenizer.TryReadDouble(out v8);
                persp = persp && tokenizer.TryReadDouble(out v9);

                if (persp) 
                    return new Matrix(v1, v2, v7, v3, v4, v8, v5, v6, v9);
                else
                    return new Matrix(v1, v2, v3, v4, v5, v6);
            }
        }

        /// <summary>
        /// Decomposes given matrix into transform operations.
        /// </summary>
        /// <param name="matrix">Matrix to decompose.</param>
        /// <param name="decomposed">Decomposed matrix.</param>
        /// <returns>The status of the operation.</returns>        
        public static bool TryDecomposeTransform(Matrix matrix, out Decomposed decomposed)
        {
            decomposed = default;

            var determinant = matrix.GetDeterminant();
            
            if (MathUtilities.IsZero(determinant) || matrix.ContainsPerspective())
            {
                return false;
            }

            var m11 = matrix.M11;
            var m21 = matrix.M21;
            var m12 = matrix.M12;
            var m22 = matrix.M22;

            // Translation.
            decomposed.Translate = new Vector(matrix.M31, matrix.M32);

            // Scale sign.
            var scaleX = 1d;
            var scaleY = 1d;

            if (determinant < 0)
            {
                if (m11 < m22)
                {
                    scaleX *= -1d;
                }
                else
                {
                    scaleY *= -1d;
                }
            }

            // X Scale.
            scaleX *= Math.Sqrt(m11 * m11 + m12 * m12);

            m11 /= scaleX;
            m12 /= scaleX;

            // XY Shear.
            double scaledShear = m11 * m21 + m12 * m22;

            m21 -= m11 * scaledShear;
            m22 -= m12 * scaledShear;

            // Y Scale.
            scaleY *= Math.Sqrt(m21 * m21 + m22 * m22);

            decomposed.Scale = new Vector(scaleX, scaleY);
            decomposed.Skew = new Vector(scaledShear / scaleY, 0d);
            decomposed.Angle = Math.Atan2(m12, m11);

            return true;
        }

        public record struct Decomposed
        {
            public Vector Translate;
            public Vector Scale;
            public Vector Skew;
            public double Angle;
        }
    }
}
