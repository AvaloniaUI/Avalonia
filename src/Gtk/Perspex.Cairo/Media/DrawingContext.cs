// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using Perspex.Cairo.Media.Imaging;
using Perspex.Media;
using IBitmap = Perspex.Media.Imaging.IBitmap;

namespace Perspex.Cairo.Media
{
    using Cairo = global::Cairo;

    /// <summary>
    /// Draws using Direct2D1.
    /// </summary>
    public class DrawingContext : IDrawingContext, IDisposable
    {
        /// <summary>
        /// The cairo context.
        /// </summary>
        private Cairo.Context _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="DrawingContext"/> class.
        /// </summary>
        /// <param name="surface">The target surface.</param>
        public DrawingContext(Cairo.Surface surface)
        {
            _context = new Cairo.Context(surface);
            this.CurrentTransform = Matrix.Identity;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DrawingContext"/> class.
        /// </summary>
        /// <param name="surface">The GDK drawable.</param>
        public DrawingContext(Gdk.Drawable drawable)
        {
            _context = Gdk.CairoHelper.Create(drawable);
            this.CurrentTransform = Matrix.Identity;
        }

        public Matrix CurrentTransform
        {
            get;
            private set;
        }

        /// <summary>
        /// Ends a draw operation.
        /// </summary>
        public void Dispose()
        {
            _context.Dispose();
        }

        public void DrawImage(IBitmap bitmap, double opacity, Rect sourceRect, Rect destRect)
        {
            var impl = bitmap.PlatformImpl as BitmapImpl;
            var size = new Size(impl.PixelWidth, impl.PixelHeight);
            var scaleX = destRect.Size.Width / sourceRect.Size.Width;
            var scaleY = destRect.Size.Height / sourceRect.Size.Height;

            _context.Save();
            _context.Scale(scaleX, scaleY);
            _context.SetSourceSurface(impl.Surface, (int)sourceRect.X, (int)sourceRect.Y);
            _context.Rectangle(sourceRect.ToCairo());
            _context.Fill();
            _context.Restore();
        }

        /// <summary>
        /// Draws a line.
        /// </summary>
        /// <param name="pen">The stroke pen.</param>
        /// <param name="p1">The first point of the line.</param>
        /// <param name="p1">The second point of the line.</param>
        public void DrawLine(Pen pen, Perspex.Point p1, Perspex.Point p2)
        {
            var size = new Rect(p1, p2).Size;

            this.SetBrush(pen.Brush, size);
            _context.LineWidth = pen.Thickness;
            _context.MoveTo(p1.ToCairo());
            _context.LineTo(p2.ToCairo());
            _context.Stroke();
        }

        /// <summary>
        /// Draws a geometry.
        /// </summary>
        /// <param name="brush">The fill brush.</param>
        /// <param name="pen">The stroke pen.</param>
        /// <param name="geometry">The geometry.</param>
        public void DrawGeometry(Perspex.Media.Brush brush, Perspex.Media.Pen pen, Perspex.Media.Geometry geometry)
        {
            var impl = geometry.PlatformImpl as StreamGeometryImpl;

            using (var pop = this.PushTransform(impl.Transform))
            {
                _context.AppendPath(impl.Path);

                if (brush != null)
                {
                    this.SetBrush(brush, geometry.Bounds.Size);

                    if (pen != null)
                        _context.FillPreserve();
                    else
                        _context.Fill();
                }


                if (pen != null)
                {
                    this.SetPen(pen, geometry.Bounds.Size);
                    _context.Stroke();
                }
            }
        }

        /// <summary>
        /// Draws the outline of a rectangle.
        /// </summary>
        /// <param name="pen">The pen.</param>
        /// <param name="rect">The rectangle bounds.</param>
        public void DrawRectange(Pen pen, Rect rect, float cornerRadius)
        {
            this.SetPen(pen, rect.Size);
            _context.Rectangle(rect.ToCairo());
            _context.Stroke();
        }

        /// <summary>
        /// Draws text.
        /// </summary>
        /// <param name="foreground">The foreground brush.</param>
        /// <param name="origin">The upper-left corner of the text.</param>
        /// <param name="text">The text.</param>
        public void DrawText(Brush foreground, Point origin, FormattedText text)
        {
            var layout = ((FormattedTextImpl)text.PlatformImpl).Layout;
            this.SetBrush(foreground, new Size(0, 0));

            _context.MoveTo(origin.X, origin.Y);
            Pango.CairoHelper.ShowLayout(_context, layout);
        }

        /// <summary>
        /// Draws a filled rectangle.
        /// </summary>
        /// <param name="brush">The brush.</param>
        /// <param name="rect">The rectangle bounds.</param>
        public void FillRectange(Perspex.Media.Brush brush, Rect rect, float cornerRadius)
        {
            this.SetBrush(brush, rect.Size);
            _context.Rectangle(rect.ToCairo());
            _context.Fill();
        }

        /// <summary>
        /// Pushes a clip rectange.
        /// </summary>
        /// <param name="clip">The clip rectangle.</param>
        /// <returns>A disposable used to undo the clip rectangle.</returns>
        public IDisposable PushClip(Rect clip)
        {
            _context.Rectangle(clip.ToCairo());
            _context.Clip();

            return Disposable.Create(() => _context.ResetClip());
        }

        /// <summary>
        /// Pushes an opacity value.
        /// </summary>
        /// <param name="opacity">The opacity.</param>
        /// <returns>A disposable used to undo the opacity.</returns>
        public IDisposable PushOpacity(double opacity)
        {
            // TODO: Implement
            return Disposable.Empty;
        }

        /// <summary>
        /// Pushes a matrix transformation.
        /// </summary>
        /// <param name="matrix">The matrix</param>
        /// <returns>A disposable used to undo the transformation.</returns>
        public IDisposable PushTransform(Matrix matrix)
        {
            _context.Transform(matrix.ToCairo());

            return Disposable.Create(() =>
            {
                _context.Transform(matrix.Invert().ToCairo());
            });
        }

        private void SetBrush(Brush brush, Size destinationSize)
        {
            var solid = brush as SolidColorBrush;
            var linearGradientBrush = brush as LinearGradientBrush;

            if (solid != null)
            {
                _context.SetSourceRGBA(
                    solid.Color.R / 255.0,
                    solid.Color.G / 255.0,
                    solid.Color.B / 255.0,
                    solid.Color.A / 255.0);
            }
            else if (linearGradientBrush != null)
            {
                Cairo.LinearGradient g = new Cairo.LinearGradient(linearGradientBrush.StartPoint.X * destinationSize.Width, linearGradientBrush.StartPoint.Y * destinationSize.Height, linearGradientBrush.EndPoint.X * destinationSize.Width, linearGradientBrush.EndPoint.Y * destinationSize.Height);

                foreach (var s in linearGradientBrush.GradientStops)
                    g.AddColorStopRgb(s.Offset, new Cairo.Color(s.Color.R, s.Color.G, s.Color.B, s.Color.A));

                g.Extend = Cairo.Extend.Pad;

                _context.SetSource(g);
            }
        }

        private void SetPen(Pen pen, Size destinationSize)
        {
            this.SetBrush(pen.Brush, destinationSize);
            _context.LineWidth = pen.Thickness;
        }
    }
}
