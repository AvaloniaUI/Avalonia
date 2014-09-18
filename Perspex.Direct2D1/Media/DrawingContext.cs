// -----------------------------------------------------------------------
// <copyright file="DrawingContext.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Direct2D1.Media
{
    using System;
    using System.Reactive.Disposables;
    using Perspex.Direct2D1.Media;
    using Perspex.Media;
    using SharpDX;
    using SharpDX.Direct2D1;
    using IBitmap = Perspex.Media.Imaging.IBitmap;
    using Matrix = Perspex.Matrix;

    /// <summary>
    /// Draws using Direct2D1.
    /// </summary>
    public class DrawingContext : IDrawingContext, IDisposable
    {
        /// <summary>
        /// The Direct2D1 render target.
        /// </summary>
        private RenderTarget renderTarget;

        /// <summary>
        /// The DirectWrite factory.
        /// </summary>
        private SharpDX.DirectWrite.Factory directWriteFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="DrawingContext"/> class.
        /// </summary>
        /// <param name="renderTarget">The render target to draw to.</param>
        /// <param name="directWriteFactory">The DirectWrite factory.</param>
        public DrawingContext(
            RenderTarget renderTarget,
            SharpDX.DirectWrite.Factory directWriteFactory)
        {
            this.renderTarget = renderTarget;
            this.directWriteFactory = directWriteFactory;
            this.renderTarget.BeginDraw();
        }

        public Matrix CurrentTransform
        {
            get { return Convert(this.renderTarget.Transform); }
            set { this.renderTarget.Transform = Convert(value); }
        }

        /// <summary>
        /// Ends a draw operation.
        /// </summary>
        public void Dispose()
        {
            this.renderTarget.EndDraw();
        }

        public void DrawImage(IBitmap bitmap, double opacity, Rect sourceRect, Rect destRect)
        {
            BitmapImpl impl = (BitmapImpl)bitmap.PlatformImpl;
            Bitmap d2d = impl.GetDirect2DBitmap(this.renderTarget);
            this.renderTarget.DrawBitmap(
                d2d,
                destRect.ToSharpDX(),
                (float)opacity,
                BitmapInterpolationMode.Linear,
                sourceRect.ToSharpDX());
        }

        /// <summary>
        /// Draws a line.
        /// </summary>
        /// <param name="pen">The stroke pen.</param>
        /// <param name="p1">The first point of the line.</param>
        /// <param name="p1">The second point of the line.</param>
        public void DrawLine(Pen pen, Perspex.Point p1, Perspex.Point p2)
        {
            if (pen != null)
            {
                using (SharpDX.Direct2D1.SolidColorBrush d2dBrush = this.Convert(pen.Brush))
                {
                    this.renderTarget.DrawLine(p1.ToSharpDX(), p2.ToSharpDX(), d2dBrush);
                }
            }
        }

        /// <summary>
        /// Draws a geometry.
        /// </summary>
        /// <param name="brush">The fill brush.</param>
        /// <param name="pen">The stroke pen.</param>
        /// <param name="geometry">The geometry.</param>
        public void DrawGeometry(Perspex.Media.Brush brush, Perspex.Media.Pen pen, Perspex.Media.Geometry geometry)
        {
            if (brush != null)
            {
                using (SharpDX.Direct2D1.SolidColorBrush d2dBrush = this.Convert(brush))
                {
                    GeometryImpl impl = (GeometryImpl)geometry.PlatformImpl;
                    this.renderTarget.FillGeometry(impl.Geometry, d2dBrush);
                }
            }

            if (pen != null)
            {
                using (SharpDX.Direct2D1.SolidColorBrush d2dBrush = this.Convert(pen.Brush))
                {
                    GeometryImpl impl = (GeometryImpl)geometry.PlatformImpl;
                    this.renderTarget.DrawGeometry(impl.Geometry, d2dBrush, (float)pen.Thickness);
                }
            }
        }

        /// <summary>
        /// Draws the outline of a rectangle.
        /// </summary>
        /// <param name="pen">The pen.</param>
        /// <param name="rect">The rectangle bounds.</param>
        public void DrawRectange(Pen pen, Rect rect)
        {
            using (SharpDX.Direct2D1.SolidColorBrush brush = this.Convert(pen.Brush))
            {
                this.renderTarget.DrawRectangle(
                    this.Convert(rect),
                    brush,
                    (float)pen.Thickness);
            }
        }

        /// <summary>
        /// Draws text.
        /// </summary>
        /// <param name="foreground">The foreground brush.</param>
        /// <param name="rect">The output rectangle.</param>
        /// <param name="text">The text.</param>
        public void DrawText(Perspex.Media.Brush foreground, Rect rect, FormattedText text)
        {
            if (!string.IsNullOrEmpty(text.Text))
            {
                using (SharpDX.Direct2D1.SolidColorBrush brush = this.Convert(foreground))
                using (SharpDX.DirectWrite.TextFormat format = TextService.GetTextFormat(this.directWriteFactory, text))
                {
                    this.renderTarget.DrawText(
                        text.Text,
                        format,
                        this.Convert(rect),
                        brush);
                }
            }
        }

        /// <summary>
        /// Draws a filled rectangle.
        /// </summary>
        /// <param name="brush">The brush.</param>
        /// <param name="rect">The rectangle bounds.</param>
        public void FillRectange(Perspex.Media.Brush brush, Rect rect)
        {
            using (SharpDX.Direct2D1.SolidColorBrush b = this.Convert(brush))
            {
                this.renderTarget.FillRectangle(
                    new RectangleF(
                        (float)rect.X,
                        (float)rect.Y,
                        (float)rect.Width,
                        (float)rect.Height),
                    b);
            }
        }

        /// <summary>
        /// Pushes a matrix transformation.
        /// </summary>
        /// <param name="matrix">The matrix</param>
        /// <returns>A disposable used to undo the transformation.</returns>
        public IDisposable PushTransform(Matrix matrix)
        {
            Matrix3x2 m3x2 = this.Convert(matrix);
            Matrix3x2 transform = this.renderTarget.Transform * m3x2;
            this.renderTarget.Transform = transform;

            return Disposable.Create(() =>
            {
                m3x2.Invert();
                this.renderTarget.Transform = transform * m3x2;
            });
        }

        /// <summary>
        /// Converts a brush to Direct2D.
        /// </summary>
        /// <param name="brush">The brush to convert.</param>
        /// <returns>The Direct2D brush.</returns>
        private SharpDX.Direct2D1.SolidColorBrush Convert(Perspex.Media.Brush brush)
        {
            Perspex.Media.SolidColorBrush solidColorBrush = brush as Perspex.Media.SolidColorBrush;

            if (solidColorBrush != null)
            {
                return new SharpDX.Direct2D1.SolidColorBrush(
                    this.renderTarget, 
                    this.Convert(solidColorBrush.Color));
            }
            else
            {
                return new SharpDX.Direct2D1.SolidColorBrush(
                    this.renderTarget,
                    new Color4());
            }
        }

        /// <summary>
        /// Converts a color to Direct2D.
        /// </summary>
        /// <param name="color">The color to convert.</param>
        /// <returns>The Direct2D color.</returns>
        private Color4 Convert(Perspex.Media.Color color)
        {
            return new Color4(
                (float)(color.R / 255.0),
                (float)(color.G / 255.0),
                (float)(color.B / 255.0),
                (float)(color.A / 255.0));
        }

        /// <summary>
        /// Converts a <see cref="Matrix"/> to a Direct2D <see cref="Matrix3x2"/>
        /// </summary>
        /// <param name="matrix">The <see cref="Matrix"/>.</param>
        /// <returns>The <see cref="Matrix3x2"/>.</returns>
        private Matrix3x2 Convert(Matrix matrix)
        {
            return new Matrix3x2(
                (float)matrix.M11,
                (float)matrix.M12,
                (float)matrix.M21,
                (float)matrix.M22,
                (float)matrix.OffsetX,
                (float)matrix.OffsetY);
        }

        private Matrix Convert(Matrix3x2 matrix)
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
        /// Converts a <see cref="Rect"/> to a <see cref="RectangleF"/>
        /// </summary>
        /// <param name="rect">The <see cref="Rect"/>.</param>
        /// <returns>The <see cref="RectangleF"/>.</returns>
        private RectangleF Convert(Rect rect)
        {
            return new RectangleF(
                (float)rect.X,
                (float)rect.Y,
                (float)rect.Width,
                (float)rect.Height);
        }
    }
}
