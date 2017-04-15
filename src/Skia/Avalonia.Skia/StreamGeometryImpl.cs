using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.RenderHelpers;
using SkiaSharp;

namespace Avalonia.Skia
{
    class StreamGeometryImpl : IStreamGeometryImpl
    {
        SKPath _path;

        private Matrix _transform = Matrix.Identity;

        public SKPath EffectivePath => _path;

        public Rect GetRenderBounds(double strokeThickness)
        {
            // TODO: Calculate properly.
            return Bounds.TransformToAABB(Transform).Inflate(strokeThickness);
        }

        public Rect Bounds { get; private set; }

        public Matrix Transform
        {
            get { return _transform; }
        }

        public IStreamGeometryImpl Clone()
        {
            return new StreamGeometryImpl
            {
                _path = _path?.Clone(),
                _transform = Transform,
                Bounds = Bounds
            };
        }

        public IStreamGeometryContextImpl Open()
        {
            _path = new SKPath();
            _path.FillType = SKPathFillType.EvenOdd;

            return new StreamContext(this);
        }

        public bool FillContains(Point point)
        {
            // TODO: Not supported by SkiaSharp yet, so use expanded Rect
            // return EffectivePath.Contains(point.X, point.Y);
            return GetRenderBounds(0).Contains(point);
        }

        public bool StrokeContains(Pen pen, Point point)
        {
            // TODO: Not supported by SkiaSharp yet, so use expanded Rect
            // return EffectivePath.Contains(point.X, point.Y);
            return GetRenderBounds(0).Contains(point);
        }

        public IGeometryImpl Intersect(IGeometryImpl geometry)
        {
            throw new NotImplementedException();
        }

        public IGeometryImpl WithTransform(Matrix transform)
        {
            var result = (StreamGeometryImpl)Clone();

            if (result.Transform != Matrix.Identity)
            {
                result._path.Transform(result.Transform.Invert().ToSKMatrix());
            }

            if (transform != Matrix.Identity)
            {
                result._path.Transform(transform.ToSKMatrix());
            }

            result._transform = transform;
            return result;
        }

        class StreamContext : IStreamGeometryContextImpl
        {
            private readonly StreamGeometryImpl _geometryImpl;
            private SKPath _path;

            Point _currentPoint;
            public StreamContext(StreamGeometryImpl geometryImpl)
            {
                _geometryImpl = geometryImpl;
                _path = _geometryImpl._path;
            }

            public void Dispose()
            {
                SKRect rc;
                _path.GetBounds(out rc);
                _geometryImpl.Bounds = rc.ToAvaloniaRect();
            }

            public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection)
            {
                _path.ArcTo(
                    (float)size.Width,
                    (float)size.Height,
                    (float)rotationAngle,
                    isLargeArc ? SKPathArcSize.Large : SKPathArcSize.Small,
                    sweepDirection == SweepDirection.Clockwise ? SKPathDirection.Clockwise : SKPathDirection.CounterClockwise,
                    (float)point.X,
                    (float)point.Y);
                _currentPoint = point;
            }

            public void BeginFigure(Point startPoint, bool isFilled)
            {
                _path.MoveTo((float)startPoint.X, (float)startPoint.Y);
                _currentPoint = startPoint;
            }

            public void CubicBezierTo(Point point1, Point point2, Point point3)
            {
                _path.CubicTo((float)point1.X, (float)point1.Y, (float)point2.X, (float)point2.Y, (float)point3.X, (float)point3.Y);
                _currentPoint = point3;
            }

            public void QuadraticBezierTo(Point point1, Point point2)
            {
                _path.QuadTo((float)point1.X, (float)point1.Y, (float)point2.X, (float)point2.Y);
                _currentPoint = point2;
            }

            public void LineTo(Point point)
            {
                _path.LineTo((float)point.X, (float)point.Y);
                _currentPoint = point;
            }

            public void EndFigure(bool isClosed)
            {
                if (isClosed)
                {
                    _path.Close();
                }
            }

            public void SetFillRule(FillRule fillRule)
            {
                _path.FillType = fillRule == FillRule.EvenOdd ? SKPathFillType.EvenOdd : SKPathFillType.Winding;
            }
        }
    }
}
