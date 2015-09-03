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
                using (var d2dBrush = this.CreateBrush(pen.Brush))
                using (var d2dStroke = pen.ToDirect2DStrokeStyle(this.renderTarget))
                {
                    this.renderTarget.DrawLine(
                        p1.ToSharpDX(),
                        p2.ToSharpDX(),
                        d2dBrush,
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
                using (var d2dBrush = this.CreateBrush(brush))
                {
                    GeometryImpl impl = (GeometryImpl)geometry.PlatformImpl;
                    this.renderTarget.FillGeometry(impl.Geometry, d2dBrush);
                }
            }

            if (pen != null)
            {
                using (var d2dBrush = this.CreateBrush(pen.Brush))
                using (var d2dStroke = pen.ToDirect2DStrokeStyle(this.renderTarget))
                {
                    GeometryImpl impl = (GeometryImpl)geometry.PlatformImpl;
                    this.renderTarget.DrawGeometry(impl.Geometry, d2dBrush, (float)pen.Thickness, d2dStroke);
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
            using (var brush = this.CreateBrush(pen.Brush))
            using (var d2dStroke = pen.ToDirect2DStrokeStyle(this.renderTarget))
            {
                this.renderTarget.DrawRoundedRectangle(
                    new RoundedRectangle { Rect = rect.ToDirect2D(), RadiusX = cornerRadius, RadiusY = cornerRadius },
                    brush,
                    (float)pen.Thickness,
                    d2dStroke);
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

                using (var brush = this.CreateBrush(foreground))
                using (var renderer = new PerspexTextRenderer(this, this.renderTarget, brush))
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
            using (var b = this.CreateBrush(brush))
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
                    b);
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
        /// Creates a Direct2D brush from a Perspex brush.
        /// </summary>
        /// <param name="brush">The perspex brush.</param>
        /// <returns>The Direct2D brush.</returns>
        public Disposable<SharpDX.Direct2D1.Brush> CreateBrush(Perspex.Media.Brush brush)
        {
            var solidColorBrush = brush as Perspex.Media.SolidColorBrush;
            var visualBrush = brush as Perspex.Media.VisualBrush;

            if (solidColorBrush != null)
            {
                return new Disposable<SharpDX.Direct2D1.Brush>(
                    new SharpDX.Direct2D1.SolidColorBrush(
                        this.renderTarget,
                        solidColorBrush.Color.ToDirect2D()));
            }
            else if (visualBrush != null)
            {
                return this.CreateBrush(visualBrush);
            }
            else
            {
                // TODO: Implement other brushes.
                return new Disposable<SharpDX.Direct2D1.Brush>(
                    new SharpDX.Direct2D1.SolidColorBrush(this.renderTarget, new Color4()));
            }
        }

        /// <summary>
        /// Creates a Direct2D <see cref="BitmapBrush"/> from a Perspex <see cref="VisualBrush"/>.
        /// </summary>
        /// <param name="brush">The perspex brush.</param>
        /// <returns>The Direct2D brush.</returns>
        private Disposable<SharpDX.Direct2D1.Brush> CreateBrush(VisualBrush brush)
        {
            var visual = brush.Visual;
            var layoutable = visual as ILayoutable;

            if (layoutable?.IsArrangeValid == false)
            {
                layoutable.Measure(Size.Infinity);
                layoutable.Arrange(new Rect(layoutable.DesiredSize));
            }

            using (var target = new BitmapRenderTarget(
                this.renderTarget,
                CompatibleRenderTargetOptions.None,
                visual.Bounds.Size.ToSharpDX()))
            {
                var renderer = new Renderer(target);
                renderer.Render(visual, null);

                var result = new BitmapBrush(this.renderTarget, target.Bitmap);
                result.ExtendModeX = ExtendMode.Wrap;
                result.ExtendModeY = ExtendMode.Wrap;
                return new Disposable<SharpDX.Direct2D1.Brush>(result, target);
            }
        }
    }
}
