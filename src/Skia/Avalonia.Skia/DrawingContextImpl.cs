using Avalonia.Media;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Utilities;
using Avalonia.Utilities;

namespace Avalonia.Skia
{
    internal class DrawingContextImpl : IDrawingContextImpl
    {
        private readonly Vector _dpi;
        private readonly Matrix? _postTransform;
        private readonly IDisposable[] _disposables;
        private readonly IVisualBrushRenderer _visualBrushRenderer;
        private Stack<PaintWrapper> maskStack = new Stack<PaintWrapper>();
        protected bool CanUseLcdRendering = true;
        public SKCanvas Canvas { get; private set; }

        public DrawingContextImpl(
            SKCanvas canvas,
            Vector dpi,
            IVisualBrushRenderer visualBrushRenderer,
            params IDisposable[] disposables)
        {
            _dpi = dpi;
            if (dpi.X != 96 || dpi.Y != 96)
                _postTransform = Matrix.CreateScale(dpi.X / 96, dpi.Y / 96);
            _visualBrushRenderer = visualBrushRenderer;
            _disposables = disposables;
            Canvas = canvas;
            Transform = Matrix.Identity;
        }

        public void Clear(Color color)
        {
            Canvas.Clear(color.ToSKColor());
        }

        public void DrawImage(IRef<IBitmapImpl> source, double opacity, Rect sourceRect, Rect destRect)
        {
            var impl = (BitmapImpl)source.Item;
            var s = sourceRect.ToSKRect();
            var d = destRect.ToSKRect();
            using (var paint = new SKPaint()
                    { Color = new SKColor(255, 255, 255, (byte)(255 * opacity * _currentOpacity)) })
            {
                Canvas.DrawBitmap(impl.Bitmap, s, d, paint);
            }
        }

        public void DrawImage(IRef<IBitmapImpl> source, IBrush opacityMask, Rect opacityMaskRect, Rect destRect)
        {
            PushOpacityMask(opacityMask, opacityMaskRect);
            DrawImage(source, 1, new Rect(0, 0, source.Item.PixelWidth, source.Item.PixelHeight), destRect);
            PopOpacityMask();
        }

        public void DrawLine(Pen pen, Point p1, Point p2)
        {
            using (var paint = CreatePaint(pen, new Size(Math.Abs(p2.X - p1.X), Math.Abs(p2.Y - p1.Y))))
            {
                Canvas.DrawLine((float)p1.X, (float)p1.Y, (float)p2.X, (float)p2.Y, paint.Paint);
            }
        }

        public void DrawGeometry(IBrush brush, Pen pen, IGeometryImpl geometry)
        {
            var impl = (GeometryImpl)geometry;
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

            public void Dispose()
            {
                _paint.Color = _color;
                _paint.Shader = _shader;
            }
        }

        internal struct PaintWrapper : IDisposable
        {
            //We are saving memory allocations there
            //TODO: add more disposable fields if needed
            public readonly SKPaint Paint;

            private IDisposable _disposable1;
            private IDisposable _disposable2;

            public IDisposable ApplyTo(SKPaint paint)
            {
                var state = new PaintState(paint, paint.Color, paint.Shader);

                paint.Color = Paint.Color;
                paint.Shader = Paint.Shader;

                return state;
            }

            public void AddDisposable(IDisposable disposable)
            {
                if (_disposable1 == null)
                    _disposable1 = disposable;
                else if (_disposable2 == null)
                    _disposable2 = disposable;
                else
                    throw new InvalidOperationException();
            }

            public PaintWrapper(SKPaint paint)
            {
                Paint = paint;
                _disposable1 = null;
                _disposable2 = null;
            }

            public void Dispose()
            {
                Paint?.Dispose();
                _disposable1?.Dispose();
                _disposable2?.Dispose();
            }
        }

