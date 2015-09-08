





namespace Perspex.Direct2D1
{
    using System;
    using System.Linq;
    using SharpDX;
    using SharpDX.Direct2D1;
    using DWrite = SharpDX.DirectWrite;

    public static class PrimitiveExtensions
    {
        public static Rect ToPerspex(this RectangleF r)
        {
            return new Rect(r.X, r.Y, r.Width, r.Height);
        }

        public static RectangleF ToSharpDX(this Rect r)
        {
            return new RectangleF((float)r.X, (float)r.Y, (float)r.Width, (float)r.Height);
        }

        public static Vector2 ToSharpDX(this Perspex.Point p)
        {
            return new Vector2((float)p.X, (float)p.Y);
        }

        public static Size2F ToSharpDX(this Perspex.Size p)
        {
            return new Size2F((float)p.Width, (float)p.Height);
        }

        public static SharpDX.Direct2D1.ExtendMode ToDirect2D(this Perspex.Media.GradientSpreadMethod spreadMethod)
        {
            if (spreadMethod == Perspex.Media.GradientSpreadMethod.Pad)
                return ExtendMode.Clamp;
            else if (spreadMethod == Perspex.Media.GradientSpreadMethod.Reflect)
                return ExtendMode.Mirror;
            else
                return ExtendMode.Wrap;
        }
        /// <summary>
        /// Converts a pen to a Direct2D stroke style.
        /// </summary>
        /// <param name="pen">The pen to convert.</param>
        /// <param name="target">The render target.</param>
        /// <returns>The Direct2D brush.</returns>
        public static StrokeStyle ToDirect2DStrokeStyle(this Perspex.Media.Pen pen, RenderTarget target)
        {
            if (pen.DashArray != null && pen.DashArray.Count > 0)
            {
                var properties = new StrokeStyleProperties
                {
                    DashStyle = DashStyle.Custom,
                };

                return new StrokeStyle(target.Factory, properties, pen.DashArray.Select(x => (float)x).ToArray());
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Converts a Perspex <see cref="Perspex.Media.Color"/> to Direct2D.
        /// </summary>
        /// <param name="color">The color to convert.</param>
        /// <returns>The Direct2D color.</returns>
        public static Color4 ToDirect2D(this Perspex.Media.Color color)
        {
            return new Color4(
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
        public static Matrix3x2 ToDirect2D(this Perspex.Matrix matrix)
        {
            return new Matrix3x2(
                (float)matrix.M11,
                (float)matrix.M12,
                (float)matrix.M21,
                (float)matrix.M22,
                (float)matrix.M31,
                (float)matrix.M32);
        }

        /// <summary>
        /// Converts a Direct2D <see cref="Matrix3x2"/> to a Perspex <see cref="Perspex.Matrix"/>.
        /// </summary>
        /// <param name="matrix">The matrix</param>
        /// <returns>a <see cref="Perspex.Matrix"/>.</returns>
        public static Perspex.Matrix ToPerspex(this Matrix3x2 matrix)
        {
            return new Perspex.Matrix(
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
        public static RectangleF ToDirect2D(this Rect rect)
        {
            return new RectangleF(
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
    }
}
