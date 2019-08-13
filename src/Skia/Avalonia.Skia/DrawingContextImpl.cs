// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Rendering.Utilities;
using Avalonia.Utilities;
using Avalonia.Visuals.Media.Imaging;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// Skia based drawing context.
    /// </summary>
    internal class DrawingContextImpl : IDrawingContextImpl, ISkiaDrawingContextImpl
    {
        private IDisposable[] _disposables;
        private readonly Vector _dpi;
        private readonly Stack<PaintWrapper> _maskStack = new Stack<PaintWrapper>();
        private readonly Stack<double> _opacityStack = new Stack<double>();
        private readonly Matrix? _postTransform;
        private readonly IVisualBrushRenderer _visualBrushRenderer;
        private double _currentOpacity = 1.0f;
        private readonly bool _canTextUseLcdRendering;
        private Matrix _currentTransform;
        private GRContext _grContext;
        private bool _disposed;
        /// <summary>
        /// Context create info.
        /// </summary>
        public struct CreateInfo
        {
            /// <summary>
            /// Canvas to draw to.
            /// </summary>
            public SKCanvas Canvas;

            /// <summary>
            /// Dpi of drawings.
            /// </summary>
            public Vector Dpi;

            /// <summary>
            /// Visual brush renderer.
            /// </summary>
            public IVisualBrushRenderer VisualBrushRenderer;

            /// <summary>
            /// Render text without Lcd rendering.
            /// </summary>
            public bool DisableTextLcdRendering;

            /// <summary>
            /// GPU-accelerated context (optional)
            /// </summary>
            public GRContext GrContext;
        }

        /// <summary>
        /// Create new drawing context.
        /// </summary>
        /// <param name="createInfo">Create info.</param>
        /// <param name="disposables">Array of elements to dispose after drawing has finished.</param>
        public DrawingContextImpl(CreateInfo createInfo, params IDisposable[] disposables)
        {
            _dpi = createInfo.Dpi;
            _visualBrushRenderer = createInfo.VisualBrushRenderer;
            _disposables = disposables;
            _canTextUseLcdRendering = !createInfo.DisableTextLcdRendering;
            _grContext = createInfo.GrContext;
            if (_grContext != null)
                Monitor.Enter(_grContext);
            
            Canvas = createInfo.Canvas;

            if (Canvas == null)
            {
                throw new ArgumentException("Invalid create info - no Canvas provided", nameof(createInfo));
            }

            if (!_dpi.NearlyEquals(SkiaPlatform.DefaultDpi))
            {
                _postTransform =
                    Matrix.CreateScale(_dpi.X / SkiaPlatform.DefaultDpi.X, _dpi.Y / SkiaPlatform.DefaultDpi.Y);
            }

            Transform = Matrix.Identity;
        }
        
        /// <summary>
        /// Skia canvas.
        /// </summary>
        public SKCanvas Canvas { get; }

        SKCanvas ISkiaDrawingContextImpl.SkCanvas => Canvas;
        GRContext ISkiaDrawingContextImpl.GrContext => _grContext;

        /// <inheritdoc />
        public void Clear(Color color)
        {
            Canvas.Clear(color.ToSKColor());
        }

        /// <inheritdoc />
        public void DrawImage(IRef<IBitmapImpl> source, double opacity, Rect sourceRect, Rect destRect, BitmapInterpolationMode bitmapInterpolationMode)
        {
            var drawableImage = (IDrawableBitmapImpl)source.Item;
            var s = sourceRect.ToSKRect();
            var d = destRect.ToSKRect();

            using (var paint =
                new SKPaint
                {
                    Color = new SKColor(255, 255, 255, (byte)(255 * opacity * _currentOpacity))
                })
            {
                paint.FilterQuality = GetInterpolationMode(bitmapInterpolationMode);

                drawableImage.Draw(this, s, d, paint);
            }
        }

        private static SKFilterQuality GetInterpolationMode(BitmapInterpolationMode interpolationMode)
        {
            switch (interpolationMode)
            {
                case BitmapInterpolationMode.LowQuality:
                    return SKFilterQuality.Low;
                case BitmapInterpolationMode.MediumQuality:
                    return SKFilterQuality.Medium;
                case BitmapInterpolationMode.HighQuality:
                    return SKFilterQuality.High;
                case BitmapInterpolationMode.Default:
                    return SKFilterQuality.None;
                default:
                    throw new ArgumentOutOfRangeException(nameof(interpolationMode), interpolationMode, null);
            }
        }

        /// <inheritdoc />
        public void DrawImage(IRef<IBitmapImpl> source, IBrush opacityMask, Rect opacityMaskRect, Rect destRect)
        {
            PushOpacityMask(opacityMask, opacityMaskRect);
            DrawImage(source, 1, new Rect(0, 0, source.Item.PixelSize.Width, source.Item.PixelSize.Height), destRect, BitmapInterpolationMode.Default);
            PopOpacityMask();
        }

        /// <inheritdoc />
        public void DrawLine(IPen pen, Point p1, Point p2)
        {
            using (var paint = CreatePaint(pen, new Size(Math.Abs(p2.X - p1.X), Math.Abs(p2.Y - p1.Y))))
            {
                Canvas.DrawLine((float) p1.X, (float) p1.Y, (float) p2.X, (float) p2.Y, paint.Paint);
            }
        }

        /// <inheritdoc />
        public void DrawGeometry(IBrush brush, IPen pen, IGeometryImpl geometry)
        {
            var impl = (GeometryImpl) geometry;
            var size = geometry.Bounds.Size;

            using (var fill = brush != null ? CreatePaint(brush, size) : default(PaintWrapper))
            using (var stroke = pen?.Brush != null ? CreatePaint(pen, size) : default(PaintWrapper))
            {
                if (fill.Paint != null)
                {
                    Canvas.DrawPath(impl.EffectivePath, fill.Paint);
                }

                if (stroke.Paint != null)
                {
                    Canvas.DrawPath(impl.EffectivePath, stroke.Paint);
                }
            }
        }

        /// <inheritdoc />
        public void DrawRectangle(IPen pen, Rect rect, float cornerRadius = 0)
        {
            using (var paint = CreatePaint(pen, rect.Size))
            {
                var rc = rect.ToSKRect();

                if (Math.Abs(cornerRadius) < float.Epsilon)
                {
                    Canvas.DrawRect(rc, paint.Paint);
                }
                else
                {
                    Canvas.DrawRoundRect(rc, cornerRadius, cornerRadius, paint.Paint);
                }
            }
        }

        /// <inheritdoc />
        public void FillRectangle(IBrush brush, Rect rect, float cornerRadius = 0)
        {
            using (var paint = CreatePaint(brush, rect.Size))
            {
                var rc = rect.ToSKRect();

                if (Math.Abs(cornerRadius) < float.Epsilon)
                {
                    Canvas.DrawRect(rc, paint.Paint);
                }
                else
                {
                    Canvas.DrawRoundRect(rc, cornerRadius, cornerRadius, paint.Paint);
                }
            }
        }

        /// <inheritdoc />
        public void DrawText(IBrush foreground, Point origin, IFormattedTextImpl text)
        {
            using (var paint = CreatePaint(foreground, text.Bounds.Size))
            {
                var textImpl = (FormattedTextImpl) text;
                textImpl.Draw(this, Canvas, origin.ToSKPoint(), paint, _canTextUseLcdRendering);
            }
        }

        /// <inheritdoc />
        public IRenderTargetBitmapImpl CreateLayer(Size size)
        {
            return CreateRenderTarget(size);
        }

        /// <inheritdoc />
        public void PushClip(Rect clip)
        {
            Canvas.Save();
            Canvas.ClipRect(clip.ToSKRect());
        }

        /// <inheritdoc />
        public void PopClip()
        {
            Canvas.Restore();
        }

        /// <inheritdoc />
        public void PushOpacity(double opacity)
        {
            _opacityStack.Push(_currentOpacity);
            _currentOpacity *= opacity;
        }

        /// <inheritdoc />
        public void PopOpacity()
        {
            _currentOpacity = _opacityStack.Pop();
        }

        /// <inheritdoc />
        public virtual void Dispose()
        {
            if(_disposed)
                return;
            try
            {
                if (_grContext != null)
                {
                    Monitor.Exit(_grContext);
                    _grContext = null;
                }

                if (_disposables != null)
                {
                    foreach (var disposable in _disposables)
                        disposable?.Dispose();
                    _disposables = null;
                }
            }
            finally
            {
                _disposed = true;
            }
        }

        /// <inheritdoc />
        public void PushGeometryClip(IGeometryImpl clip)
        {
            Canvas.Save();
            Canvas.ClipPath(((GeometryImpl)clip).EffectivePath);
        }

        /// <inheritdoc />
        public void PopGeometryClip()
        {
            Canvas.Restore();
        }

        public void Custom(ICustomDrawOperation custom) => custom.Render(this);

        /// <inheritdoc />
        public void PushOpacityMask(IBrush mask, Rect bounds)
        {
            // TODO: This should be disposed
            var paint = new SKPaint();

            Canvas.SaveLayer(paint);
            _maskStack.Push(CreatePaint(mask, bounds.Size));
        }

        /// <inheritdoc />
        public void PopOpacityMask()
        {
            using (var paint = new SKPaint { BlendMode = SKBlendMode.DstIn })
            {
                Canvas.SaveLayer(paint);
                using (var paintWrapper = _maskStack.Pop())
                {
                    Canvas.DrawPaint(paintWrapper.Paint);
                }
                Canvas.Restore();
            }

            Canvas.Restore();
        }

        /// <inheritdoc />
        public Matrix Transform
        {
            get { return _currentTransform; }
            set
            {
                if (_currentTransform == value)
                    return;

                _currentTransform = value;

                var transform = value;

                if (_postTransform.HasValue)
                {
                    transform *= _postTransform.Value;
                }

                Canvas.SetMatrix(transform.ToSKMatrix());
            }
        }

        /// <summary>
        /// Configure paint wrapper for using gradient brush.
        /// </summary>
        /// <param name="paintWrapper">Paint wrapper.</param>
        /// <param name="targetSize">Target size.</param>
        /// <param name="gradientBrush">Gradient brush.</param>
        private void ConfigureGradientBrush(ref PaintWrapper paintWrapper, Size targetSize, IGradientBrush gradientBrush)
        {
            var tileMode = gradientBrush.SpreadMethod.ToSKShaderTileMode();
            var stopColors = gradientBrush.GradientStops.Select(s => s.Color.ToSKColor()).ToArray();
            var stopOffsets = gradientBrush.GradientStops.Select(s => (float)s.Offset).ToArray();

            switch (gradientBrush)
            {
                case ILinearGradientBrush linearGradient:
                {
                    var start = linearGradient.StartPoint.ToPixels(targetSize).ToSKPoint();
                    var end = linearGradient.EndPoint.ToPixels(targetSize).ToSKPoint();

                    // would be nice to cache these shaders possibly?
                    using (var shader =
                        SKShader.CreateLinearGradient(start, end, stopColors, stopOffsets, tileMode))
                    {
                        paintWrapper.Paint.Shader = shader;
                    }

                    break;
                }
                case IRadialGradientBrush radialGradient:
                {
                    var center = radialGradient.Center.ToPixels(targetSize).ToSKPoint();
                    var radius = (float)(radialGradient.Radius * targetSize.Width);

                    // TODO: There is no SetAlpha in SkiaSharp
                    //paint.setAlpha(128);

                    // would be nice to cache these shaders possibly?
                    using (var shader =
                        SKShader.CreateRadialGradient(center, radius, stopColors, stopOffsets, tileMode))
                    {
                        paintWrapper.Paint.Shader = shader;
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// Configure paint wrapper for using tile brush.
        /// </summary>
        /// <param name="paintWrapper">Paint wrapper.</param>
        /// <param name="targetSize">Target size.</param>
        /// <param name="tileBrush">Tile brush to use.</param>
        /// <param name="tileBrushImage">Tile brush image.</param>
        private void ConfigureTileBrush(ref PaintWrapper paintWrapper, Size targetSize, ITileBrush tileBrush, IDrawableBitmapImpl tileBrushImage)
        {
            var calc = new TileBrushCalculator(tileBrush, tileBrushImage.PixelSize.ToSizeWithDpi(_dpi), targetSize);
            var intermediate = CreateRenderTarget(calc.IntermediateSize);

            paintWrapper.AddDisposable(intermediate);

            using (var context = intermediate.CreateDrawingContext(null))
            {
                var sourceRect = new Rect(tileBrushImage.PixelSize.ToSizeWithDpi(96));
                var targetRect = new Rect(tileBrushImage.PixelSize.ToSizeWithDpi(_dpi));

                context.Clear(Colors.Transparent);
                context.PushClip(calc.IntermediateClip);
                context.Transform = calc.IntermediateTransform;
                context.DrawImage(
                    RefCountable.CreateUnownedNotClonable(tileBrushImage),
                    1,
                    sourceRect,
                    targetRect,
                    tileBrush.BitmapInterpolationMode);
                context.PopClip();
            }

            var tileTransform =
                tileBrush.TileMode != TileMode.None
                    ? SKMatrix.MakeTranslation(-(float)calc.DestinationRect.X, -(float)calc.DestinationRect.Y)
                    : SKMatrix.MakeIdentity();

            SKShaderTileMode tileX =
                tileBrush.TileMode == TileMode.None
                    ? SKShaderTileMode.Clamp
                    : tileBrush.TileMode == TileMode.FlipX || tileBrush.TileMode == TileMode.FlipXY
                        ? SKShaderTileMode.Mirror
                        : SKShaderTileMode.Repeat;

            SKShaderTileMode tileY =
                tileBrush.TileMode == TileMode.None
                    ? SKShaderTileMode.Clamp
                    : tileBrush.TileMode == TileMode.FlipY || tileBrush.TileMode == TileMode.FlipXY
                        ? SKShaderTileMode.Mirror
                        : SKShaderTileMode.Repeat;


            var image = intermediate.SnapshotImage();
            paintWrapper.AddDisposable(image);

            var paintTransform = default(SKMatrix);

            SKMatrix.Concat(
                ref paintTransform,
                tileTransform,
                SKMatrix.MakeScale((float)(96.0 / _dpi.X), (float)(96.0 / _dpi.Y)));

            using (var shader = image.ToShader(tileX, tileY, paintTransform))
            {
                paintWrapper.Paint.Shader = shader;
            }
        }

        /// <summary>
        /// Configure paint wrapper to use visual brush.
        /// </summary>
        /// <param name="paintWrapper">Paint wrapper.</param>
        /// <param name="visualBrush">Visual brush.</param>
        /// <param name="visualBrushRenderer">Visual brush renderer.</param>
        /// <param name="tileBrushImage">Tile brush image.</param>
        private void ConfigureVisualBrush(ref PaintWrapper paintWrapper, IVisualBrush visualBrush, IVisualBrushRenderer visualBrushRenderer, ref IDrawableBitmapImpl tileBrushImage)
        {
            if (_visualBrushRenderer == null)
            {
                throw new NotSupportedException("No IVisualBrushRenderer was supplied to DrawingContextImpl.");
            }

            var intermediateSize = visualBrushRenderer.GetRenderTargetSize(visualBrush);

            if (intermediateSize.Width >= 1 && intermediateSize.Height >= 1)
            {
                var intermediate = CreateRenderTarget(intermediateSize);

                using (var ctx = intermediate.CreateDrawingContext(visualBrushRenderer))
                {
                    ctx.Clear(Colors.Transparent);

                    visualBrushRenderer.RenderVisualBrush(ctx, visualBrush);
                }

                tileBrushImage = intermediate;
                paintWrapper.AddDisposable(tileBrushImage);
            }
        }

        /// <summary>
        /// Creates paint wrapper for given brush.
        /// </summary>
        /// <param name="brush">Source brush.</param>
        /// <param name="targetSize">Target size.</param>
        /// <returns>Paint wrapper for given brush.</returns>
        internal PaintWrapper CreatePaint(IBrush brush, Size targetSize)
        {
            var paint = new SKPaint
            {
                IsAntialias = true
            };

            var paintWrapper = new PaintWrapper(paint);

            double opacity = brush.Opacity * _currentOpacity;

            if (brush is ISolidColorBrush solid)
            {
                paint.Color = new SKColor(solid.Color.R, solid.Color.G, solid.Color.B, (byte) (solid.Color.A * opacity));

                return paintWrapper;
            }

            paint.Color = new SKColor(255, 255, 255, (byte) (255 * opacity));

            if (brush is IGradientBrush gradient)
            {
                ConfigureGradientBrush(ref paintWrapper, targetSize, gradient);

                return paintWrapper;
            }

            var tileBrush = brush as ITileBrush;
            var visualBrush = brush as IVisualBrush;
            var tileBrushImage = default(IDrawableBitmapImpl);

            if (visualBrush != null)
            {
                ConfigureVisualBrush(ref paintWrapper, visualBrush, _visualBrushRenderer, ref tileBrushImage);
            }
            else
            {
                tileBrushImage = (IDrawableBitmapImpl)(tileBrush as IImageBrush)?.Source?.PlatformImpl.Item;
            }

            if (tileBrush != null && tileBrushImage != null)
            {
                ConfigureTileBrush(ref paintWrapper, targetSize, tileBrush, tileBrushImage);
            }
            else
            {
                paint.Color = new SKColor(255, 255, 255, 0);
            }

            return paintWrapper;
        }

        /// <summary>
        /// Creates paint wrapper for given pen.
        /// </summary>
        /// <param name="pen">Source pen.</param>
        /// <param name="targetSize">Target size.</param>
        /// <returns></returns>
        private PaintWrapper CreatePaint(IPen pen, Size targetSize)
        {
            // In Skia 0 thickness means - use hairline rendering
            // and for us it means - there is nothing rendered.
            if (pen.Thickness == 0d)
            {
                return default;
            }

            var rv = CreatePaint(pen.Brush, targetSize);
            var paint = rv.Paint;

            paint.IsStroke = true;
            paint.StrokeWidth = (float) pen.Thickness;

            // Need to modify dashes due to Skia modifying their lengths
            // https://docs.microsoft.com/en-us/xamarin/xamarin-forms/user-interface/graphics/skiasharp/paths/dots
            // TODO: Still something is off, dashes are now present, but don't look the same as D2D ones.

            switch (pen.LineCap)
            {
                case PenLineCap.Round:
                    paint.StrokeCap = SKStrokeCap.Round;
                    break;
                case PenLineCap.Square:
                    paint.StrokeCap = SKStrokeCap.Square;
                    break;
                default:
                    paint.StrokeCap = SKStrokeCap.Butt;
                    break;
            }

            switch (pen.LineJoin)
            {
                case PenLineJoin.Miter:
                    paint.StrokeJoin = SKStrokeJoin.Miter;
                    break;
                case PenLineJoin.Round:
                    paint.StrokeJoin = SKStrokeJoin.Round;
                    break;
                default:
                    paint.StrokeJoin = SKStrokeJoin.Bevel;
                    break;
            }

            paint.StrokeMiter = (float) pen.MiterLimit;

            if (pen.DashStyle?.Dashes != null && pen.DashStyle.Dashes.Count > 0)
            {
                var srcDashes = pen.DashStyle.Dashes;
                var dashesArray = new float[srcDashes.Count];

                for (var i = 0; i < srcDashes.Count; ++i)
                {
                    dashesArray[i] = (float) srcDashes[i] * paint.StrokeWidth;
                }

                var offset = (float)(pen.DashStyle.Offset * pen.Thickness);

                var pe = SKPathEffect.CreateDash(dashesArray, offset);

                paint.PathEffect = pe;
                rv.AddDisposable(pe);
            }

            return rv;
        }

        /// <summary>
        /// Create new render target compatible with this drawing context.
        /// </summary>
        /// <param name="size">The size of the render target in DIPs.</param>
        /// <param name="format">Pixel format.</param>
        /// <returns></returns>
        private SurfaceRenderTarget CreateRenderTarget(Size size, PixelFormat? format = null)
        {
            var pixelSize = PixelSize.FromSizeWithDpi(size, _dpi);
            var createInfo = new SurfaceRenderTarget.CreateInfo
            {
                Width = pixelSize.Width,
                Height = pixelSize.Height,
                Dpi = _dpi,
                Format = format,
                DisableTextLcdRendering = !_canTextUseLcdRendering,
                GrContext = _grContext
            };

            return new SurfaceRenderTarget(createInfo);
        }

        /// <summary>
        /// Skia cached paint state.
        /// </summary>
        private struct PaintState : IDisposable
        {
            private readonly SKColor _color;
            private readonly SKShader _shader;
            private readonly SKPaint _paint;
            
            public PaintState(SKPaint paint, SKColor color, SKShader shader)
            {
                _paint = paint;
                _color = color;
                _shader = shader;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _paint.Color = _color;
                _paint.Shader = _shader;
            }
        }

        /// <summary>
        /// Skia paint wrapper.
        /// </summary>
        internal struct PaintWrapper : IDisposable
        {
            //We are saving memory allocations there
            public readonly SKPaint Paint;

            private IDisposable _disposable1;
            private IDisposable _disposable2;
            private IDisposable _disposable3;

            public PaintWrapper(SKPaint paint)
            {
                Paint = paint;

                _disposable1 = null;
                _disposable2 = null;
                _disposable3 = null;
            }

            public IDisposable ApplyTo(SKPaint paint)
            {
                var state = new PaintState(paint, paint.Color, paint.Shader);

                paint.Color = Paint.Color;
                paint.Shader = Paint.Shader;

                return state;
            }

            /// <summary>
            /// Add new disposable to a wrapper.
            /// </summary>
            /// <param name="disposable">Disposable to add.</param>
            public void AddDisposable(IDisposable disposable)
            {
                if (_disposable1 == null)
                {
                    _disposable1 = disposable;
                }
                else if (_disposable2 == null)
                {
                    _disposable2 = disposable;
                }
                else if (_disposable3 == null)
                {
                    _disposable3 = disposable;
                }
                else
                {
                    Debug.Assert(false);

                    // ReSharper disable once HeuristicUnreachableCode
                    throw new InvalidOperationException(
                        "PaintWrapper disposable object limit reached. You need to add extra struct fields to support more disposables.");
                }
            }
            
            /// <inheritdoc />
            public void Dispose()
            {
                Paint?.Dispose();
                _disposable1?.Dispose();
                _disposable2?.Dispose();
                _disposable3?.Dispose();
            }
        }
    }
}
