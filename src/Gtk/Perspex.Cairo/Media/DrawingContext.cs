// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Disposables;
using Perspex.Cairo.Media.Imaging;
using Perspex.Media;

namespace Perspex.Cairo.Media
{
    using Perspex.Media.Imaging;
    using Cairo = global::Cairo;

    /// <summary>
    /// Draws using Direct2D1.
    /// </summary>
    public class DrawingContext : IDrawingContext, IDisposable
    {
        /// <summary>
        /// The cairo context.
        /// </summary>
        private readonly Cairo.Context _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="DrawingContext"/> class.
        /// </summary>
        /// <param name="surface">The target surface.</param>
        public DrawingContext(Cairo.Surface surface)
        {
            _context = new Cairo.Context(surface);
            CurrentTransform = Matrix.Identity;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DrawingContext"/> class.
        /// </summary>
        /// <param name="surface">The GDK drawable.</param>
        public DrawingContext(Gdk.Drawable drawable)
        {
            _context = Gdk.CairoHelper.Create(drawable);
            CurrentTransform = Matrix.Identity;
        }

        /// <summary>
        /// Gets the current transform of the drawing context.
        /// </summary>
        public Matrix CurrentTransform
        {
            get; }

        /// <summary>
        /// Ends a draw operation.
        /// </summary>
        public void Dispose()
        {
            _context.Dispose();
        }

        /// <summary>
        /// Draws a bitmap image.
        /// </summary>
        /// <param name="source">The bitmap image.</param>
        /// <param name="opacity">The opacity to draw with.</param>
        /// <param name="sourceRect">The rect in the image to draw.</param>
        /// <param name="destRect">The rect in the output to draw to.</param>
        public void DrawImage(IBitmap bitmap, double opacity, Rect sourceRect, Rect destRect)
        {
            var impl = bitmap.PlatformImpl as BitmapImpl;
            var size = new Size(impl.PixelWidth, impl.PixelHeight);
            var scale = new Vector(destRect.Width / sourceRect.Width, destRect.Height / sourceRect.Height);

            _context.Save();
            _context.Scale(scale.X, scale.Y);
            destRect /= scale;

            Gdk.CairoHelper.SetSourcePixbuf(
                _context, 
                impl.Surface, 
                -sourceRect.X + destRect.X, 
                -sourceRect.Y + destRect.Y);

            _context.Rectangle(destRect.ToCairo());
            _context.Fill();
            _context.Restore();
        }

        /// <summary>
        /// Draws a line.
        /// </summary>
        /// <param name="pen">The stroke pen.</param>
        /// <param name="p1">The first point of the line.</param>
        /// <param name="p1">The second point of the line.</param>
        public void DrawLine(Pen pen, Point p1, Point p2)
        {
            var size = new Rect(p1, p2).Size;
            
            SetPen(pen, size);

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
        public void DrawGeometry(Brush brush, Pen pen, Geometry geometry)
        {
            var impl = geometry.PlatformImpl as StreamGeometryImpl;

            using (var pop = PushTransform(impl.Transform))
            {
                _context.AppendPath(impl.Path);

                if (brush != null)
                {
                    SetBrush(brush, geometry.Bounds.Size);

                    if (pen != null)
                        _context.FillPreserve();
                    else
                        _context.Fill();
                }


                if (pen != null)
                {
                    SetPen(pen, geometry.Bounds.Size);
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
            SetPen(pen, rect.Size);
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
            _context.MoveTo(origin.X, origin.Y);

            SetBrush(foreground, new Size(0, 0));
            Pango.CairoHelper.ShowLayout(_context, layout);
        }

        /// <summary>
        /// Draws a filled rectangle.
        /// </summary>
        /// <param name="brush">The brush.</param>
        /// <param name="rect">The rectangle bounds.</param>
        public void FillRectange(Brush brush, Rect rect, float cornerRadius)
        {
            SetBrush(brush, rect.Size);
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
                    g.AddColorStop(s.Offset, new Cairo.Color(s.Color.R / 255.0, s.Color.G / 255.0, s.Color.B / 255.0, s.Color.A / 255.0));

                g.Extend = Cairo.Extend.Pad;

                _context.SetSource(g);
            }
        }

        private void SetPen(Pen pen, Size destinationSize)
        {
            SetBrush(pen.Brush, destinationSize);
            if (pen.DashStyle != null)
            {
                if (pen.DashStyle.Dashes != null && pen.DashStyle.Dashes.Count > 0)
                {
                    var cray = pen.DashStyle.Dashes.ToArray();
                    _context.SetDash(cray, pen.DashStyle.Offset);
                }
            }

            _context.LineWidth = pen.Thickness;
            _context.MiterLimit = pen.MiterLimit;

            // Line caps and joins are currently broken on Cairo. I've defaulted them to sensible defaults for now.
            // Cairo does not have StartLineCap, EndLineCap, and DashCap properties, whereas Direct2D does. 
            // TODO: Figure out a solution for this.
            _context.LineJoin = Cairo.LineJoin.Miter;
            _context.LineCap = Cairo.LineCap.Butt;
        }
    }
}
