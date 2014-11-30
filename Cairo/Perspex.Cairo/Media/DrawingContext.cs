// -----------------------------------------------------------------------
// <copyright file="DrawingContext.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Cairo.Media
{
    using System;
    using System.Reactive.Disposables;
    using global::Cairo;
    using Perspex.Media;
    using IBitmap = Perspex.Media.Imaging.IBitmap;
    using Matrix = Perspex.Matrix;

    /// <summary>
    /// Draws using Direct2D1.
    /// </summary>
    public class DrawingContext : IDrawingContext, IDisposable
    {
        /// <summary>
        /// The cairo context.
        /// </summary>
        private Context context;

        /// <summary>
        /// Initializes a new instance of the <see cref="DrawingContext"/> class.
        /// </summary>
        /// <param name="surface">The target surface.</param>
        public DrawingContext(Surface surface)
        {
            this.context = new Context(surface);
        }

        public Matrix CurrentTransform
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Ends a draw operation.
        /// </summary>
        public void Dispose()
        {
            this.context.Dispose();
        }

        public void DrawImage(IBitmap bitmap, double opacity, Rect sourceRect, Rect destRect)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Draws a line.
        /// </summary>
        /// <param name="pen">The stroke pen.</param>
        /// <param name="p1">The first point of the line.</param>
        /// <param name="p1">The second point of the line.</param>
        public void DrawLine(Pen pen, Perspex.Point p1, Perspex.Point p2)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Draws a geometry.
        /// </summary>
        /// <param name="brush">The fill brush.</param>
        /// <param name="pen">The stroke pen.</param>
        /// <param name="geometry">The geometry.</param>
        public void DrawGeometry(Perspex.Media.Brush brush, Perspex.Media.Pen pen, Perspex.Media.Geometry geometry)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Draws the outline of a rectangle.
        /// </summary>
        /// <param name="pen">The pen.</param>
        /// <param name="rect">The rectangle bounds.</param>
        public void DrawRectange(Pen pen, Rect rect)
        {
            this.SetPen(pen);
            this.context.Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
            this.context.Stroke();
        }

        /// <summary>
        /// Draws text.
        /// </summary>
        /// <param name="foreground">The foreground brush.</param>
        /// <param name="rect">The output rectangle.</param>
        /// <param name="text">The text.</param>
        public void DrawText(Perspex.Media.Brush foreground, Rect rect, FormattedText text)
        {
            // TODO: Implement
        }

        /// <summary>
        /// Draws a filled rectangle.
        /// </summary>
        /// <param name="brush">The brush.</param>
        /// <param name="rect">The rectangle bounds.</param>
        public void FillRectange(Perspex.Media.Brush brush, Rect rect)
        {
            this.SetBrush(brush);
            this.context.Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
            this.context.Fill();
        }

        /// <summary>
        /// Pushes a clip rectange.
        /// </summary>
        /// <param name="clip">The clip rectangle.</param>
        /// <returns>A disposable used to undo the clip rectangle.</returns>
        public IDisposable PushClip(Rect clip)
        {
            return Disposable.Empty;
        }

        /// <summary>
        /// Pushes a matrix transformation.
        /// </summary>
        /// <param name="matrix">The matrix</param>
        /// <returns>A disposable used to undo the transformation.</returns>
        public IDisposable PushTransform(Matrix matrix)
        {
            return Disposable.Empty;
        }

        private void SetBrush(Brush brush)
        {
            var solid = brush as SolidColorBrush;

            if (solid != null)
            {
                this.context.SetSourceRGBA(
                    solid.Color.R / 255.0,
                    solid.Color.G / 255.0,
                    solid.Color.B / 255.0,
                    solid.Color.A / 255.0);
            }
        }

        private void SetPen(Pen pen)
        {
            this.SetBrush(pen.Brush);
            this.context.LineWidth = pen.Thickness;
        }
    }
}
