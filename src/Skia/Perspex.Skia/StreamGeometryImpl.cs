using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Perspex.Media;
using Perspex.Platform;

namespace Perspex.Skia
{
    enum SkiaGeometryElementType
    {
        LineTo,
        ArcTo,
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
        public SkPath Path;

        public Rect GetRenderBounds(double strokeThickness)
        {
            // TODO: Calculate properly.
            return Bounds.Inflate(strokeThickness);
        }

        public Rect Bounds { get; private set; }

        public Matrix Transform { get; set; } = Matrix.Identity;

        public IStreamGeometryImpl Clone()
        {
            return new StreamGeometryImpl() {Path = Path, Transform = Transform, Bounds = Bounds};
        }

        public IStreamGeometryContextImpl Open()
        {
            return new StreamContext(this);
        }

        class StreamContext : IStreamGeometryContextImpl
        {
            private readonly StreamGeometryImpl _geometryImpl;
            readonly List<SkiaGeometryElement> _elements = new List<SkiaGeometryElement>();

            public StreamContext(StreamGeometryImpl geometryImpl)
            {
                _geometryImpl = geometryImpl;
            }

            public void Dispose()
            {
                var arr = _elements.ToArray();
                SkRect rc;
                _geometryImpl.Path = new SkPath(MethodTable.Instance.CreatePath(arr, arr.Length, out rc));
                _geometryImpl.Bounds = rc.ToRect();

            }

            public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection)
            {
                //TODO: Implement me

            }

            public void BeginFigure(Point startPoint, bool isFilled)
            {
                _elements.Add(new SkiaGeometryElement
                {
                    Type = SkiaGeometryElementType.BeginFigure,
                    Point1 = new SkiaPoint(startPoint),
                    Flag = isFilled
                });
            }

            public void BezierTo(Point point1, Point point2, Point point3)
            {
                _elements.Add(new SkiaGeometryElement
                {
                    Type = SkiaGeometryElementType.BezierTo,
                    Point1 = new SkiaPoint(point1),
                    Point2 = new SkiaPoint(point2),
                    Point3 = new SkiaPoint(point3)
                });
            }

            public void QuadTo(Point control, Point endPoint)
            {
                //throw new NotImplementedException();
            }

            public void LineTo(Point point)
            {
                _elements.Add(new SkiaGeometryElement
                {
                    Type = SkiaGeometryElementType.LineTo,
                    Point1 = new SkiaPoint(point)
                });
            }

            public void EndFigure(bool isClosed)
            {
                _elements.Add(new SkiaGeometryElement
                {
                    Type = SkiaGeometryElementType.EndFigure,
                    Flag = isClosed
                });
            }
        }
    }
}
