// -----------------------------------------------------------------------
// <copyright file="DrawingContext.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Direct2D1.Media
{
    using System;
    using System.Reactive.Disposables;
    using Layout;
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

        /// <summary>
        /// Gets the current transform of the drawing context.
        /// </summary>
        public Matrix CurrentTransform
        {
            get { return this.renderTarget.Transform.ToPerspex(); }
            private set { this.renderTarget.Transform = value.ToDirect2D(); }
        }

        /// <summary>
        /// Ends a draw operation.
        /// </summary>
        public void Dispose()
        {
            this.renderTarget.EndDraw();
        }

        /// <summary>
        /// Draws a bitmap image.
        /// </summary>
        /// <param name="source">The bitmap image.</param>
        /// <param name="opacity">The opacity to draw with.</param>
        /// <param name="sourceRect">The rect in the image to draw.</param>
        /// <param name="destRect">The rect in the output to draw to.</param>
        public void DrawImage(IBitmap source, double opacity, Rect sourceRect, Rect destRect)
        {
            BitmapImpl impl = (BitmapImpl)source.PlatformImpl;
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
        /// <param name="p2">The second point of the line.</param>
        public void DrawLine(Pen pen, Perspex.Point p1, Perspex.Point p2)
        {
            if (pen != null)
            {
                var size = new Rect(p1, p2).Size;

                using (var d2dBrush = this.CreateBrush(pen.Brush, size))
                using (var d2dStroke = pen.ToDirect2DStrokeStyle(this.renderTarget))
                {
                    this.renderTarget.DrawLine(
                        p1.ToSharpDX(),
                        p2.ToSharpDX(),
                        d2dBrush.PlatformBrush,
                        (float)pen.Thickness,
                        d2dStroke);
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
                using (var d2dBrush = this.CreateBrush(brush, geometry.Bounds.Size))
                {
                    GeometryImpl impl = (GeometryImpl)geometry.PlatformImpl;
                    this.renderTarget.FillGeometry(impl.Geometry, d2dBrush.PlatformBrush);
                }
            }

            if (pen != null)
            {
                using (var d2dBrush = this.CreateBrush(pen.Brush, geometry.GetRenderBounds(pen.Thickness).Size))
                using (var d2dStroke = pen.ToDirect2DStrokeStyle(this.renderTarget))
                {
                    GeometryImpl impl = (GeometryImpl)geometry.PlatformImpl;
                    this.renderTarget.DrawGeometry(impl.Geometry, d2dBrush.PlatformBrush, (float)pen.Thickness, d2dStroke);
                }
            }
        }

        /// <summary>
        /// Draws the outline of a rectangle.
        /// </summary>
        /// <param name="pen">The pen.</param>
        /// <param name="rect">The rectangle bounds.</param>
        /// <param name="cornerRadius">The corner radius.</param>
        public void DrawRectange(Pen pen, Rect rect, float cornerRadius)
        {
            using (var brush = this.CreateBrush(pen.Brush, rect.Size))
            using (var d2dStroke = pen.ToDirect2DStrokeStyle(this.renderTarget))
            {
                if (cornerRadius == 0)
                {
                    this.renderTarget.DrawRectangle(
                        rect.ToDirect2D(),
                        brush.PlatformBrush,
                        (float)pen.Thickness,
                        d2dStroke);
                }
                else
                {
                    this.renderTarget.DrawRoundedRectangle(
                        new RoundedRectangle { Rect = rect.ToDirect2D(), RadiusX = cornerRadius, RadiusY = cornerRadius },
                        brush.PlatformBrush,
                        (float)pen.Thickness,
                        d2dStroke);
                }
            }
        }

        /// <summary>
        /// Draws text.
        /// </summary>
        /// <param name="foreground">The foreground brush.</param>
        /// <param name="origin">The upper-left corner of the text.</param>
        /// <param name="text">The text.</param>
        public void DrawText(Perspex.Media.Brush foreground, Perspex.Point origin, FormattedText text)
        {
            if (!string.IsNullOrEmpty(text.Text))
            {
                var impl = (FormattedTextImpl)text.PlatformImpl;

                using (var brush = this.CreateBrush(foreground, impl.Measure()))
                using (var renderer = new PerspexTextRenderer(this, this.renderTarget, brush.PlatformBrush))
                {
                    impl.TextLayout.Draw(renderer, (float)origin.X, (float)origin.Y);
                }
            }
        }

        /// <summary>
        /// Draws a filled rectangle.
        /// </summary>
        /// <param name="brush">The brush.</param>
        /// <param name="rect">The rectangle bounds.</param>
        /// <param name="cornerRadius">The corner radius.</param>
        public void FillRectange(Perspex.Media.Brush brush, Rect rect, float cornerRadius)
        {
            using (var b = this.CreateBrush(brush, rect.Size))
            {
                if (cornerRadius == 0)
                {
                    this.renderTarget.FillRectangle(rect.ToDirect2D(), b.PlatformBrush);
                }
                else
                {
                    this.renderTarget.FillRoundedRectangle(
                        new RoundedRectangle
                        {
                            Rect = new RectangleF(
                                    (float)rect.X,
                                    (float)rect.Y,
                                    (float)rect.Width,
                                    (float)rect.Height),
                            RadiusX = cornerRadius,
                            RadiusY = cornerRadius
                        },
                        b.PlatformBrush);
                }
            }
        }

        /// <summary>
        /// Pushes a clip rectange.
        /// </summary>
        /// <param name="clip">The clip rectangle.</param>
        /// <returns>A disposable used to undo the clip rectangle.</returns>
        public IDisposable PushClip(Rect clip)
        {
            this.renderTarget.PushAxisAlignedClip(clip.ToSharpDX(), AntialiasMode.PerPrimitive);

            return Disposable.Create(() =>
            {
                this.renderTarget.PopAxisAlignedClip();
            });
        }

        /// <summary>
        /// Pushes an opacity value.
        /// </summary>
        /// <param name="opacity">The opacity.</param>
        /// <returns>A disposable used to undo the opacity.</returns>
        public IDisposable PushOpacity(double opacity)
        {
            if (opacity < 1)
            {
                var parameters = new LayerParameters
                {
                    ContentBounds = RectangleF.Infinite,
                    MaskTransform = Matrix3x2.Identity,
                    Opacity = (float)opacity,
                };

                var layer = new Layer(this.renderTarget);

                this.renderTarget.PushLayer(ref parameters, layer);

                return Disposable.Create(() =>
                {
                    this.renderTarget.PopLayer();
                });
            }
            else
            {
                return Disposable.Empty;
            }
        }

        /// <summary>
        /// Pushes a matrix transformation.
        /// </summary>
        /// <param name="matrix">The matrix</param>
        /// <returns>A disposable used to undo the transformation.</returns>
        public IDisposable PushTransform(Matrix matrix)
        {
            Matrix3x2 m3x2 = matrix.ToDirect2D();
            Matrix3x2 transform = this.renderTarget.Transform * m3x2;
            this.renderTarget.Transform = transform;

            return Disposable.Create(() =>
            {
                m3x2.Invert();
                this.renderTarget.Transform = transform * m3x2;
            });
        }

        /// <summary>
        /// Creates a Direct2D brush wrapper for a Perspex brush.
        /// </summary>
        /// <param name="brush">The perspex brush.</param>
        /// <param name="destinationSize">The size of the brush's target area.</param>
        /// <returns>The Direct2D brush wrapper.</returns>
        public BrushImpl CreateBrush(Perspex.Media.Brush brush, Size destinationSize)
        {
            var solidColorBrush = brush as Perspex.Media.SolidColorBrush;
            var linearGradientBrush = brush as Perspex.Media.LinearGradientBrush;
            var visualBrush = brush as Perspex.Media.VisualBrush;

            if (solidColorBrush != null)
            {
                return new SolidColorBrushImpl(solidColorBrush, this.renderTarget, destinationSize);
            }
            else if (linearGradientBrush != null)
            {
                return new LinearGradientBrushImpl(linearGradientBrush, this.renderTarget, destinationSize);
            }
            else if (visualBrush != null)
            {
                return new VisualBrushImpl(visualBrush, this.renderTarget, destinationSize);
            }
            else
            {
                return new SolidColorBrushImpl(null, this.renderTarget, destinationSize);
            }
        }
    }
}
