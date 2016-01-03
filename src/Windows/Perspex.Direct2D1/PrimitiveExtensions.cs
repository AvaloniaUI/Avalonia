// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using SharpDX;
using SharpDX.Direct2D1;
using DWrite = SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;

namespace Perspex.Direct2D1
{
    public static class PrimitiveExtensions
    {
        /// <summary>
        /// The value for which all absolute numbers smaller than are considered equal to zero.
        /// </summary>
        public const float ZeroTolerance = 1e-6f; // Value a 8x higher than 1.19209290E-07F

        public static readonly RawRectangleF RectangleInfinite;

        /// <summary>
        /// Gets the identity matrix.
        /// </summary>
        /// <value>The identity matrix.</value>
        public readonly static RawMatrix3x2 Matrix3x2Identity = new RawMatrix3x2 { M11 = 1, M12 = 0, M21 = 0, M22 = 1, M31 = 0, M32 = 0 };

        static PrimitiveExtensions()
        {
            RectangleInfinite = new RawRectangleF
            {
                Left = float.NegativeInfinity,
                Top = float.NegativeInfinity,
                Right = float.PositiveInfinity,
                Bottom = float.PositiveInfinity
            };
        }

        public static Rect ToPerspex(this RawRectangleF r)
        {
            return new Rect(new Point(r.Left, r.Top), new Point(r.Right, r.Bottom));
        }

        public static RawRectangleF ToSharpDX(this Rect r)
        {
            return new RawRectangleF((float)r.X, (float)r.Y, (float)r.Right, (float)r.Bottom);
        }

        public static RawVector2 ToSharpDX(this Point p)
        {
            return new RawVector2 { X = (float)p.X, Y = (float)p.Y };
        }

        public static Size2F ToSharpDX(this Size p)
        {
            return new Size2F((float)p.Width, (float)p.Height);
        }

        public static ExtendMode ToDirect2D(this Perspex.Media.GradientSpreadMethod spreadMethod)
        {
            if (spreadMethod == Perspex.Media.GradientSpreadMethod.Pad)
                return ExtendMode.Clamp;
            else if (spreadMethod == Perspex.Media.GradientSpreadMethod.Reflect)
                return ExtendMode.Mirror;
            else
                return ExtendMode.Wrap;
        }

        public static SharpDX.Direct2D1.LineJoin ToDirect2D(this Perspex.Media.PenLineJoin lineJoin)
        {
            if (lineJoin == Perspex.Media.PenLineJoin.Round)
                return LineJoin.Round;
            else if (lineJoin == Perspex.Media.PenLineJoin.Miter)
                return LineJoin.Miter;
            else
                return LineJoin.Bevel;
        }

        public static SharpDX.Direct2D1.CapStyle ToDirect2D(this Perspex.Media.PenLineCap lineCap)
        {
            if (lineCap == Perspex.Media.PenLineCap.Flat)
                return CapStyle.Flat;
            else if (lineCap == Perspex.Media.PenLineCap.Round)
                return CapStyle.Round;
            else if (lineCap == Perspex.Media.PenLineCap.Square)
                return CapStyle.Square;
            else
                return CapStyle.Triangle;
        }

        /// <summary>
        /// Converts a pen to a Direct2D stroke style.
        /// </summary>
        /// <param name="pen">The pen to convert.</param>
        /// <param name="target">The render target.</param>
        /// <returns>The Direct2D brush.</returns>
        public static StrokeStyle ToDirect2DStrokeStyle(this Perspex.Media.Pen pen, RenderTarget target)
        {
            if (pen.DashStyle != null)
            {
                if (pen.DashStyle.Dashes != null && pen.DashStyle.Dashes.Count > 0)
                {
                    var properties = new StrokeStyleProperties
                    {
                        DashStyle = DashStyle.Custom,
                        DashOffset = (float)pen.DashStyle.Offset,
                        MiterLimit = (float)pen.MiterLimit,
                        LineJoin = pen.LineJoin.ToDirect2D(),
                        StartCap = pen.StartLineCap.ToDirect2D(),
                        EndCap = pen.EndLineCap.ToDirect2D(),
                        DashCap = pen.DashCap.ToDirect2D()
                    };

                    return new StrokeStyle(target.Factory, properties, pen.DashStyle?.Dashes.Select(x => (float)x).ToArray());
                }
            }

            return null;
        }

        /// <summary>
        /// Converts a Perspex <see cref="Perspex.Media.Color"/> to Direct2D.
        /// </summary>
        /// <param name="color">The color to convert.</param>
        /// <returns>The Direct2D color.</returns>
        public static RawColor4 ToDirect2D(this Perspex.Media.Color color)
        {
            return new RawColor4(
                (float)(color.R / 255.0),
                (float)(color.G / 255.0),
                (float)(color.B / 255.0),
                (float)(color.A / 255.0));
        }

