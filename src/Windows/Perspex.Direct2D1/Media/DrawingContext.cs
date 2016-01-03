// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using Perspex.Media;
using SharpDX;
using SharpDX.Direct2D1;
using IBitmap = Perspex.Media.Imaging.IBitmap;
using SharpDX.Mathematics.Interop;

namespace Perspex.Direct2D1.Media
{
    /// <summary>
    /// Draws using Direct2D1.
    /// </summary>
    public class DrawingContext : IDrawingContext, IDisposable
    {
        /// <summary>
        /// The Direct2D1 render target.
        /// </summary>
        private readonly RenderTarget _renderTarget;

        /// <summary>
        /// The DirectWrite factory.
        /// </summary>
        private SharpDX.DirectWrite.Factory _directWriteFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="DrawingContext"/> class.
        /// </summary>
        /// <param name="renderTarget">The render target to draw to.</param>
        /// <param name="directWriteFactory">The DirectWrite factory.</param>
        public DrawingContext(
            RenderTarget renderTarget,
            SharpDX.DirectWrite.Factory directWriteFactory)
        {
            _renderTarget = renderTarget;
            _directWriteFactory = directWriteFactory;
            _renderTarget.BeginDraw();
        }

        /// <summary>
        /// Gets the current transform of the drawing context.
        /// </summary>
        public Matrix CurrentTransform
        {
            get { return _renderTarget.Transform.ToPerspex(); }
            private set { _renderTarget.Transform = value.ToDirect2D(); }
        }

