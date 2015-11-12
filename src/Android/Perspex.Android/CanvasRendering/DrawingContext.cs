using Android.Graphics;
using Perspex.Android.Platform.CanvasPlatform;
using Perspex.Android.Platform.Specific;
using Perspex.Media;
using Perspex.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;

namespace Perspex.Android.CanvasRendering
{
    public class DrawingContextImpl : IDrawingContextImpl
    {
        private double _currentOpacity = 1.0f;

        private double CurrentOpacity
        {
            get { return _currentOpacity; }
            set
            {
                _currentOpacity = value;
            }
        }

        public Canvas _canvas;
        private Dictionary<object, IDisposable> _visualCaches;
        private Dictionary<object, IDisposable> _cache;

        public DrawingContextImpl(Canvas canvas, Dictionary<object, IDisposable> visualCaches)
        {
            _canvas = canvas;
            _visualCaches = visualCaches;
            _cache = visualCaches ?? new Dictionary<object, IDisposable>();
        }

        public void Dispose()
        {
            _canvas = null;
            if (_visualCaches == null)
            {
                DisposeCaches();
            }
        }

        private void DisposeCaches()
        {
            foreach (var d in _cache)
            {
                d.Value.Dispose();
            }
            _cache.Clear();
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
                _canvas.Matrix = value.ToAndroidGraphics();
            }
        }

        public void DrawImage(IBitmap source, double opacity, Rect sourceRect, Rect destRect)
        {
            var nativeBitmap = (source.PlatformImpl as BitmapImpl).PlatformBitmap;
            _canvas.DrawBitmap(nativeBitmap, sourceRect.ToAndroidGraphics(true), destRect.ToAndroidGraphicsF(), new Paint() { Alpha = AndroidGraphicsExtensions.OpacityToAndroidAlfa(opacity * CurrentOpacity) });
        }

        public void DrawLine(Pen pen, Point pp1, Point pp2)
        {
            var p1 = PointUnitService.Instance.PerspexToNative(pp1);
            var p2 = PointUnitService.Instance.PerspexToNative(pp2);

            var Rect = new Rect(p1.X, p1.Y, p2.X, p2.Y);
            PenImpl np;
            using ((GetPen(pen, new Size(Rect.Width, Rect.Height), out np)))
            {
                _canvas.DrawLine((float)p1.X, (float)p1.Y, (float)p2.X, (float)p2.Y, np.NativePaint);
            }
        }

        public void DrawGeometry(Brush brush, Pen pen, Geometry geometry)
        {
            var impl = geometry.PlatformImpl as StreamGeometryImpl;

            if (brush != null)
            {
                BrushImpl nb;
                using (var b = GetBrush(brush, geometry.Bounds.Size, out nb))
                {
                    _canvas.DrawPath(impl.Path, nb.NativePaint);
                }
            }

            //if the pen brush is null and thickness null on android
            //the shape is filled with black !! better not draw the stroke!!!
            if (pen?.Brush != null && pen?.Thickness > 0)
            {
                PenImpl np;
                using (var p = GetPen(pen, geometry.Bounds.Size, out np))
                {
                    _canvas.DrawPath(impl.Path, np.NativePaint);
                }
            }
        }

        public void DrawRectangle(Pen pen, Rect rect, float cornerRadius = 0)
        {
            PenImpl np;
            using (GetPen(pen, rect.Size, out np))
            {
                if (cornerRadius == 0)
                {
                    _canvas.DrawRect(rect.ToAndroidGraphicsF(), np.NativePaint);
                }
                else
                {
                    _canvas.DrawRoundRect(rect.ToAndroidGraphicsF(), cornerRadius, cornerRadius, np.NativePaint);
                }
            }
        }

        public void DrawText(Brush foreground, Point origin, FormattedText text)
        {
            if (!string.IsNullOrEmpty(text.Text))
            {
                var ap = origin.ToAndroidGraphics();
                var impl = (FormattedTextImpl)text.PlatformImpl;
                _canvas.Save();
                BrushImpl nb;
                using (var b = GetBrush(foreground, impl.Measure(), out nb))
                {
                    nb.ApplyTo(impl.TextPaint);
                    _canvas.Translate((float)ap.X, (float)ap.Y);
                    impl.TextLayout.Draw(_canvas);
                }
                _canvas.Restore();
            }
        }

        public void FillRectangle(Brush brush, Rect rect, float cornerRadius = 0)
        {
            BrushImpl np;
            using (var b = GetBrush(brush, rect.Size, out np))
            {
                var ar = rect.ToAndroidGraphicsF();
                if (cornerRadius == 0)
                {
                    _canvas.DrawRect(ar, np.NativePaint);
                }
                else
                {
                    var cr = PointUnitService.Instance.PerspexToNativeXF(cornerRadius);
                    _canvas.DrawRoundRect(ar, cr, cr, np.NativePaint);
                    //Canvas.DrawPath(RoundRectPath(rect, cornerRadius), nb.NativePaint);
                }
            }
        }

        private Rect _clip;

        public void PushClip(Rect clip)
        {
            _canvas.Save();
            _canvas.ClipRect(clip.ToAndroidGraphics());
            _clip = clip;
        }

        public void PopClip() => _canvas.Restore();

        private Stack<double> _opacityStack = new Stack<double>();

        public void PushOpacity(double opacity)
        {
            _opacityStack.Push(CurrentOpacity);
            CurrentOpacity = CurrentOpacity * opacity;
        }

        public void PopOpacity() => CurrentOpacity = _opacityStack.Pop();

        //private Path RoundRectPath(Rect rc, float radius)
        //{
        //    var x = (float)rc.TopLeft.X;
        //    var y = (float)rc.TopLeft.Y;
        //    var width = (float)rc.Width;
        //    var height = (float)rc.Height;
        //    var rx = radius;
        //    var ry = radius;
        //    var path = new Path();
        //    path.MoveTo(x + rx, y);
        //    path.LineTo(x + width - rx, y + 0);
        //    path.QuadTo(x + width, y, x + width, y + ry);
        //    path.LineTo(x + width, y + height - ry);
        //    path.QuadTo(x + width, y + height, x + width - rx, y + height);
        //    path.LineTo(x + rx, y + height);
        //    path.QuadTo(x, y + height, x, y + height - ry);
        //    path.LineTo(x, y + ry);
        //    path.QuadTo(x, y, x + rx, y);
        //    return path;
        //}

        private IDisposable GetPen(Pen pen, Size dstRect, out PenImpl result)
        {
            var penImpl = GetOrCreatePaintWrapper<PenImpl>(pen, () => new PenImpl(pen, dstRect, Paint.Style.Stroke));

            penImpl.PushOpacity(CurrentOpacity);

            result = penImpl;

            return Disposable.Create(() => penImpl.PopOpacity());
        }

        private IDisposable GetBrush(Brush brush, Size dstRect, out BrushImpl result)
        {
            var brushImpl = GetOrCreatePaintWrapper<BrushImpl>(brush, () => BrushImpl.Create(brush, dstRect, Paint.Style.Fill));

            brushImpl.PushOpacity(CurrentOpacity);

            result = brushImpl;

            return Disposable.Create(() => brushImpl.PopOpacity());
        }

        private T GetOrCreatePaintWrapper<T>(object key, Func<T> createFunc) where T : INativePaintWrapper
        {
            IDisposable cachedVisual;
            T result;

            if (!_cache.TryGetValue(key, out cachedVisual))
            {
                result = createFunc();

                _cache[key] = result;
            }
            else
            {
                result = (T)cachedVisual;
            }

            return result;
        }
    }
}