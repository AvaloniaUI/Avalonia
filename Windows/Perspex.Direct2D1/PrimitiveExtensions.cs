// -----------------------------------------------------------------------
// <copyright file="PrimitiveExtensions.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Direct2D1
{
    using SharpDX;
    using SharpDX.Direct2D1;

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

        /// <summary>
        /// Converts a brush to Direct2D.
        /// </summary>
        /// <param name="brush">The brush to convert.</param>
        /// <returns>The Direct2D brush.</returns>
        public static SharpDX.Direct2D1.Brush ToDirect2D(this Perspex.Media.Brush brush, RenderTarget target)
        {
            Perspex.Media.SolidColorBrush solidColorBrush = brush as Perspex.Media.SolidColorBrush;

            if (solidColorBrush != null)
            {
                return new SharpDX.Direct2D1.SolidColorBrush(target, solidColorBrush.Color.ToDirect2D());
            }
            else
            {
                // TODO: Implement other brushes.
                return new SharpDX.Direct2D1.SolidColorBrush(target, new Color4());
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
                (float)matrix.OffsetX,
                (float)matrix.OffsetY);
        }

        /// <summary>
        /// Converts a Direct2D <see cref="Matrix3x2"/> to a Perspex <see cref="Perspex.Matrix"/>.
        /// </summary>
        /// <param name="matrix"></param>
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
    }
}
