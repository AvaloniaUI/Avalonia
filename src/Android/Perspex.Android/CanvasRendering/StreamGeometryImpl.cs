using Android.Graphics;
using Perspex.Media;
using Perspex.Platform;
using System;
using System.Linq;
using APath = Android.Graphics.Path;
using ARect = Android.Graphics.RectF;

namespace Perspex.Android.CanvasRendering
{
    public class StreamGeometryImpl : IStreamGeometryImpl
    {
        private readonly StreamGeometryContextImpl _impl;
        private Matrix _transform = Matrix.Identity;

        public StreamGeometryImpl()
        {
            _impl = new StreamGeometryContextImpl(null);
        }

        public StreamGeometryImpl(StreamGeometryContextImpl impl)
        {
            _impl = impl;
        }

        public APath Path => _impl.Path;

        public Rect Bounds => _impl.CalculatedBounds;

        public Matrix Transform
        {
            get { return _transform; }
            set
            {
                if (value == Transform) return;
                if (!value.IsIdentity)
                {
                    _transform = value;

                    Path.Transform(_transform.ToAndroidGraphics());
                }
            }
        }

        public Rect GetRenderBounds(double strokeThickness)
        {
            return Bounds.Inflate(strokeThickness);
        }

        public IStreamGeometryImpl Clone()
        {
            return new StreamGeometryImpl(_impl);
        }

        public IStreamGeometryContextImpl Open()
        {
            return _impl;
        }
    }

    public class StreamGeometryContextImpl : IStreamGeometryContextImpl
    {
        private double _top = double.MaxValue;
        private double _left = double.MaxValue;
        private double _bottom = 1;
        private double _right = 1;

        public StreamGeometryContextImpl(APath path = null)
        {
            Path = path != null ? new APath(path) : new APath();
            Path.AddRect(new ARect(), APath.Direction.Cw);
        }

        public APath Path { get; }

        public Rect PathBounds
        {
            get
            {
                var _bounds = new ARect();

                Path.ComputeBounds(_bounds, true);
                return _bounds.ToPerspex();
            }
        }

        public Rect CalculatedBounds
        {
            get
            {
                return new Rect(_left, _top, _right - _left, _bottom - _top);
            }
        }

        public void Dispose()
        {
        }

        private void ProcessBounds(params Point[] points)
        {
            _top = Math.Min(_top, points.Min(p => p.Y));
            _left = Math.Min(_left, points.Min(p => p.X));
            _bottom = Math.Max(_bottom, points.Max(p => p.Y));
            _right = Math.Max(_right, points.Max(p => p.X));
        }

        public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection)
        {
            ProcessBounds(point);

            Path.ArcTo(new Rect(point, size).ToAndroidGraphicsF(), 0, (float)rotationAngle);
        }

        public void BeginFigure(Point pstartPoint, bool isFilled)
        {
            ProcessBounds(pstartPoint);
            var sp = pstartPoint.ToAndroidGraphics();

            Path.MoveTo(sp.X, sp.Y);
        }

        public void BezierTo(Point ppoint1, Point ppoint2, Point ppoint3)
        {
            ProcessBounds(ppoint1, ppoint2, ppoint3);
            var point1 = ppoint1.ToAndroidGraphics();
            var point2 = ppoint2.ToAndroidGraphics();
            var point3 = ppoint3.ToAndroidGraphics();

            Path.CubicTo(point1.X, point1.Y,
                            point2.X, point2.Y,
                            point3.X, point3.Y);
        }

        public void LineTo(Point ppoint)
        {
            ProcessBounds(ppoint);
            var point = ppoint.ToAndroidGraphics();

            Path.LineTo(point.X, point.Y);
        }

        public void EndFigure(bool isClosed)
        {
            if (Path != null)
                if (isClosed)
                    Path.Close();
        }

        public void QuadTo(Point ppoint1, Point ppoint2)
        {
            ProcessBounds(ppoint1, ppoint2);
            var point1 = ppoint1.ToAndroidGraphics();
            var point2 = ppoint2.ToAndroidGraphics();

            Path.QuadTo(point1.X, point1.Y, point2.X, point2.Y);
        }
    }
}