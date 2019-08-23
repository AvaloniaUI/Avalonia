// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Utilities;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using BitmapInterpolationMode = Avalonia.Visuals.Media.Imaging.BitmapInterpolationMode;

namespace Avalonia.Direct2D1.Media
{
    /// <summary>
    /// Draws using Direct2D1.
    /// </summary>
    public class DrawingContextImpl : IDrawingContextImpl
    {
        private readonly IVisualBrushRenderer _visualBrushRenderer;
        private readonly ILayerFactory _layerFactory;
        private readonly SharpDX.Direct2D1.RenderTarget _renderTarget;
        private readonly DeviceContext _deviceContext;
        private readonly SharpDX.DXGI.SwapChain1 _swapChain;
        private readonly Action _finishedCallback;

        /// <summary>
        /// Initializes a new instance of the <see cref="DrawingContextImpl"/> class.
        /// </summary>
        /// <param name="visualBrushRenderer">The visual brush renderer.</param>
        /// <param name="renderTarget">The render target to draw to.</param>
        /// <param name="layerFactory">
        /// An object to use to create layers. May be null, in which case a
        /// <see cref="WicRenderTargetBitmapImpl"/> will created when a new layer is requested.
        /// </param>
        /// <param name="swapChain">An optional swap chain associated with this drawing context.</param>
        /// <param name="finishedCallback">An optional delegate to be called when context is disposed.</param>
        public DrawingContextImpl(
            IVisualBrushRenderer visualBrushRenderer,
            ILayerFactory layerFactory,
            SharpDX.Direct2D1.RenderTarget renderTarget,
            SharpDX.DXGI.SwapChain1 swapChain = null,
            Action finishedCallback = null)
        {
            _visualBrushRenderer = visualBrushRenderer;
            _layerFactory = layerFactory;
            _renderTarget = renderTarget;
            _swapChain = swapChain;
            _finishedCallback = finishedCallback;

            if (_renderTarget is DeviceContext deviceContext)
            {
                _deviceContext = deviceContext;
            }
            else
            {
                _deviceContext = _renderTarget.QueryInterface<DeviceContext>();
            }

            _deviceContext.BeginDraw();
        }

        /// <summary>
        /// Gets the current transform of the drawing context.
        /// </summary>
        public Matrix Transform
        {
            get { return _deviceContext.Transform.ToAvalonia(); }
            set { _deviceContext.Transform = value.ToDirect2D(); }
        }

        /// <inheritdoc/>
        public void Clear(Color color)
        {
            _deviceContext.Clear(color.ToDirect2D());
        }

        /// <summary>
        /// Ends a draw operation.
        /// </summary>
        public void Dispose()
        {
            foreach (var layer in _layerPool)
            {
                layer.Dispose();
            }

            try
            {
                _deviceContext.EndDraw();

                _swapChain?.Present(1, SharpDX.DXGI.PresentFlags.None);
                _finishedCallback?.Invoke();
            }
            catch (SharpDXException ex) when ((uint)ex.HResult == 0x8899000C) // D2DERR_RECREATE_TARGET
            {
                throw new RenderTargetCorruptedException(ex);
            }
        }

        /// <summary>
        /// Draws a bitmap image.
        /// </summary>
        /// <param name="source">The bitmap image.</param>
        /// <param name="opacity">The opacity to draw with.</param>
        /// <param name="sourceRect">The rect in the image to draw.</param>
        /// <param name="destRect">The rect in the output to draw to.</param>
        /// <param name="bitmapInterpolationMode">The bitmap interpolation mode.</param>
        public void DrawImage(IRef<IBitmapImpl> source, double opacity, Rect sourceRect, Rect destRect, BitmapInterpolationMode bitmapInterpolationMode)
        {
            using (var d2d = ((BitmapImpl)source.Item).GetDirect2DBitmap(_deviceContext))
            {
                var interpolationMode = GetInterpolationMode(bitmapInterpolationMode);

                _deviceContext.DrawBitmap(
                    d2d.Value,
                    destRect.ToSharpDX(),
                    (float)opacity,
                    interpolationMode,
                    sourceRect.ToSharpDX(),
                    null);
            }
        }

        private static InterpolationMode GetInterpolationMode(BitmapInterpolationMode interpolationMode)
        {
            switch (interpolationMode)
            {
                case BitmapInterpolationMode.LowQuality:
                    return InterpolationMode.NearestNeighbor;
                case BitmapInterpolationMode.MediumQuality:
                    return InterpolationMode.Linear;
                case BitmapInterpolationMode.HighQuality:
                    return InterpolationMode.HighQualityCubic;
                case BitmapInterpolationMode.Default:
                    return InterpolationMode.Linear;
                default:
                    throw new ArgumentOutOfRangeException(nameof(interpolationMode), interpolationMode, null);
            }
        }

