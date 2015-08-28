﻿// -----------------------------------------------------------------------
// <copyright file="Matrix.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System;
    using System.Globalization;

    /// <summary>
    /// A 2x3 matrix.
    /// </summary>
    public struct Matrix
    {
        private double m11;
        private double m12;
        private double m21;
        private double m22;
        private double m31;
        private double m32;

        /// <summary>
        /// Initializes a new instance of the <see cref="Matrix"/> struct.
        /// </summary>
        /// <param name="m11">The first element of the first row.</param>
        /// <param name="m12">The second element of the first row.</param>
        /// <param name="m21">The first element of the second row.</param>
        /// <param name="m22">The second element of the second row.</param>
        /// <param name="offsetX">The first element of the third row.</param>
        /// <param name="offsetY">The second element of the third row.</param>
        public Matrix(
            double m11,
            double m12,
            double m21,
            double m22,
            double offsetX,
            double offsetY)
        {
            this.m11 = m11;
            this.m12 = m12;
            this.m21 = m21;
            this.m22 = m22;
            this.m31 = offsetX;
            this.m32 = offsetY;
        }

        /// <summary>
        /// Returns the multiplicative identity matrix.
        /// </summary>
        public static Matrix Identity
        {
            get { return new Matrix(1.0, 0.0, 0.0, 1.0, 0.0, 0.0); }
        }

        /// <summary>
        /// Returns whether the matrix is the identity matrix.
        /// </summary>
        public bool IsIdentity
        {
            get { return this.Equals(Matrix.Identity); }
        }

        /// <summary>
        /// The first element of the first row
        /// </summary>
        public double M11
        {
            get { return this.m11; }
        }

        /// <summary>
        /// The second element of the first row
        /// </summary>
        public double M12
        {
            get { return this.m12; }
        }

        /// <summary>
        /// The first element of the second row
        /// </summary>
        public double M21
        {
            get { return this.m21; }
        }

        /// <summary>
        /// The second element of the second row
        /// </summary>
        public double M22
        {
            get { return this.m22; }
        }

        /// <summary>
        /// The first element of the third row
        /// </summary>
        public double M31
        {
            get { return this.m31; }
        }

        /// <summary>
        /// The second element of the third row
        /// </summary>
        public double M32
        {
            get { return this.m32; }
        }

        /// <summary>
        /// Multiplies two matrices together and returns the resulting matrix.
        /// </summary>
        /// <param name="value1">The first source matrix.</param>
        /// <param name="value2">The second source matrix.</param>
        /// <returns>The product matrix.</returns>
        public static Matrix operator *(Matrix value1, Matrix value2)
        {
            return new Matrix(
                (value1.M11 * value2.M11) + (value1.M12 * value2.M21),
                (value1.M11 * value2.M12) + (value1.M12 * value2.M22),
                (value1.M21 * value2.M11) + (value1.M22 * value2.M21),
                (value1.M21 * value2.M12) + (value1.M22 * value2.M22),
                (value1.m31 * value2.M11) + (value1.m32 * value2.M21) + value2.m31,
                (value1.m31 * value2.M12) + (value1.m32 * value2.M22) + value2.m32);
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
        /// Creates a scale matrix from the given X and Y components.
        /// </summary>
        /// <param name="xScale">Value to scale by on the X-axis.</param>
        /// <param name="yScale">Value to scale by on the Y-axis.</param>
        /// <returns>A scaling matrix.</returns>
        public static Matrix CreateScale(double xScale, double yScale)
        {
            return CreateScale(new Vector(xScale, yScale));
        }

        /// <summary>
        /// Creates a scale matrix from the given vector scale.
        /// </summary>
        /// <param name="scales">The scale to use.</param>
        /// <returns>A scaling matrix.</returns>
        public static Matrix CreateScale(Vector scales)
        {
            return new Matrix(scales.X, 0, 0, scales.Y, 0, 0);
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
        /// Converts an ange in degrees to radians.
        /// </summary>
        /// <param name="angle">The angle in degrees.</param>
        /// <returns>The angle in radians.</returns>
        public static double ToRadians(double angle)
        {
            return angle * 0.0174532925;
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
            return (this.m11 * this.m22) - (this.m12 * this.m21);
        }

        /// <summary>
        /// Returns a boolean indicating whether the matrix is equal to the other given matrix.
        /// </summary>
        /// <param name="other">The other matrix to test equality against.</param>
        /// <returns>True if this matrix is equal to other; False otherwise.</returns>
        public bool Equals(Matrix other)
        {
            return this.m11 == other.M11 &&
                   this.m12 == other.M12 &&
                   this.m21 == other.M21 &&
                   this.m22 == other.M22 &&
                   this.m31 == other.M31 &&
                   this.m32 == other.M32;
        }

        /// <summary>
        /// Returns a boolean indicating whether the given Object is equal to this matrix instance.
        /// </summary>
        /// <param name="obj">The Object to compare against.</param>
        /// <returns>True if the Object is equal to this matrix; False otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is Matrix))
            {
                return false;
            }

            return this.Equals((Matrix)obj);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return this.M11.GetHashCode() + this.M12.GetHashCode() +
                   this.M21.GetHashCode() + this.M22.GetHashCode() +
                   this.M31.GetHashCode() + this.M32.GetHashCode();
        }

        /// <summary>
        /// Returns a String representing this matrix instance.
        /// </summary>
        /// <returns>The string representation.</returns>
        public override string ToString()
        {
            CultureInfo ci = CultureInfo.CurrentCulture;
            return string.Format(
                ci,
                "{{ {{M11:{0} M12:{1}}} {{M21:{2} M22:{3}}} {{M31:{4} M32:{5}}} }}",
                this.M11.ToString(ci),
                this.M12.ToString(ci),
                this.M21.ToString(ci),
                this.M22.ToString(ci),
                this.M31.ToString(ci),
                this.M32.ToString(ci));
        }

        /// <summary>
        /// Inverts the Matrix.
        /// </summary>
        /// <returns>The inverted matrix.</returns>
        public Matrix Invert()
        {
            if (this.GetDeterminant() == 0)
            {
                throw new InvalidOperationException("Transform is not invertible.");
            }

            double d = this.GetDeterminant();

            return new Matrix(
                this.m22 / d,
                -this.m12 / d,
                -this.m21 / d,
                this.m11 / d,
                ((this.m21 * this.m32) - (this.m22 * this.m31)) / d,
                ((this.m12 * this.m31) - (this.m11 * this.m32)) / d);
        }
    }
}