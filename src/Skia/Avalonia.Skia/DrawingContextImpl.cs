using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.RenderHelpers;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Skia
{
    internal class DrawingContextImpl : IDrawingContextImpl
    {
        private readonly Matrix? _postTransform;
        private readonly IDisposable[] _disposables;
        private Stack<PaintWrapper> maskStack = new Stack<PaintWrapper>();
        
        public SKCanvas Canvas { get; private set; }

        public DrawingContextImpl(SKCanvas canvas, Matrix? postTransform = null, params IDisposable[] disposables)
        {
            if (_postTransform.HasValue && !_postTransform.Value.IsIdentity)
                _postTransform = postTransform;
            _disposables = disposables;
            Canvas = canvas;
            Canvas.Clear();
        }

        public void DrawImage(IBitmap source, double opacity, Rect sourceRect, Rect destRect)
        {
            var impl = (BitmapImpl)source.PlatformImpl;
            var s = sourceRect.ToSKRect();
            var d = destRect.ToSKRect();
            using (var paint = new SKPaint()
                    { Color = new SKColor(255, 255, 255, (byte)(255 * opacity)) })
            {
                Canvas.DrawBitmap(impl.Bitmap, s, d, paint);
            }
        }

        public void DrawLine(Pen pen, Point p1, Point p2)
        {
            using (var paint = CreatePaint(pen, new Size(Math.Abs(p2.X - p1.X), Math.Abs(p2.Y - p1.Y))))
            {
                Canvas.DrawLine((float)p1.X, (float)p1.Y, (float)p2.X, (float)p2.Y, paint.Paint);
            }
        }

        public void DrawGeometry(IBrush brush, Pen pen, Geometry geometry)
        {
            var impl = ((StreamGeometryImpl)geometry.PlatformImpl);
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
                else
                    throw new InvalidOperationException();
            }

            public PaintWrapper(SKPaint paint)
            {
                Paint = paint;
                _disposable1 = null;
            }

            public void Dispose()
            {
                Paint?.Dispose();
                _disposable1?.Dispose();
            }
        }

        internal PaintWrapper CreatePaint(IBrush brush, Size targetSize)
        {
            SKPaint paint = new SKPaint();
            var rv = new PaintWrapper(paint);
            paint.IsStroke = false;

            // TODO: SkiaSharp does not contain alpha yet!
            double opacity = brush.Opacity * _currentOpacity;
            //paint.SetAlpha(paint.GetAlpha() * opacity);
            paint.IsAntialias = true;

            SKColor color = new SKColor(255, 255, 255, 255);

            var solid = brush as ISolidColorBrush;
            if (solid != null)
                color = solid.Color.ToSKColor();

            paint.Color = (new SKColor(color.Red, color.Green, color.Blue, (byte)(color.Alpha * opacity)));

            if (solid != null)
            {
                return rv;
            }

            var gradient = brush as GradientBrush;
            if (gradient != null)
            {
                var tileMode = gradient.SpreadMethod.ToSKShaderTileMode();
                var stopColors = gradient.GradientStops.Select(s => s.Color.ToSKColor()).ToArray();
                var stopOffsets = gradient.GradientStops.Select(s => (float)s.Offset).ToArray();

                var linearGradient = brush as LinearGradientBrush;
                if (linearGradient != null)
                {
                    var start = linearGradient.StartPoint.ToPixels(targetSize).ToSKPoint();
                    var end = linearGradient.EndPoint.ToPixels(targetSize).ToSKPoint();

                    // would be nice to cache these shaders possibly?
                    var shader = SKShader.CreateLinearGradient(start, end, stopColors, stopOffsets, tileMode);
                    paint.Shader = shader;
                    shader.Dispose();
                }
                else
                {
                    var radialGradient = brush as RadialGradientBrush;
                    if (radialGradient != null)
                    {
                        var center = radialGradient.Center.ToPixels(targetSize).ToSKPoint();
                        var radius = (float)radialGradient.Radius;

                        // TODO: There is no SetAlpha in SkiaSharp
                        //paint.setAlpha(128);

                        // would be nice to cache these shaders possibly?
                        var shader = SKShader.CreateRadialGradient(center, radius, stopColors, stopOffsets, tileMode);
                        paint.Shader = shader;
                        shader.Dispose();
                    }
                }

                return rv;
            }

            var tileBrush = brush as TileBrush;
            if (tileBrush != null)
            {
                var helper = new TileBrushImplHelper(tileBrush, targetSize);
                var bitmap = new BitmapImpl((int)helper.IntermediateSize.Width, (int)helper.IntermediateSize.Height);
                rv.AddDisposable(bitmap);
                using (var ctx = bitmap.CreateDrawingContext())
                    helper.DrawIntermediate(ctx);
                SKMatrix translation = SKMatrix.MakeTranslation(-(float)helper.DestinationRect.X, -(float)helper.DestinationRect.Y);
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
                paint.Shader = SKShader.CreateBitmap(bitmap.Bitmap, tileX, tileY, translation);
                paint.Shader.Dispose();
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
                paint.StrokeJoin = SKStrokeJoin.Mitter;
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

        public void DrawText(IBrush foreground, Point origin, FormattedText text)
        {
            using (var paint = CreatePaint(foreground, text.Measure()))
            {
                var textImpl = text.PlatformImpl as FormattedTextImpl;
                textImpl.Draw(this, Canvas, origin.ToSKPoint(), paint);
            }
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

        public void PushGeometryClip(Geometry clip)
        {
            Canvas.Save();
            Canvas.ClipPath(((StreamGeometryImpl)clip.PlatformImpl).EffectivePath);
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
            Canvas.SaveLayer(new SKPaint { XferMode = SKXferMode.DstIn });
            using (var paintWrapper = maskStack.Pop())
            {
                Canvas.DrawPaint(paintWrapper.Paint);
            }
            Canvas.Restore();
            Canvas.Restore();
        }

        private Matrix _currentTransform = Matrix.Identity;

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