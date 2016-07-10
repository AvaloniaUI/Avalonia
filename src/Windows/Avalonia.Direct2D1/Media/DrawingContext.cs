// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.Media;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using IBitmap = Avalonia.Media.Imaging.IBitmap;

namespace Avalonia.Direct2D1.Media
{
    /// <summary>
    /// Draws using Direct2D1.
    /// </summary>
    public class DrawingContext : IDrawingContextImpl, IDisposable
    {
        /// <summary>
        /// The Direct2D1 render target.
        /// </summary>
        private readonly SharpDX.Direct2D1.RenderTarget _renderTarget;

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
            SharpDX.Direct2D1.RenderTarget renderTarget,
            SharpDX.DirectWrite.Factory directWriteFactory)
        {
            _renderTarget = renderTarget;
            _directWriteFactory = directWriteFactory;
            _renderTarget.BeginDraw();
        }

        /// <summary>
        /// Gets the current transform of the drawing context.
        /// </summary>
        public Matrix Transform
        {
            get { return _renderTarget.Transform.ToAvalonia(); }
            set { _renderTarget.Transform = value.ToDirect2D(); }
        }

        /// <summary>
        /// Ends a draw operation.
        /// </summary>
        public void Dispose()
        {
            foreach (var layer in _layerPool)
                layer.Dispose();
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
        public void DrawGeometry(IBrush brush, Pen pen, Avalonia.Media.Geometry geometry)
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
        public void DrawRectangle(Pen pen, Rect rect, float cornerRadius)
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
        public void DrawText(IBrush foreground, Point origin, FormattedText text)
        {
            if (!string.IsNullOrEmpty(text.Text))
            {
                var impl = (FormattedTextImpl)text.PlatformImpl;

                using (var brush = CreateBrush(foreground, impl.Measure()))
                using (var renderer = new AvaloniaTextRenderer(this, _renderTarget, brush.PlatformBrush))
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
        public void FillRectangle(IBrush brush, Rect rect, float cornerRadius)
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
        public void PushClip(Rect clip)
        {
            _renderTarget.PushAxisAlignedClip(clip.ToSharpDX(), AntialiasMode.PerPrimitive);
        }

        public void PopClip()
        {
            _renderTarget.PopAxisAlignedClip();
        }

        readonly Stack<Layer> _layers = new Stack<Layer>();
        private readonly Stack<Layer> _layerPool = new Stack<Layer>();
        /// <summary>
        /// Pushes an opacity value.
        /// </summary>
        /// <param name="opacity">The opacity.</param>
        /// <returns>A disposable used to undo the opacity.</returns>
        public void PushOpacity(double opacity)
        {
            if (opacity < 1)
            {
                var parameters = new LayerParameters
                {
                    ContentBounds = PrimitiveExtensions.RectangleInfinite,
                    MaskTransform = PrimitiveExtensions.Matrix3x2Identity,
                    Opacity = (float) opacity,
                };

                var layer = _layerPool.Count != 0 ? _layerPool.Pop() : new Layer(_renderTarget);
                _renderTarget.PushLayer(ref parameters, layer);

                _layers.Push(layer);
            }
            else
                _layers.Push(null);
        }

        public void PopOpacity()
        {
            PopLayer();
        }

        private void PopLayer()
        {
            var layer = _layers.Pop();
            if (layer != null)
            {
                _renderTarget.PopLayer();
                _layerPool.Push(layer);
            }
        }

        /// <summary>
        /// Creates a Direct2D brush wrapper for a Avalonia brush.
        /// </summary>
        /// <param name="brush">The avalonia brush.</param>
        /// <param name="destinationSize">The size of the brush's target area.</param>
        /// <returns>The Direct2D brush wrapper.</returns>
        public BrushImpl CreateBrush(IBrush brush, Size destinationSize)
        {
            var solidColorBrush = brush as Avalonia.Media.ISolidColorBrush;
            var linearGradientBrush = brush as Avalonia.Media.LinearGradientBrush;
            var radialGradientBrush = brush as Avalonia.Media.RadialGradientBrush;
            var imageBrush = brush as Avalonia.Media.ImageBrush;
            var visualBrush = brush as Avalonia.Media.VisualBrush;

            if (solidColorBrush != null)
            {
                return new SolidColorBrushImpl(solidColorBrush, _renderTarget);
            }
            else if (linearGradientBrush != null)
            {
                return new LinearGradientBrushImpl(linearGradientBrush, _renderTarget, destinationSize);
            }
            else if (radialGradientBrush != null)
            {
                return new RadialGradientBrushImpl(radialGradientBrush, _renderTarget, destinationSize);
            }
            else if (imageBrush != null)
            {
                return new TileBrushImpl(imageBrush, _renderTarget, destinationSize);
            }
            else if (visualBrush != null)
            {
                return new TileBrushImpl(visualBrush, _renderTarget, destinationSize);
            }
            else
            {
                return new SolidColorBrushImpl((Avalonia.Media.SolidColorBrush)null, _renderTarget);
            }
        }

        public void PushGeometryClip(Avalonia.Media.Geometry clip)
        {
            var parameters = new LayerParameters
            {
                ContentBounds = PrimitiveExtensions.RectangleInfinite,
                MaskTransform = PrimitiveExtensions.Matrix3x2Identity,
                Opacity = 1,
                GeometricMask = ((GeometryImpl)clip.PlatformImpl).Geometry
            };
            var layer = _layerPool.Count != 0 ? _layerPool.Pop() : new Layer(_renderTarget);
            _renderTarget.PushLayer(ref parameters, layer);

            _layers.Push(layer);

        }

        public void PopGeometryClip()
        {
            PopLayer();
        }

        public void PushOpacityMask(IBrush mask, Rect bounds)
        {
            var parameters = new LayerParameters
            {
                ContentBounds = PrimitiveExtensions.RectangleInfinite,
                MaskTransform = PrimitiveExtensions.Matrix3x2Identity,
                Opacity = 1,
                OpacityBrush = CreateBrush(mask, bounds.Size).PlatformBrush
            };
            var layer = _layerPool.Count != 0 ? _layerPool.Pop() : new Layer(_renderTarget);
            _renderTarget.PushLayer(ref parameters, layer);

            _layers.Push(layer);
        }

        public void PopOpacityMask()
        {
            PopLayer();
        }
    }
}