        /// <summary>
        /// Draws a bitmap image.
        /// </summary>
        /// <param name="source">The bitmap image.</param>
        /// <param name="opacityMask">The opacity mask to draw with.</param>
        /// <param name="opacityMaskRect">The destination rect for the opacity mask.</param>
        /// <param name="destRect">The rect in the output to draw to.</param>
        public void DrawImage(IRef<IBitmapImpl> source, IBrush opacityMask, Rect opacityMaskRect, Rect destRect)
        {
            using (var d2dSource = ((BitmapImpl)source.Item).GetDirect2DBitmap(_deviceContext))
            using (var sourceBrush = new BitmapBrush(_deviceContext, d2dSource.Value))
            using (var d2dOpacityMask = CreateBrush(opacityMask, opacityMaskRect.Size))
            using (var geometry = new SharpDX.Direct2D1.RectangleGeometry(_deviceContext.Factory, destRect.ToDirect2D()))
            {
                if (d2dOpacityMask.PlatformBrush != null)
                {
                    d2dOpacityMask.PlatformBrush.Transform = Matrix.CreateTranslation(opacityMaskRect.Position).ToDirect2D();
                }

                _deviceContext.FillGeometry(
                    geometry,
                    sourceBrush,
                    d2dOpacityMask.PlatformBrush);
            }
        }

