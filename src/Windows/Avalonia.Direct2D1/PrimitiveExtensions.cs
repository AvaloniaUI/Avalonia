// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using DWrite = SharpDX.DirectWrite;

namespace Avalonia.Direct2D1
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

        public static Rect ToAvalonia(this RawRectangleF r)
        {
            return new Rect(new Point(r.Left, r.Top), new Point(r.Right, r.Bottom));
        }

        public static PixelSize ToAvalonia(this Size2 p) => new PixelSize(p.Width, p.Height);

        public static Vector ToAvaloniaVector(this Size2F p) => new Vector(p.Width, p.Height);

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

        public static ExtendMode ToDirect2D(this Avalonia.Media.GradientSpreadMethod spreadMethod)
        {
            if (spreadMethod == Avalonia.Media.GradientSpreadMethod.Pad)
                return ExtendMode.Clamp;
            else if (spreadMethod == Avalonia.Media.GradientSpreadMethod.Reflect)
                return ExtendMode.Mirror;
            else
                return ExtendMode.Wrap;
        }

        public static SharpDX.Direct2D1.LineJoin ToDirect2D(this Avalonia.Media.PenLineJoin lineJoin)
        {
            if (lineJoin == Avalonia.Media.PenLineJoin.Round)
                return LineJoin.Round;
            else if (lineJoin == Avalonia.Media.PenLineJoin.Miter)
                return LineJoin.Miter;
            else
                return LineJoin.Bevel;
        }
        
        public static SharpDX.Direct2D1.CapStyle ToDirect2D(this Avalonia.Media.PenLineCap lineCap)
        {
            if (lineCap == Avalonia.Media.PenLineCap.Flat)
                return CapStyle.Flat;
            else if (lineCap == Avalonia.Media.PenLineCap.Round)
                return CapStyle.Round;
            else if (lineCap == Avalonia.Media.PenLineCap.Square)
                return CapStyle.Square;
            else
                return CapStyle.Triangle;
        }

        public static Guid ToWic(this Platform.PixelFormat format)
        {
            if (format == Platform.PixelFormat.Rgb565)
                return SharpDX.WIC.PixelFormat.Format16bppBGR565;
            if (format == Platform.PixelFormat.Bgra8888)
                return SharpDX.WIC.PixelFormat.Format32bppPBGRA;
            if (format == Platform.PixelFormat.Rgba8888)
                return SharpDX.WIC.PixelFormat.Format32bppPRGBA;
            throw new ArgumentException("Unknown pixel format");
        }

        /// <summary>
        /// Converts a pen to a Direct2D stroke style.
        /// </summary>
        /// <param name="pen">The pen to convert.</param>
        /// <param name="renderTarget">The render target.</param>
        /// <returns>The Direct2D brush.</returns>
        public static StrokeStyle ToDirect2DStrokeStyle(this Avalonia.Media.IPen pen, SharpDX.Direct2D1.RenderTarget renderTarget)
        {
            return pen.ToDirect2DStrokeStyle(renderTarget.Factory);
        }

        /// <summary>
        /// Converts a pen to a Direct2D stroke style.
        /// </summary>
        /// <param name="pen">The pen to convert.</param>
        /// <param name="factory">The factory associated with this resource.</param>
        /// <returns>The Direct2D brush.</returns>
        public static StrokeStyle ToDirect2DStrokeStyle(this Avalonia.Media.IPen pen, Factory factory)
        {
            var d2dLineCap = pen.LineCap.ToDirect2D();

            var properties = new StrokeStyleProperties
            {
                DashStyle = DashStyle.Solid,
                MiterLimit = (float)pen.MiterLimit,
                LineJoin = pen.LineJoin.ToDirect2D(),
                StartCap = d2dLineCap,
                EndCap = d2dLineCap,
                DashCap = d2dLineCap
            };
            float[] dashes = null;
            if (pen.DashStyle?.Dashes != null && pen.DashStyle.Dashes.Count > 0)
            {
                properties.DashStyle = DashStyle.Custom;
                properties.DashOffset = (float)pen.DashStyle.Offset;
                dashes = pen.DashStyle.Dashes.Select(x => (float)x).ToArray();
            }

            dashes = dashes ?? Array.Empty<float>();

            return new StrokeStyle(factory, properties, dashes);
        }

        /// <summary>
        /// Converts a Avalonia <see cref="Avalonia.Media.Color"/> to Direct2D.
        /// </summary>
        /// <param name="color">The color to convert.</param>
        /// <returns>The Direct2D color.</returns>
        public static RawColor4 ToDirect2D(this Avalonia.Media.Color color)
        {
            return new RawColor4(
                (float)(color.R / 255.0),
                (float)(color.G / 255.0),
                (float)(color.B / 255.0),
                (float)(color.A / 255.0));
        }

        /// <summary>
        /// Converts a Avalonia <see cref="Avalonia.Matrix"/> to a Direct2D <see cref="RawMatrix3x2"/>
        /// </summary>
        /// <param name="matrix">The <see cref="Matrix"/>.</param>
        /// <returns>The <see cref="RawMatrix3x2"/>.</returns>
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
        /// Converts a Direct2D <see cref="RawMatrix3x2"/> to a Avalonia <see cref="Avalonia.Matrix"/>.
        /// </summary>
        /// <param name="matrix">The matrix</param>
        /// <returns>a <see cref="Avalonia.Matrix"/>.</returns>
        public static Matrix ToAvalonia(this RawMatrix3x2 matrix)
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
        /// Converts a Avalonia <see cref="Rect"/> to a Direct2D <see cref="RawRectangleF"/>
        /// </summary>
        /// <param name="rect">The <see cref="Rect"/>.</param>
        /// <returns>The <see cref="RawRectangleF"/>.</returns>
        public static RawRectangleF ToDirect2D(this Rect rect)
        {
            return new RawRectangleF(
                (float)rect.X,
                (float)rect.Y,
                (float)rect.Right,
                (float)rect.Bottom);
        }

        public static DWrite.TextAlignment ToDirect2D(this Avalonia.Media.TextAlignment alignment)
        {
            switch (alignment)
            {
                case Avalonia.Media.TextAlignment.Left:
                    return DWrite.TextAlignment.Leading;
                case Avalonia.Media.TextAlignment.Center:
                    return DWrite.TextAlignment.Center;
                case Avalonia.Media.TextAlignment.Right:
                    return DWrite.TextAlignment.Trailing;
                default:
                    throw new InvalidOperationException("Invalid TextAlignment");
            }
        }
    }
}