        internal PaintWrapper CreatePaint(IBrush brush, Size targetSize)
        {
            SKPaint paint = new SKPaint();
            var rv = new PaintWrapper(paint);
            paint.IsStroke = false;

            
            double opacity = brush.Opacity * _currentOpacity;
            paint.IsAntialias = true;

            var solid = brush as ISolidColorBrush;
            if (solid != null)
            {
                paint.Color = new SKColor(solid.Color.R, solid.Color.G, solid.Color.B, (byte) (solid.Color.A * opacity));
                return rv;
            }
            paint.Color = (new SKColor(255, 255, 255, (byte)(255 * opacity)));

            var gradient = brush as IGradientBrush;
            if (gradient != null)
            {
                var tileMode = gradient.SpreadMethod.ToSKShaderTileMode();
                var stopColors = gradient.GradientStops.Select(s => s.Color.ToSKColor()).ToArray();
                var stopOffsets = gradient.GradientStops.Select(s => (float)s.Offset).ToArray();

                var linearGradient = brush as ILinearGradientBrush;
                if (linearGradient != null)
                {
                    var start = linearGradient.StartPoint.ToPixels(targetSize).ToSKPoint();
                    var end = linearGradient.EndPoint.ToPixels(targetSize).ToSKPoint();

                    // would be nice to cache these shaders possibly?
                    using (var shader = SKShader.CreateLinearGradient(start, end, stopColors, stopOffsets, tileMode))
                        paint.Shader = shader;

                }
                else
                {
                    var radialGradient = brush as IRadialGradientBrush;
                    if (radialGradient != null)
                    {
                        var center = radialGradient.Center.ToPixels(targetSize).ToSKPoint();
                        var radius = (float)radialGradient.Radius;

                        // TODO: There is no SetAlpha in SkiaSharp
                        //paint.setAlpha(128);

                        // would be nice to cache these shaders possibly?
                        using (var shader = SKShader.CreateRadialGradient(center, radius, stopColors, stopOffsets, tileMode))
                            paint.Shader = shader;

                    }
                }

                return rv;
            }

            var tileBrush = brush as ITileBrush;
            var visualBrush = brush as IVisualBrush;
            var tileBrushImage = default(BitmapImpl);

            if (visualBrush != null)
            {
                if (_visualBrushRenderer != null)
                {
                    var intermediateSize = _visualBrushRenderer.GetRenderTargetSize(visualBrush);

                    if (intermediateSize.Width >= 1 && intermediateSize.Height >= 1)
                    {
                        var intermediate = new BitmapImpl((int)intermediateSize.Width, (int)intermediateSize.Height, _dpi);

                        using (var ctx = intermediate.CreateDrawingContext(_visualBrushRenderer))
                        {
                            ctx.Clear(Colors.Transparent);
                            _visualBrushRenderer.RenderVisualBrush(ctx, visualBrush);
                        }

                        tileBrushImage = intermediate;
                        rv.AddDisposable(tileBrushImage);
                    }
                }
                else
                {
                    throw new NotSupportedException("No IVisualBrushRenderer was supplied to DrawingContextImpl.");
                }
            }
            else
            {
                tileBrushImage = (BitmapImpl)((tileBrush as IImageBrush)?.Source?.PlatformImpl.Item);
            }

            if (tileBrush != null && tileBrushImage != null)
            {
                var calc = new TileBrushCalculator(tileBrush, new Size(tileBrushImage.PixelWidth, tileBrushImage.PixelHeight), targetSize);
                var bitmap = new BitmapImpl((int)calc.IntermediateSize.Width, (int)calc.IntermediateSize.Height, _dpi);
                rv.AddDisposable(bitmap);
                using (var context = bitmap.CreateDrawingContext(null))
                {
                    var rect = new Rect(0, 0, tileBrushImage.PixelWidth, tileBrushImage.PixelHeight);

                    context.Clear(Colors.Transparent);
                    context.PushClip(calc.IntermediateClip);
                    context.Transform = calc.IntermediateTransform;
                    context.DrawImage(RefCountable.CreateUnownedNotClonable(tileBrushImage), 1, rect, rect);
                    context.PopClip();
                }

                SKMatrix translation = SKMatrix.MakeTranslation(-(float)calc.DestinationRect.X, -(float)calc.DestinationRect.Y);
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
                using (var shader = SKShader.CreateBitmap(bitmap.Bitmap, tileX, tileY, translation))
                    paint.Shader = shader;
            }

            return rv;
        }

