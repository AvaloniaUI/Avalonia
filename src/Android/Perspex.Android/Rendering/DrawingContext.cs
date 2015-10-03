using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Widget;
using Perspex.Controls.Shapes;
using Perspex.Media;
using Perspex.Media.Imaging;
using ARect = Android.Graphics.Rect;
using Color = Perspex.Media.Color;
using Path = Android.Graphics.Path;

namespace Perspex.Android.Rendering
{
    public class DrawingContext : IDrawingContext
    {
        public Canvas Canvas;
        private TextPaint _nativebrush;

        public DrawingContext()
        {
            Canvas = PerspexActivity.Instance.Canvas;
            _nativebrush = new TextPaint {AntiAlias = true};
        }

        public void Dispose()
        {
            Canvas = null;
            _nativebrush = null;
        }

        public Matrix CurrentTransform => Canvas.Matrix.ToPerspex();

        public void DrawImage(IBitmap source, double opacity, Rect sourceRect, Rect destRect)
        {
            throw new NotImplementedException();
        }

        public void DrawLine(Pen pen, Point p1, Point p2)
        {
            var Rect = new Rect(p1.X, p1.Y, p2.X, p2.Y);
            using ((SetPen(pen, new Size(Rect.Width, Rect.Height))))
            {
                Canvas.DrawLine((float) p1.X, (float) p1.Y, (float) p2.X, (float) p2.Y, _nativebrush);   
            }
        }

        public void DrawGeometry(Brush brush, Pen pen, Geometry geometry)
        {
            var impl = geometry.PlatformImpl as StreamGeometryImpl;

            if (brush != null)
            {
                using (var b = SetBrush(brush, geometry.Bounds.Size, BrushUsage.Fill))
                {
                    Canvas.DrawPath(impl.Path, _nativebrush);
                }
            }

            if (pen != null)
            {
                using (var p = SetPen(pen, geometry.Bounds.Size))
                {
                    Canvas.DrawPath(impl.Path, _nativebrush);
                }
            }
        }

        public void DrawRectangle(Pen pen, Rect rect, float cornerRadius = 0)
        {
            using (SetPen(pen, rect.Size))
            {
                if (cornerRadius == 0)
                {
                    Canvas.DrawRect(rect.ToAndroidGraphicsF(), _nativebrush);
                }
                else
                {
                    Canvas.DrawPath(RoundRectPath(rect, cornerRadius), _nativebrush);
                }
            }
        }

        private Path RoundRectPath(Rect rc, float radius)
        {
            var x =(float) rc.TopLeft.X;
            var y = (float)rc.TopLeft.Y;
            var width = (float)rc.Width;
            var height = (float) rc.Height;
            var rx = radius;
            var ry = radius;
            var path = new Path();
            path.MoveTo(x + rx, y);
            path.LineTo(x + width - rx, y + 0);
            path.QuadTo(x + width, y, x + width, y + ry);
            path.LineTo(x + width, y + height - ry);
            path.QuadTo(x + width, y + height, x + width - rx, y + height);
            path.LineTo(x + rx, y + height);
            path.QuadTo(x, y + height, x, y + height - ry);
            path.LineTo(x, y + ry);
            path.QuadTo(x, y, x + rx, y);
            return path;
        }

        public void DrawText(Brush foreground, Point origin, FormattedText text)
        {
            var impl = text.PlatformImpl as FormattedTextImpl;

            using (SetBrush(foreground, new Size(0, 0), BrushUsage.Stroke))
            {
                Canvas.Save();
                Canvas.Translate(0, (float)impl.Constraint.Height);
                var path = new Path();
                var p = new Rect(origin, new Size(0, 0)); 
                path.AddRect(p.ToAndroidGraphicsF(), Path.Direction.Ccw);
                impl.TextFormatting.Color = _nativebrush.Color;
                Canvas.DrawTextOnPath(impl.String, path, 0, 0, impl.TextFormatting);
                Canvas.DrawPath(path, impl.TextFormatting);
                Canvas.Restore();
            }
        }

