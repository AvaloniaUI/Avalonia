// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using Avalonia.Cairo.Media.Imaging;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Media.Imaging;
// ReSharper disable PossibleNullReferenceException

namespace Avalonia.Cairo.Media
{
    using Cairo = global::Cairo;

    /// <summary>
    /// Draws using Cairo.
    /// </summary>
    public class DrawingContext : IDrawingContextImpl, IDisposable
    {
        private readonly Cairo.Context _context;
        private readonly IVisualBrushRenderer _visualBrushRenderer;
        private readonly Stack<BrushImpl> _maskStack = new Stack<BrushImpl>();

        private double _opacityOverride = 1.0f;

        /// <summary>
        /// Initializes a new instance of the <see cref="DrawingContext"/> class.
        /// </summary>
        /// <param name="surface">The target surface.</param>
        public DrawingContext(Cairo.Surface surface, IVisualBrushRenderer visualBrushRenderer)
        {
            _context = new Cairo.Context(surface);
            _visualBrushRenderer = visualBrushRenderer;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DrawingContext"/> class.
        /// </summary>
        /// <param name="surface">The GDK drawable.</param>
        public DrawingContext(Gdk.Drawable drawable, IVisualBrushRenderer visualBrushRenderer)
        {
            _context = Gdk.CairoHelper.Create(drawable);
            _visualBrushRenderer = visualBrushRenderer;
        }

        private Matrix _transform = Matrix.Identity;
        /// <summary>
        /// Gets the current transform of the drawing context.
        /// </summary>
        public Matrix Transform
        {
            get { return _transform; }
            set
            {
                _transform = value;
                _context.Matrix = value.ToCairo();

            }
        }

        public void Clear(Color color)
        {
            _context.SetSourceRGBA(color.R, color.G, color.B, color.A);
            _context.Paint();
        }

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
        public void DrawImage(IBitmapImpl bitmap, double opacity, Rect sourceRect, Rect destRect)
        {
            var pixbuf = bitmap as Gdk.Pixbuf;
            var rtb = bitmap as RenderTargetBitmapImpl;
            var size = new Size(pixbuf?.Width ?? rtb.PixelWidth, pixbuf?.Height ?? rtb.PixelHeight);
            var scale = new Vector(destRect.Width / sourceRect.Width, destRect.Height / sourceRect.Height);

            _context.Save();
            _context.Scale(scale.X, scale.Y);
            destRect /= scale;

            _context.PushGroup();

            if (pixbuf != null)
            {
                Gdk.CairoHelper.SetSourcePixbuf(
                    _context,
                    pixbuf,
                    -sourceRect.X + destRect.X,
                    -sourceRect.Y + destRect.Y);
            }
            else
            {
                _context.SetSourceSurface(
                        rtb.Surface,
                        (int)(-sourceRect.X + destRect.X),
                        (int)(-sourceRect.Y + destRect.Y));
            }

            _context.Rectangle(destRect.ToCairo());
            _context.Fill();
            _context.PopGroupToSource();
            _context.PaintWithAlpha(_opacityOverride);
            _context.Restore();
        }

        public void DrawImage(IBitmapImpl source, IBrush opacityMask, Rect opacityMaskRect, Rect destRect)
        {
            PushOpacityMask(opacityMask, opacityMaskRect);
            DrawImage(source, 1, new Rect(0, 0, source.PixelWidth, source.PixelHeight), destRect);
            PopOpacityMask();
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

            using (var p = SetPen(pen, size))
            {
                _context.MoveTo(p1.ToCairo());
                _context.LineTo(p2.ToCairo());
                _context.Stroke();
            }
        }

        /// <summary>
        /// Draws a geometry.
        /// </summary>
        /// <param name="brush">The fill brush.</param>
        /// <param name="pen">The stroke pen.</param>
        /// <param name="geometry">The geometry.</param>
        public void DrawGeometry(IBrush brush, Pen pen, IGeometryImpl geometry)
        {
            var impl = geometry as StreamGeometryImpl;

            var oldMatrix = Transform;
            Transform = impl.Transform * Transform;


            if (brush != null)
            {
                _context.AppendPath(impl.Path);
                using (var b = SetBrush(brush, geometry.Bounds.Size))
                {
                    _context.FillRule = impl.FillRule == FillRule.EvenOdd
                        ? Cairo.FillRule.EvenOdd
                        : Cairo.FillRule.Winding;

                    if (pen != null)
                        _context.FillPreserve();
                    else
                        _context.Fill();
                }
            }
            Transform = oldMatrix;

            if (pen != null)
            {
                _context.AppendPath(impl.Path);
                using (var p = SetPen(pen, geometry.Bounds.Size))
                {
                    _context.Stroke();
                }
            }
        }

        /// <summary>
        /// Draws the outline of a rectangle.
        /// </summary>
        /// <param name="pen">The pen.</param>
        /// <param name="rect">The rectangle bounds.</param>
        public void DrawRectangle(Pen pen, Rect rect, float cornerRadius)
        {
            using (var p = SetPen(pen, rect.Size))
            {
                _context.Rectangle(rect.ToCairo());
                _context.Stroke();
            }
        }

        /// <summary>
        /// Draws text.
        /// </summary>
        /// <param name="foreground">The foreground brush.</param>
        /// <param name="origin">The upper-left corner of the text.</param>
        /// <param name="text">The text.</param>
        public void DrawText(IBrush foreground, Point origin, IFormattedTextImpl text)
        {
            var layout = ((FormattedTextImpl)text).Layout;
            _context.MoveTo(origin.X, origin.Y);

            using (var b = SetBrush(foreground, new Size(0, 0)))
            {
                Pango.CairoHelper.ShowLayout(_context, layout);
            }
        }

        /// <summary>
        /// Draws a filled rectangle.
        /// </summary>
        /// <param name="brush">The brush.</param>
        /// <param name="rect">The rectangle bounds.</param>
        public void FillRectangle(IBrush brush, Rect rect, float cornerRadius)
        {
            using (var b = SetBrush(brush, rect.Size))
            {
                _context.Rectangle(rect.ToCairo());
                _context.Fill();
            }
        }

        /// <summary>
        /// Pushes a clip rectange.
        /// </summary>
        /// <param name="clip">The clip rectangle.</param>
        /// <returns>A disposable used to undo the clip rectangle.</returns>
        public void PushClip(Rect clip)
        {
            _context.Save();
            _context.Rectangle(clip.ToCairo());
            _context.Clip();
        }

        public void PopClip()
        {
            _context.Restore();
        }

        readonly Stack<double> _opacityStack = new Stack<double>();

        /// <summary>
        /// Pushes an opacity value.
        /// </summary>
        /// <param name="opacity">The opacity.</param>
        /// <returns>A disposable used to undo the opacity.</returns>
        public void PushOpacity(double opacity)
        {
            _opacityStack.Push(_opacityOverride);

            if (opacity < 1.0f)
                _opacityOverride *= opacity;

        }

        public void PopOpacity()
        {
            _opacityOverride = _opacityStack.Pop();
        }

        /// <summary>
        /// Pushes a matrix transformation.
        /// </summary>
        /// <param name="matrix">The matrix</param>
        /// <returns>A disposable used to undo the transformation.</returns>
        public IDisposable PushTransform(Matrix matrix)
        {
            _context.Save();
            _context.Transform(matrix.ToCairo());

            return Disposable.Create(() =>
            {
                _context.Restore();
            });
        }
        
        private IDisposable SetBrush(IBrush brush, Size destinationSize)
        {
            _context.Save();

            BrushImpl impl = CreateBrushImpl(brush, destinationSize);

            _context.SetSource(impl.PlatformBrush);
            return Disposable.Create(() =>
            {
                impl.Dispose();
                _context.Restore();
            });
        }

        private BrushImpl CreateBrushImpl(IBrush brush, Size destinationSize)
        {
            BrushImpl impl = null;

            if (brush is ISolidColorBrush solid)
            {
                impl = new SolidColorBrushImpl(solid, _opacityOverride);
            }
            else if (brush is ILinearGradientBrush linearGradientBrush)
            {
                impl = new LinearGradientBrushImpl(linearGradientBrush, destinationSize);
            }
            else if (brush is IRadialGradientBrush radialGradientBrush)
            {
                impl = new RadialGradientBrushImpl(radialGradientBrush, destinationSize);
            }
            else if (brush is IImageBrush imageBrush)
            {
                if (imageBrush.Source is IBitmap bitmap)
                {
                    impl = new ImageBrushImpl(imageBrush, bitmap.PlatformImpl, destinationSize);
                }
                else if (imageBrush.Source is IDrawing drawing)
                {
                    impl = new DrawingBrushImpl(imageBrush, drawing, destinationSize);
                }
            }
            else if (brush is IVisualBrush visualBrush)
            {
                if (_visualBrushRenderer != null)
                {
                    var intermediateSize = _visualBrushRenderer.GetRenderTargetSize(visualBrush);

                    if (intermediateSize.Width >= 1 && intermediateSize.Height >= 1)
                    {
                        using (var intermediate = new Cairo.ImageSurface(Cairo.Format.ARGB32, (int)intermediateSize.Width, (int)intermediateSize.Height))
                        {
                            using (var ctx = new RenderTarget(intermediate).CreateDrawingContext(_visualBrushRenderer))
                            {
                                ctx.Clear(Colors.Transparent);
                                _visualBrushRenderer.RenderVisualBrush(ctx, visualBrush);
                            }

                            return new ImageBrushImpl(
                                visualBrush,
                                new RenderTargetBitmapImpl(intermediate),
                                destinationSize);
                        }
                    }
                }
                else
                {
                    throw new NotSupportedException("No IVisualBrushRenderer was supplied to DrawingContextImpl.");
                }
            }
            
            if (impl == null)
            {
                impl = new SolidColorBrushImpl(null, _opacityOverride);
            }

            return impl;
        }

        private IDisposable SetPen(Pen pen, Size destinationSize)
        {
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

            if (pen.Brush == null)
                return Disposable.Empty;

            return SetBrush(pen.Brush, destinationSize);
        }

        public void PushGeometryClip(IGeometryImpl clip)
        {
            _context.Save();
            _context.AppendPath(((StreamGeometryImpl)clip).Path);
            _context.Clip();
        }

        public void PopGeometryClip()
        {
            _context.Restore();
        }

        public void PushOpacityMask(IBrush mask, Rect bounds)
        {
            _context.PushGroup();
            var impl = CreateBrushImpl(mask, bounds.Size);
            _maskStack.Push(impl);
        }

        public void PopOpacityMask()
        {
            _context.PopGroupToSource();
            var brushImpl = _maskStack.Pop();

            _context.Mask(brushImpl.PlatformBrush);
            brushImpl.Dispose();
        }
    }
}
