// -----------------------------------------------------------------------
// <copyright file="DrawingContext.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------
namespace Perspex.Cairo.Media
{
    using System;
    using System.Reactive.Disposables;
    using Perspex.Cairo.Media.Imaging;
    using Perspex.Media;
    using Perspex.Platform;
    using Splat;
    using Cairo = global::Cairo;
    using IBitmap = Perspex.Media.Imaging.IBitmap;
    using System.Collections.Generic;

    /// <summary>
    /// Draws using Direct2D1.
    /// </summary>
    public class DrawingContext : IDrawingContext, IDisposable
    {
        /// <summary>
        /// The cairo context.
        /// </summary>
        private Cairo.Context context;

        /// <summary>
        /// The cairo surface.
        /// </summary>
        private Cairo.Surface surface;

        /// <summary>
        /// Initializes a new instance of the <see cref="DrawingContext"/> class.
        /// </summary>
        /// <param name="surface">The target surface.</param>
        public DrawingContext(Cairo.Surface surface)
        {
            this.surface = surface;
            this.context = new Cairo.Context(surface);
            this.CurrentTransform = Matrix.Identity;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DrawingContext"/> class.
        /// </summary>
        /// <param name="surface">The GDK drawable.</param>
        public DrawingContext(Gdk.Drawable drawable)
        {
            this.Drawable = drawable;
            this.context = Gdk.CairoHelper.Create(drawable);
            this.CurrentTransform = Matrix.Identity;
        }

        public Matrix CurrentTransform
        {
            get;
            private set;
        }

        public Gdk.Drawable Drawable
        {
            get;
            private set;
        }

        /// <summary>
        /// Ends a draw operation.
        /// </summary>
        public void Dispose()
        {
            this.context.Dispose();

         //   if (this.surface is Cairo.Win32Surface)
       //     {
            if (this.surface != null)
                this.surface.Dispose();
           // }
        }

        public void DrawImage(IBitmap bitmap, double opacity, Rect sourceRect, Rect destRect)
        {
            var impl = bitmap.PlatformImpl as BitmapImpl;
            var size = new Size(impl.PixelWidth, impl.PixelHeight);
            var scaleX = destRect.Size.Width / sourceRect.Size.Width;
            var scaleY = destRect.Size.Height / sourceRect.Size.Height;

            this.context.Save();
            this.context.Scale(scaleX, scaleY);
            this.context.SetSourceSurface(impl.Surface, (int)sourceRect.X, (int)sourceRect.Y);
            this.context.Rectangle(sourceRect.ToCairo());
            this.context.Fill();
            this.context.Restore();
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
            this.context.LineWidth = pen.Thickness;
            this.context.MoveTo(p1.ToCairo());
            this.context.LineTo(p2.ToCairo());
            this.context.Stroke();
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
                this.context.AppendPath(impl.Path);
                
                if (brush != null)
                {
                    this.SetBrush(brush, geometry.Bounds.Size);

                    if (pen != null)
                        this.context.FillPreserve();
                    else
                        this.context.Fill();
                }


                if (pen != null)
                {
                    this.SetPen(pen, geometry.Bounds.Size);
                    this.context.Stroke();
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
            this.context.Rectangle(rect.ToCairo());
            this.context.Stroke();
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
            
            this.context.MoveTo(origin.X, origin.Y);
            Pango.CairoHelper.ShowLayout(this.context, layout);
        }

        /// <summary>
        /// Draws a filled rectangle.
        /// </summary>
        /// <param name="brush">The brush.</param>
        /// <param name="rect">The rectangle bounds.</param>
        public void FillRectange(Perspex.Media.Brush brush, Rect rect, float cornerRadius)
        {
            this.SetBrush(brush, rect.Size);
            this.context.Rectangle(rect.ToCairo());
            this.context.Fill();
        }

        /// <summary>
        /// Pushes a clip rectange.
        /// </summary>
        /// <param name="clip">The clip rectangle.</param>
        /// <returns>A disposable used to undo the clip rectangle.</returns>
        public IDisposable PushClip(Rect clip)
        {
            this.context.Rectangle(clip.ToCairo());
            this.context.Clip();

            return Disposable.Create(() => this.context.ResetClip());
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
            this.context.Transform(matrix.ToCairo());

            return Disposable.Create(() =>
            {
                this.context.Transform(matrix.Invert().ToCairo());
            });
        }

        private void SetBrush(Brush brush, Size destinationSize)
        {
            var solid = brush as SolidColorBrush;
            var linearGradientBrush = brush as LinearGradientBrush;

            if (solid != null)
            {
                this.context.SetSourceRGBA(
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

                this.context.SetSource(g);
            }
        }

        private void SetPen(Pen pen, Size destinationSize)
        {
            this.SetBrush(pen.Brush, destinationSize);
            this.context.LineWidth = pen.Thickness;
        }
    }
}
