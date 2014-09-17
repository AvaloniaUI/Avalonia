// -----------------------------------------------------------------------
// <copyright file="Matrix.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System;

    public struct Matrix
    {
        private double m11;
        private double m12;
        private double m21;
        private double m22;
        private double offsetX;
        private double offsetY;

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
            this.offsetX = offsetX;
            this.offsetY = offsetY;
        }

        public static Matrix Identity
        {
            get { return new Matrix(1.0, 0.0, 0.0, 1.0, 0.0, 0.0); }
        }

        public double Determinant
        {
            get { return (this.m11 * this.m22) - (this.m12 * this.m21); }
        }

        public bool HasInverse
        {
            get { return this.Determinant != 0; }
        }

        public bool IsIdentity
        {
            get { return this.Equals(Matrix.Identity); }
        }

        public double M11
        {
            get { return this.m11; }
        }

        public double M12
        {
            get { return this.m12; }
        }

        public double M21
        {
            get { return this.m21; }
        }

        public double M22
        {
            get { return this.m22; }
        }

        public double OffsetX
        {
            get { return this.offsetX; }
        }

        public double OffsetY
        {
            get { return this.offsetY; }
        }

        public static Matrix operator *(Matrix left, Matrix right)
        {
            return new Matrix(
                (left.M11 * right.M11) + (left.M12 * right.M21),
                (left.M11 * right.M12) + (left.M12 * right.M22),
                (left.M21 * right.M11) + (left.M22 * right.M21),
                (left.M21 * right.M12) + (left.M22 * right.M22),
                (left.offsetX * right.M11) + (left.offsetY * right.M21) + right.offsetX,
                (left.offsetX * right.M12) + (left.offsetY * right.M22) + right.offsetY);
        }

        public static bool Equals(Matrix matrix1, Matrix matrix2)
        {
            return matrix1.Equals(matrix2);
        }

        public static Matrix Rotation(double angle)
        {
            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);
            return new Matrix(cos, sin, -sin, cos, 0, 0);
        }

        public static Matrix Translation(Vector v)
        {
            return Translation(v.X, v.Y);
        }

        public static Matrix Translation(double x, double y)
        {
            return new Matrix(1.0, 0.0, 0.0, 1.0, x, y);
        }

        public static double ToRadians(double angle)
        {
            return angle * 0.0174532925;
        }

        public static Matrix operator -(Matrix matrix)
        {
            return matrix.Invert();
        }

        public static bool operator ==(Matrix matrix1, Matrix matrix2)
        {
            return matrix1.Equals(matrix2);
        }

        public static bool operator !=(Matrix matrix1, Matrix matrix2)
        {
            return !matrix1.Equals(matrix2);
        }

        public bool Equals(Matrix value)
        {
            return this.m11 == value.M11 &&
                   this.m12 == value.M12 &&
                   this.m21 == value.M21 &&
                   this.m22 == value.M22 &&
                   this.offsetX == value.OffsetX &&
                   this.offsetY == value.OffsetY;
        }

        public override bool Equals(object o)
        {
            if (!(o is Matrix))
            {
                return false;
            }

            return this.Equals((Matrix)o);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

        public Matrix Invert()
        {
            if (!this.HasInverse)
            {
                throw new InvalidOperationException("Transform is not invertible.");
            }

            double d = this.Determinant;

            return new Matrix(
                this.m22 / d,
                -this.m12 / d,
                -this.m21 / d,
                this.m11 / d,
                ((this.m21 * this.offsetY) - (this.m22 * this.offsetX)) / d,
                ((this.m12 * this.offsetX) - (this.m11 * this.offsetY)) / d);
        }
    }
}