        /// <summary>
        /// Ends a draw operation.
        /// </summary>
        public void Dispose()
        {
            _renderTarget.EndDraw();
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
            Bitmap d2d = impl.GetDirect2DBitmap(_renderTarget);
            _renderTarget.DrawBitmap(
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
        public void DrawLine(Pen pen, Point p1, Point p2)
        {
            if (pen != null)
            {
                var size = new Rect(p1, p2).Size;

                using (var d2dBrush = CreateBrush(pen.Brush, size))
                using (var d2dStroke = pen.ToDirect2DStrokeStyle(_renderTarget))
                {
                    if (d2dBrush.PlatformBrush != null)
                    {
                        _renderTarget.DrawLine(
                            p1.ToSharpDX(),
                            p2.ToSharpDX(),
                            d2dBrush.PlatformBrush,
                            (float)pen.Thickness,
                            d2dStroke);
                    }
                }
            }
        }

        /// <summary>
        /// Draws a geometry.
        /// </summary>
        /// <param name="brush">The fill brush.</param>
        /// <param name="pen">The stroke pen.</param>
        /// <param name="geometry">The geometry.</param>
        public void DrawGeometry(Perspex.Media.Brush brush, Pen pen, Perspex.Media.Geometry geometry)
        {
            if (brush != null)
            {
                using (var d2dBrush = CreateBrush(brush, geometry.Bounds.Size))
                {
                    if (d2dBrush.PlatformBrush != null)
                    {
                        var impl = (GeometryImpl)geometry.PlatformImpl;
                        _renderTarget.FillGeometry(impl.Geometry, d2dBrush.PlatformBrush);
                    }
                }
            }

            if (pen != null)
            {
                using (var d2dBrush = CreateBrush(pen.Brush, geometry.GetRenderBounds(pen.Thickness).Size))
                using (var d2dStroke = pen.ToDirect2DStrokeStyle(_renderTarget))
                {
                    if (d2dBrush.PlatformBrush != null)
                    {
                        var impl = (GeometryImpl)geometry.PlatformImpl;
                        _renderTarget.DrawGeometry(impl.Geometry, d2dBrush.PlatformBrush, (float)pen.Thickness, d2dStroke);
                    }
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
            using (var brush = CreateBrush(pen.Brush, rect.Size))
            using (var d2dStroke = pen.ToDirect2DStrokeStyle(_renderTarget))
            {
                if (brush.PlatformBrush != null)
                {
                    if (cornerRadius == 0)
                    {
                        _renderTarget.DrawRectangle(
                            rect.ToDirect2D(),
                            brush.PlatformBrush,
                            (float)pen.Thickness,
                            d2dStroke);
                    }
                    else
                    {
                        _renderTarget.DrawRoundedRectangle(
                            new RoundedRectangle { Rect = rect.ToDirect2D(), RadiusX = cornerRadius, RadiusY = cornerRadius },
                            brush.PlatformBrush,
                            (float)pen.Thickness,
                            d2dStroke);
                    }
                }
            }
        }

        /// <summary>
        /// Draws text.
        /// </summary>
        /// <param name="foreground">The foreground brush.</param>
        /// <param name="origin">The upper-left corner of the text.</param>
        /// <param name="text">The text.</param>
        public void DrawText(Perspex.Media.Brush foreground, Point origin, FormattedText text)
        {
            if (!string.IsNullOrEmpty(text.Text))
            {
                var impl = (FormattedTextImpl)text.PlatformImpl;

                using (var brush = CreateBrush(foreground, impl.Measure()))
                using (var renderer = new PerspexTextRenderer(this, _renderTarget, brush.PlatformBrush))
                {
                    if (brush.PlatformBrush != null)
                    {
                        impl.TextLayout.Draw(renderer, (float)origin.X, (float)origin.Y);
                    }
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
            using (var b = CreateBrush(brush, rect.Size))
            {
                if (b.PlatformBrush != null)
                {
                    if (cornerRadius == 0)
                    {
                        _renderTarget.FillRectangle(rect.ToDirect2D(), b.PlatformBrush);
                    }
                    else
                    {
                        _renderTarget.FillRoundedRectangle(
                            new RoundedRectangle
                            {
                                Rect = new RawRectangleF(
                                        (float)rect.X,
                                        (float)rect.Y,
                                        (float)rect.Right,
                                        (float)rect.Bottom),
                                RadiusX = cornerRadius,
                                RadiusY = cornerRadius
                            },
                            b.PlatformBrush);
                    }
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
            _renderTarget.PushAxisAlignedClip(clip.ToSharpDX(), AntialiasMode.PerPrimitive);

            return Disposable.Create(() =>
            {
                _renderTarget.PopAxisAlignedClip();
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
                    ContentBounds = PrimitiveExtensions.RectangleInfinite,
                    MaskTransform = Matrix.Identity.ToDirect2D(),
                    Opacity = (float)opacity,
                };

                var layer = new Layer(_renderTarget);

                _renderTarget.PushLayer(ref parameters, layer);

                return Disposable.Create(() =>
                {
                    _renderTarget.PopLayer();
                    layer.Dispose();
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
            RawMatrix3x2 m3x2 = matrix.ToDirect2D();
            RawMatrix3x2 transform = PrimitiveExtensions.Multiply(_renderTarget.Transform, m3x2);
            _renderTarget.Transform = transform;

            return Disposable.Create(() =>
            {
                PrimitiveExtensions.Invert(ref m3x2, out m3x2);
                _renderTarget.Transform = PrimitiveExtensions.Multiply(transform, m3x2);
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
            var imageBrush = brush as Perspex.Media.ImageBrush;
            var visualBrush = brush as Perspex.Media.VisualBrush;

            if (solidColorBrush != null)
            {
                return new SolidColorBrushImpl(solidColorBrush, _renderTarget);
            }
            else if (linearGradientBrush != null)
            {
                return new LinearGradientBrushImpl(linearGradientBrush, _renderTarget, destinationSize);
            }
            else if (imageBrush != null)
            {
                return new ImageBrushImpl(imageBrush, _renderTarget, destinationSize);
            }
            else if (visualBrush != null)
            {
                return new VisualBrushImpl(visualBrush, _renderTarget, destinationSize);
            }
            else
            {
                return new SolidColorBrushImpl(null, _renderTarget);
            }
        }
    }
}