        /// <summary>
        /// Draws a line.
        /// </summary>
        /// <param name="pen">The stroke pen.</param>
        /// <param name="p1">The first point of the line.</param>
        /// <param name="p2">The second point of the line.</param>
        public void DrawLine(IPen pen, Point p1, Point p2)
        {
            if (pen != null)
            {
                var size = new Rect(p1, p2).Size;

                using (var d2dBrush = CreateBrush(pen.Brush, size))
                using (var d2dStroke = pen.ToDirect2DStrokeStyle(_deviceContext))
                {
                    if (d2dBrush.PlatformBrush != null)
                    {
                        _deviceContext.DrawLine(
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
        public void DrawGeometry(IBrush brush, IPen pen, IGeometryImpl geometry)
        {
            if (brush != null)
            {
                using (var d2dBrush = CreateBrush(brush, geometry.Bounds.Size))
                {
                    if (d2dBrush.PlatformBrush != null)
                    {
                        var impl = (GeometryImpl)geometry;
                        _deviceContext.FillGeometry(impl.Geometry, d2dBrush.PlatformBrush);
                    }
                }
            }

            if (pen != null)
            {
                using (var d2dBrush = CreateBrush(pen.Brush, geometry.GetRenderBounds(pen).Size))
                using (var d2dStroke = pen.ToDirect2DStrokeStyle(_deviceContext))
                {
                    if (d2dBrush.PlatformBrush != null)
                    {
                        var impl = (GeometryImpl)geometry;
                        _deviceContext.DrawGeometry(impl.Geometry, d2dBrush.PlatformBrush, (float)pen.Thickness, d2dStroke);
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
        public void DrawRectangle(IPen pen, Rect rect, float cornerRadius)
        {
            using (var brush = CreateBrush(pen.Brush, rect.Size))
            using (var d2dStroke = pen.ToDirect2DStrokeStyle(_deviceContext))
            {
                if (brush.PlatformBrush != null)
                {
                    if (cornerRadius == 0)
                    {
                        _deviceContext.DrawRectangle(
                            rect.ToDirect2D(),
                            brush.PlatformBrush,
                            (float)pen.Thickness,
                            d2dStroke);
                    }
                    else
                    {
                        _deviceContext.DrawRoundedRectangle(
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
        public void DrawText(IBrush foreground, Point origin, IFormattedTextImpl text)
        {
            if (!string.IsNullOrEmpty(text.Text))
            {
                var impl = (FormattedTextImpl)text;

                using (var brush = CreateBrush(foreground, impl.Bounds.Size))
                using (var renderer = new AvaloniaTextRenderer(this, _deviceContext, brush.PlatformBrush))
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
                        _deviceContext.FillRectangle(rect.ToDirect2D(), b.PlatformBrush);
                    }
                    else
                    {
                        _deviceContext.FillRoundedRectangle(
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

        public IRenderTargetBitmapImpl CreateLayer(Size size)
        {
            if (_layerFactory != null)
            {
                return _layerFactory.CreateLayer(size);
            }
            else
            {
                var platform = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();
                var dpi = new Vector(_deviceContext.DotsPerInch.Width, _deviceContext.DotsPerInch.Height);
                var pixelSize = PixelSize.FromSizeWithDpi(size, dpi);
                return platform.CreateRenderTargetBitmap(pixelSize, dpi);
            }
        }

        /// <summary>
        /// Pushes a clip rectange.
        /// </summary>
        /// <param name="clip">The clip rectangle.</param>
        /// <returns>A disposable used to undo the clip rectangle.</returns>
        public void PushClip(Rect clip)
        {
            _deviceContext.PushAxisAlignedClip(clip.ToSharpDX(), AntialiasMode.PerPrimitive);
        }

        public void PopClip()
        {
            _deviceContext.PopAxisAlignedClip();
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
                    Opacity = (float)opacity,
                };

                var layer = _layerPool.Count != 0 ? _layerPool.Pop() : new Layer(_deviceContext);
                _deviceContext.PushLayer(ref parameters, layer);

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
                _deviceContext.PopLayer();
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
            var solidColorBrush = brush as ISolidColorBrush;
            var linearGradientBrush = brush as ILinearGradientBrush;
            var radialGradientBrush = brush as IRadialGradientBrush;
            var imageBrush = brush as IImageBrush;
            var visualBrush = brush as IVisualBrush;

            if (solidColorBrush != null)
            {
                return new SolidColorBrushImpl(solidColorBrush, _deviceContext);
            }
            else if (linearGradientBrush != null)
            {
                return new LinearGradientBrushImpl(linearGradientBrush, _deviceContext, destinationSize);
            }
            else if (radialGradientBrush != null)
            {
                return new RadialGradientBrushImpl(radialGradientBrush, _deviceContext, destinationSize);
            }
            else if (imageBrush?.Source != null)
            {
                return new ImageBrushImpl(
                    imageBrush,
                    _deviceContext,
                    (BitmapImpl)imageBrush.Source.PlatformImpl.Item,
                    destinationSize);
            }
            else if (visualBrush != null)
            {
                if (_visualBrushRenderer != null)
                {
                    var intermediateSize = _visualBrushRenderer.GetRenderTargetSize(visualBrush);

                    if (intermediateSize.Width >= 1 && intermediateSize.Height >= 1)
                    {
                        // We need to ensure the size we're requesting is an integer pixel size, otherwise
                        // D2D alters the DPI of the render target, which messes stuff up. PixelSize.FromSize
                        // will do the rounding for us.
                        var dpi = new Vector(_deviceContext.DotsPerInch.Width, _deviceContext.DotsPerInch.Height);
                        var pixelSize = PixelSize.FromSizeWithDpi(intermediateSize, dpi);

                        using (var intermediate = new BitmapRenderTarget(
                            _deviceContext,
                            CompatibleRenderTargetOptions.None,
                            pixelSize.ToSizeWithDpi(dpi).ToSharpDX()))
                        {
                            using (var ctx = new RenderTarget(intermediate).CreateDrawingContext(_visualBrushRenderer))
                            {
                                intermediate.Clear(null);
                                _visualBrushRenderer.RenderVisualBrush(ctx, visualBrush);
                            }

                            return new ImageBrushImpl(
                                visualBrush,
                                _deviceContext,
                                new D2DBitmapImpl(intermediate.Bitmap),
                                destinationSize);
                        }
                    }
                }
                else
                {
                    throw new NotSupportedException("No IVisualBrushRenderer was supplied to DrawingContextImpl.");
                }
            }

            return new SolidColorBrushImpl(null, _deviceContext);
        }

        public void PushGeometryClip(IGeometryImpl clip)
        {
            var parameters = new LayerParameters
            {
                ContentBounds = PrimitiveExtensions.RectangleInfinite,
                MaskTransform = PrimitiveExtensions.Matrix3x2Identity,
                Opacity = 1,
                GeometricMask = ((GeometryImpl)clip).Geometry
            };
            var layer = _layerPool.Count != 0 ? _layerPool.Pop() : new Layer(_deviceContext);
            _deviceContext.PushLayer(ref parameters, layer);

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
            var layer = _layerPool.Count != 0 ? _layerPool.Pop() : new Layer(_deviceContext);
            _deviceContext.PushLayer(ref parameters, layer);

            _layers.Push(layer);
        }

        public void PopOpacityMask()
        {
            PopLayer();
        }
        
        public void Custom(ICustomDrawOperation custom) => custom.Render(this);
    }
}
