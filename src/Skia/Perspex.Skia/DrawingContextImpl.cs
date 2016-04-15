using System;
using System.Collections.Generic;
using Perspex.Media;
using Perspex.Media.Imaging;
using Perspex.RenderHelpers;
using SkiaSharp;
using System.Linq;

namespace Perspex.Skia
{
    unsafe class DrawingContextImpl : IDrawingContextImpl
    {
        public SKCanvas Canvas { get; private set; }

        public DrawingContextImpl(SKCanvas canvas)
        {
            Canvas = canvas;
        }

        public void DrawImage(IBitmap source, double opacity, Rect sourceRect, Rect destRect)
        {
            var impl = (BitmapImpl)source.PlatformImpl;
            var s = sourceRect.ToSKRect();
            var d = destRect.ToSKRect();
            Canvas.DrawBitmap(impl.Bitmap, s, d);
        }

        public void DrawLine(Pen pen, Point p1, Point p2)
        {
            using (var paint = CreatePaint(pen, new Size(Math.Abs(p2.X - p1.X), Math.Abs(p2.Y - p1.Y))))
            {
                Canvas.DrawLine((float)p1.X, (float)p1.Y, (float)p2.X, (float)p2.Y, paint);
            }
        }

        public void DrawGeometry(IBrush brush, Pen pen, Geometry geometry)
        {
            var impl = ((StreamGeometryImpl)geometry.PlatformImpl);
            var size = geometry.Bounds.Size;

            using (var fill = brush != null ? CreatePaint(brush, size) : null)
            using (var stroke = pen?.Brush != null ? CreatePaint(pen, size) : null)
            {
                if (fill != null)
                {
                    Canvas.DrawPath(impl.EffectivePath, fill);
                }
                if (stroke != null)
                {
                    Canvas.DrawPath(impl.EffectivePath, stroke);
                }
            }
        }

        private SKPaint CreatePaint(IBrush brush, Size targetSize)
        {
            SKPaint paint = new SKPaint();

            paint.IsStroke = false;

            // TODO: SkiaSharp does not contain alpha yet!
            //double opacity = brush.Opacity * _currentOpacity;
            //paint.SetAlpha(paint.GetAlpha() * opacity);
            paint.IsAntialias = true;

            var solid = brush as SolidColorBrush;
            if (solid != null)
            {
                paint.Color = solid.Color.ToSKColor();
                return paint;
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
                    }
                }

                return paint;
            }

            var tileBrush = brush as TileBrush;
            if (tileBrush != null)
            {
                // TODO: Get Tile Brushes working!!!
                //
                //throw new NotImplementedException();

                //	rv.Brush->Type = NativeBrushType.Image;
                //	var helper = new TileBrushImplHelper(tileBrush, targetSize);
                //	var bitmap = new BitmapImpl((int)helper.IntermediateSize.Width, (int)helper.IntermediateSize.Height);
                //	rv.AddDisposable(bitmap);
                //	using (var ctx = bitmap.CreateDrawingContext())
                //		helper.DrawIntermediate(ctx);
                //	rv.Brush->Bitmap = bitmap.Handle;
                //	rv.Brush->BitmapTileMode = tileBrush.TileMode;
                //	rv.Brush->BitmapTranslation = new SkiaPoint(-helper.DestinationRect.X, -helper.DestinationRect.Y);

                //	SkMatrix matrix;
                //	matrix.setTranslate(brush->BitmapTranslation);
                //	SkShader::TileMode tileX = brush->BitmapTileMode == ptmNone ? SkShader::kClamp_TileMode
                //		: (brush->BitmapTileMode == ptmFlipX || brush->BitmapTileMode == ptmFlipXY) ? SkShader::kMirror_TileMode : SkShader::kRepeat_TileMode;
                //	SkShader::TileMode tileY = brush->BitmapTileMode == ptmNone ? SkShader::kClamp_TileMode
                //		: (brush->BitmapTileMode == ptmFlipY || brush->BitmapTileMode == ptmFlipXY) ? SkShader::kMirror_TileMode : SkShader::kRepeat_TileMode;

                //	paint.setShader(SkShader::CreateBitmapShader(brush->Bitmap->Bitmap, tileX, tileY, &matrix))->unref();
            }

            return paint;
        }

        private SKPaint CreatePaint(Pen pen, Size targetSize)
        {
            var paint = CreatePaint(pen.Brush, targetSize);

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

            //if (pen.DashStyle?.Dashes != null)
            //{
            //	var dashes = pen.DashStyle.Dashes;
            //	if (dashes.Count > NativeBrush.MaxDashCount)
            //		throw new NotSupportedException("Maximum supported dash count is " + NativeBrush.MaxDashCount);
            //	brush.Brush->StrokeDashCount = dashes.Count;
            //	for (int c = 0; c < dashes.Count; c++)
            //		brush.Brush->StrokeDashes[c] = (float)dashes[c];
            //	brush.Brush->StrokeDashOffset = (float)pen.DashStyle.Offset;

            //}

            //if (brush->StrokeDashCount != 0)
            //{
            //	paint.setPathEffect(SkDashPathEffect::Create(brush->StrokeDashes, brush->StrokeDashCount, brush->StrokeDashOffset))->unref();
            //}

            return paint;
        }

        public void DrawRectangle(Pen pen, Rect rect, float cornerRadius = 0)
        {
            using (var paint = CreatePaint(pen, rect.Size))
            {
                var rc = rect.ToSKRect();
                if (cornerRadius == 0)
                {
                    Canvas.DrawRect(rc, paint);
                }
                else
                {
                    // DrawRRect is not accesible in SkiaSharp yet. We should add that
                    // to SkiaSharp and initiate a PR....
                    Canvas.DrawRect(rc, paint);
                    //Canvas.DrawRoundedRect(rc, cornerRadius, cornerRadius, paint);
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
                    Canvas.DrawRect(rc, paint);
                }
                else
                {
                    // this does not appear to exist in SkiaSharp?
                    //throw new NotImplementedException();
                    //Canvas.DrawRoundedRect(rc, cornerRadius, cornerRadius, paint);
                    Canvas.DrawRect(rc, paint);
                }
            }
        }

        public void DrawText(IBrush foreground, Point origin, FormattedText text)
        {
            using (var paint = CreatePaint(foreground, text.Measure()))
            {
                var textImpl = text.PlatformImpl as FormattedTextImpl;
                textImpl.Draw(this.Canvas, origin.ToSKPoint());
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

        double _currentOpacity = 1.0f;
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
                Canvas.SetMatrix(value.ToSKMatrix());
            }
        }
    }

    // not sure we need this yet
    internal class WindowDrawingContextImpl : DrawingContextImpl
    {
        WindowRenderTarget _target;

        public WindowDrawingContextImpl(WindowRenderTarget target)
            : base(target.Surface.Canvas)
        {
            _target = target;
        }

        public override void Dispose()
        {
            base.Dispose();
            _target.Present();
        }
    }
}