// -----------------------------------------------------------------------
// <copyright file="Matrix.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media
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

        public static bool Equals(Matrix matrix1, Matrix matrix2)
        {
            return matrix1.Equals(matrix2);
        }

        public static Matrix Translation(double x, double y)
        {
            return new Matrix(1.0, 0.0, 0.0, 1.0, x, y);
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
    }
}