        /// <summary>
        /// Converts a Perspex <see cref="Perspex.Matrix"/> to a Direct2D <see cref="Matrix3x2"/>
        /// </summary>
        /// <param name="matrix">The <see cref="Matrix"/>.</param>
        /// <returns>The <see cref="Matrix3x2"/>.</returns>
        public static RawMatrix3x2 ToDirect2D(this Matrix matrix)
        {
            return new RawMatrix3x2
            {
                M11 = (float)matrix.M11,
                M12 = (float)matrix.M12,
                M21 = (float)matrix.M21,
                M22 = (float)matrix.M22,
                M31 = (float)matrix.M31,
                M32 = (float)matrix.M32
            };
        }

        /// <summary>
        /// Converts a Direct2D <see cref="Matrix3x2"/> to a Perspex <see cref="Perspex.Matrix"/>.
        /// </summary>
        /// <param name="matrix">The matrix</param>
        /// <returns>a <see cref="Perspex.Matrix"/>.</returns>
        public static Matrix ToPerspex(this RawMatrix3x2 matrix)
        {
            return new Matrix(
                matrix.M11,
                matrix.M12,
                matrix.M21,
                matrix.M22,
                matrix.M31,
                matrix.M32);
        }

        /// <summary>
        /// Converts a Perspex <see cref="Rect"/> to a Direct2D <see cref="RectangleF"/>
        /// </summary>
        /// <param name="rect">The <see cref="Rect"/>.</param>
        /// <returns>The <see cref="RectangleF"/>.</returns>
        public static RawRectangleF ToDirect2D(this Rect rect)
        {
            return new RawRectangleF(
                (float)rect.X,
                (float)rect.Y,
                (float)rect.Width,
                (float)rect.Height);
        }

        public static DWrite.TextAlignment ToDirect2D(this Perspex.Media.TextAlignment alignment)
        {
            switch (alignment)
            {
                case Perspex.Media.TextAlignment.Left:
                    return DWrite.TextAlignment.Leading;
                case Perspex.Media.TextAlignment.Center:
                    return DWrite.TextAlignment.Center;
                case Perspex.Media.TextAlignment.Right:
                    return DWrite.TextAlignment.Trailing;
                default:
                    throw new InvalidOperationException("Invalid TextAlignment");
            }
        }

        /// <summary>
        /// Determines whether the specified value is close to zero (0.0f).
        /// </summary>
        /// <param name="a">The floating value.</param>
        /// <returns><c>true</c> if the specified value is close to zero (0.0f); otherwise, <c>false</c>.</returns>
        public static bool IsZero(float a)
        {
            return Math.Abs(a) < ZeroTolerance;
        }

        /// <summary>
        /// Determines the product of two matrices.
        /// </summary>
        /// <param name="left">The first matrix to multiply.</param>
        /// <param name="right">The second matrix to multiply.</param>
        /// <param name="result">The product of the two matrices.</param>
        public static void Multiply(ref RawMatrix3x2 left, ref RawMatrix3x2 right, out RawMatrix3x2 result)
        {
            result = new RawMatrix3x2();
            result.M11 = (left.M11 * right.M11) + (left.M12 * right.M21);
            result.M12 = (left.M11 * right.M12) + (left.M12 * right.M22);
            result.M21 = (left.M21 * right.M11) + (left.M22 * right.M21);
            result.M22 = (left.M21 * right.M12) + (left.M22 * right.M22);
            result.M31 = (left.M31 * right.M11) + (left.M32 * right.M21) + right.M31;
            result.M32 = (left.M31 * right.M12) + (left.M32 * right.M22) + right.M32;
        }

        /// <summary>
        /// Determines the product of two matrices.
        /// </summary>
        /// <param name="left">The first matrix to multiply.</param>
        /// <param name="right">The second matrix to multiply.</param>
        /// <returns>The product of the two matrices.</returns>
        public static RawMatrix3x2 Multiply(RawMatrix3x2 left, RawMatrix3x2 right)
        {
            RawMatrix3x2 result;
            Multiply(ref left, ref right, out result);
            return result;
        }

        /// <summary>
        /// Calculates the inverse of the specified matrix.
        /// </summary>
        /// <param name="value">The matrix whose inverse is to be calculated.</param>
        /// <returns>the inverse of the specified matrix.</returns>
        public static RawMatrix3x2 Invert(RawMatrix3x2 value)
        {
            RawMatrix3x2 result;
            Invert(ref value, out result);
            return result;
        }


        /// <summary>
        /// Calculates the inverse of the specified matrix.
        /// </summary>
        /// <param name="value">The matrix whose inverse is to be calculated.</param>
        /// <param name="result">When the method completes, contains the inverse of the specified matrix.</param>
        public static void Invert(ref RawMatrix3x2 value, out RawMatrix3x2 result)
        {
            float determinant = (value.M11 * value.M22) - (value.M12 * value.M21);

            if (IsZero(determinant))
            {
                result = Matrix3x2Identity;
                return;
            }

            float invdet = 1.0f / determinant;
            float _offsetX = value.M31;
            float _offsetY = value.M32;

            result = new RawMatrix3x2
            {
                M11 = value.M22 * invdet,
                M12 = -value.M12 * invdet,
                M21 = -value.M21 * invdet,
                M22 = value.M11 * invdet,
                M31 = (value.M21 * _offsetY - _offsetX * value.M22) * invdet,
                M32 = (_offsetX * value.M12 - value.M11 * _offsetY) * invdet
            };
        }
    }
}