        private PaintWrapper CreatePaint(Pen pen, Size targetSize)
        {
            var rv = CreatePaint(pen.Brush, targetSize);
            var paint = rv.Paint;

            paint.IsStroke = true;
            paint.StrokeWidth = (float)pen.Thickness;

            if (pen.StartLineCap == PenLineCap.Round)
                paint.StrokeCap = SKStrokeCap.Round;
            else if (pen.StartLineCap == PenLineCap.Square)
                paint.StrokeCap = SKStrokeCap.Square;
            else
                paint.StrokeCap = SKStrokeCap.Butt;

            if (pen.LineJoin == PenLineJoin.Miter)
                paint.StrokeJoin = SKStrokeJoin.Miter;
            else if (pen.LineJoin == PenLineJoin.Round)
                paint.StrokeJoin = SKStrokeJoin.Round;
            else
                paint.StrokeJoin = SKStrokeJoin.Bevel;

            paint.StrokeMiter = (float)pen.MiterLimit;

            if (pen.DashStyle?.Dashes != null && pen.DashStyle.Dashes.Count > 0)
            {
                var pe = SKPathEffect.CreateDash(
                    pen.DashStyle?.Dashes.Select(x => (float)x).ToArray(),
                    (float)pen.DashStyle.Offset);
                paint.PathEffect = pe;
                rv.AddDisposable(pe);
            }

            return rv;
        }

        public void DrawRectangle(Pen pen, Rect rect, float cornerRadius = 0)
        {
            using (var paint = CreatePaint(pen, rect.Size))
            {
                var rc = rect.ToSKRect();
                if (cornerRadius == 0)
                {
                    Canvas.DrawRect(rc, paint.Paint);
                }
                else
                {
                    Canvas.DrawRoundRect(rc, cornerRadius, cornerRadius, paint.Paint);
                }
            }
        }

        public void FillRectangle(IBrush brush, Rect rect, float cornerRadius = 0)
        {
            using (var paint = CreatePaint(brush, rect.Size))
            {
                var rc = rect.ToSKRect();
                if (cornerRadius == 0)
                {
                    Canvas.DrawRect(rc, paint.Paint);
                }
                else
                {
                    Canvas.DrawRoundRect(rc, cornerRadius, cornerRadius, paint.Paint);
                }
            }
        }

        public void DrawText(IBrush foreground, Point origin, IFormattedTextImpl text)
        {
            using (var paint = CreatePaint(foreground, text.Size))
            {
                var textImpl = (FormattedTextImpl)text;
                textImpl.Draw(this, Canvas, origin.ToSKPoint(), paint, CanUseLcdRendering);
            }
        }

        public IRenderTargetBitmapImpl CreateLayer(Size size)
        {
            var pixelSize = size * (_dpi / 96);
            return new BitmapImpl((int)pixelSize.Width, (int)pixelSize.Height, _dpi);
        }

        public void PushClip(Rect clip)
        {
            Canvas.Save();
            Canvas.ClipRect(clip.ToSKRect());
        }

        public void PopClip()
        {
            Canvas.Restore();
        }

        private double _currentOpacity = 1.0f;
        private readonly Stack<double> _opacityStack = new Stack<double>();

        public void PushOpacity(double opacity)
        {
            _opacityStack.Push(_currentOpacity);
            _currentOpacity *= opacity;
        }

        public void PopOpacity()
        {
            _currentOpacity = _opacityStack.Pop();
        }

        public virtual void Dispose()
        {
            if(_disposables!=null)
                foreach (var disposable in _disposables)
                    disposable?.Dispose();
        }

        public void PushGeometryClip(IGeometryImpl clip)
        {
            Canvas.Save();
            Canvas.ClipPath(((StreamGeometryImpl)clip).EffectivePath);
        }

        public void PopGeometryClip()
        {
            Canvas.Restore();
        }

        public void PushOpacityMask(IBrush mask, Rect bounds)
        {
            Canvas.SaveLayer(new SKPaint());
            maskStack.Push(CreatePaint(mask, bounds.Size));
        }

        public void PopOpacityMask()
        {
            Canvas.SaveLayer(new SKPaint { BlendMode = SKBlendMode.DstIn });
            using (var paintWrapper = maskStack.Pop())
            {
                Canvas.DrawPaint(paintWrapper.Paint);
            }
            Canvas.Restore();
            Canvas.Restore();
        }

        private Matrix _currentTransform;

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
                    transform *= _postTransform.Value;
                Canvas.SetMatrix(transform.ToSKMatrix());
            }
        }
    }
}