        public void FillRectangle(Brush brush, Rect rect, float cornerRadius = 0)
        {
            using (var b = SetBrush(brush, rect.Size, BrushUsage.Fill))
            {
                if (cornerRadius == 0)
                {
                    Canvas.DrawRect(rect.ToAndroidGraphics(), _nativebrush);
                }
                else
                {
                    Canvas.DrawPath(RoundRectPath(rect, cornerRadius), _nativebrush);
                }
            }
        }

        public IDisposable PushClip(Rect clip)
        {
            Canvas.Save();
            Canvas.ClipBounds.Set(clip.ToAndroidGraphics());
            return Disposable.Create(() => Canvas.Restore());
        }

        private float _currentOpacity = 1.0f;
        public IDisposable PushOpacity(double opacity)
        {
            var previous = _currentOpacity;
            _currentOpacity = (float) opacity;
            _nativebrush.Alpha = (int)_currentOpacity;

            return Disposable.Create(() =>
            {
                _currentOpacity = previous;
                _nativebrush.Alpha = (int) _currentOpacity;
            });
        }

        public IDisposable PushTransform(Matrix matrix)
        {
            Canvas.Matrix.SetConcat(Canvas.Matrix, matrix.ToAndroidGraphics());

            return Disposable.Create(() =>
            {
                Canvas.Matrix.PostConcat(matrix.Invert().ToAndroidGraphics());
            });
        }

        private IDisposable SetPen(Pen pen, Size dstRect)
        {
            if (pen.DashStyle?.Dashes != null && pen.DashStyle.Dashes.Count > 0)
            {
                var cray = pen.DashStyle.Dashes.Select(d => (float) d).ToArray();
                _nativebrush.SetPathEffect(new DashPathEffect(cray, (float)pen.DashStyle.Offset));
            }

            _nativebrush.StrokeWidth = (float) pen.Thickness;
            _nativebrush.StrokeMiter = (float) pen.MiterLimit;

            _nativebrush.StrokeJoin = pen.LineJoin.ToAndroidGraphics();
            _nativebrush.StrokeCap = pen.StartLineCap.ToAndroidGraphics();

            if (pen.Brush == null)
                return Disposable.Empty;

            return SetBrush(pen.Brush, dstRect, BrushUsage.Stroke);
        }


        private static BrushImpl FallbackBrush = new SolidColorBrushImpl(new SolidColorBrush(Colors.Magenta));

        private IDisposable SetBrush(Brush brush, Size dstRect, BrushUsage usage)
        {
            var solid = brush as SolidColorBrush;
            var linearGradientBrush = brush as LinearGradientBrush;
            var radialGradientBrush = brush as RadialGradientBrush;
            var imageBrush = brush as ImageBrush;
            var visualBrush = brush as VisualBrush;
            BrushImpl impl = null;

            if (solid != null)
            {
                impl = new SolidColorBrushImpl(solid);
            }
            else if (linearGradientBrush != null)
            {
                // TODO: Implement me
                Log.Debug("REND", "LinearGradientBrush not implemented");
                impl = FallbackBrush;
            }
            else if (radialGradientBrush != null)
            {
                // TODO: Implement me
                Log.Debug("REND", "RadialGradientBrush not implemented");
                impl = FallbackBrush;
            }
            else if (imageBrush != null)
            {
                // TODO: Implement me
                Log.Debug("REND", "ImageBrush not implemented");
                impl = FallbackBrush;
            }
            else if (visualBrush != null)
            {
                // TODO: Implement me
                Log.Debug("REND", "VisualBrush not implemented");
                impl = FallbackBrush;
            }
            else
            {
                impl = new SolidColorBrushImpl(null);
            }

            impl.Apply(_nativebrush, usage);

            return Disposable.Create(() =>
            {
                impl.Dispose();
                _nativebrush = new TextPaint { AntiAlias = true };
            });
        }
    }
}