using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Perspex.Media;
using Perspex.Platform;
using Perspex.RenderHelpers;

namespace Perspex.Skia
{
    enum SkiaGeometryElementType
    {
        LineTo,
        QuadTo,
        BezierTo,
        BeginFigure,
        EndFigure
    };

    [StructLayout(LayoutKind.Sequential)]
    struct SkiaGeometryElement
    {
        public SkiaGeometryElementType Type;
        public SkiaPoint Point1, Point2, Point3;
        public bool Flag;
    }

    class SkPath : PerspexHandleHolder
    {
        public SkPath(IntPtr handle) : base(handle)
        {
        }

        protected override void Delete(IntPtr handle)
        {
            MethodTable.Instance.DisposePath(handle);
        }
    }


    class StreamGeometryImpl : IStreamGeometryImpl
    {
        RefCountable<SkPath> _path;
        RefCountable<SkPath> _transformedPath;
        private Matrix _transform = Matrix.Identity;
        
        public IntPtr EffectivePath => (_transformedPath ?? _path).Handle;

        public Rect GetRenderBounds(double strokeThickness)
        {
            // TODO: Calculate properly.
            return Bounds.Inflate(strokeThickness);
        }

        public Rect Bounds { get; private set; }

        public Matrix Transform
        {
            get { return _transform; }
            set
            {
                if(_transform == value)
                    return;
                _transform = value;
                ApplyTransform();
            }
        }

        void ApplyTransform()
        {
            if(_path == null)
                return;
            if (_transformedPath != null)
            {
                _transformedPath.Dispose();
                _transformedPath = null;
            }
            if (!_transform.IsIdentity)
                _transformedPath =
                    new RefCountable<SkPath>(new SkPath(MethodTable.Instance.TransformPath(_path.Handle, Transform)));
        }

        public IStreamGeometryImpl Clone()
        {
            return new StreamGeometryImpl {_path = _path?.Clone(), _transformedPath = _transformedPath?.Clone(), _transform = Transform, Bounds = Bounds};
        }

        public IStreamGeometryContextImpl Open()
        {
            return new StreamContext(this);
        }

        class StreamContext : IStreamGeometryContextImpl
        {
            private readonly StreamGeometryImpl _geometryImpl;
            readonly List<SkiaGeometryElement> _elements = new List<SkiaGeometryElement>();
            Point _currentPoint;
            public StreamContext(StreamGeometryImpl geometryImpl)
            {
                _geometryImpl = geometryImpl;
            }

            public void Dispose()
            {
                var arr = _elements.ToArray();
                SkRect rc;
                _geometryImpl._path?.Dispose();
                _geometryImpl._path =
                    new RefCountable<SkPath>(new SkPath(MethodTable.Instance.CreatePath(arr, arr.Length, out rc)));
                _geometryImpl.ApplyTransform();

                _geometryImpl.Bounds = rc.ToRect();

            }

            public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection)
            {
                ArcToHelper.ArcTo(this, _currentPoint, point, size, rotationAngle, isLargeArc, sweepDirection);
                _currentPoint = point;
            }

            public void BeginFigure(Point startPoint, bool isFilled)
            {
                _elements.Add(new SkiaGeometryElement
                {
                    Type = SkiaGeometryElementType.BeginFigure,
                    Point1 = new SkiaPoint(startPoint),
                    Flag = isFilled
                });
                _currentPoint = startPoint;
            }

            public void CubicBezierTo(Point point1, Point point2, Point point3)
            {
                _elements.Add(new SkiaGeometryElement
                {
                    Type = SkiaGeometryElementType.BezierTo,
                    Point1 = new SkiaPoint(point1),
                    Point2 = new SkiaPoint(point2),
                    Point3 = new SkiaPoint(point3)
                });
                _currentPoint = point3;
            }

            public void QuadraticBezierTo(Point control, Point endPoint)
            {
                _elements.Add(new SkiaGeometryElement
                {
                    Type = SkiaGeometryElementType.QuadTo,
                    Point1 = control,
                    Point2 = endPoint
                });
                _currentPoint = endPoint;
            }

            public void LineTo(Point point)
            {
                _elements.Add(new SkiaGeometryElement
                {
                    Type = SkiaGeometryElementType.LineTo,
                    Point1 = new SkiaPoint(point)
                });
                _currentPoint = point;
            }

            public void EndFigure(bool isClosed)
            {
                _elements.Add(new SkiaGeometryElement
                {
                    Type = SkiaGeometryElementType.EndFigure,
                    Flag = isClosed
                });
            }

            public void SetFillRule(FillRule fillRule)
            {
                _geometryImpl.FillRule = fillRule;
            }
        }

        public FillRule FillRule { get; set; }
    }
}
