using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Perspex.Media;
using Perspex.Platform;
using Perspex.RenderHelpers;
using SkiaSharp;

namespace Perspex.Skia
{
    class StreamGeometryImpl : IStreamGeometryImpl
    {
        SKPath _path;
        SKPath _transformedPath;

        private Matrix _transform = Matrix.Identity;

        public SKPath EffectivePath => (_transformedPath ?? _path);

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
                if (_transform == value)
                    return;

                _transform = value;
                ApplyTransform();
            }
        }

        void ApplyTransform()
        {
            if (_path == null)
                return;

            if (_transformedPath != null)
            {
                _transformedPath.Dispose();
                _transformedPath = null;
            }

            // TODO: SkiaSharp does not expose Transform yet!!!
            //if (!Transform.IsIdentity)
            //{
            //	_transformedPath = new SKPath();
            //	_path.Transform(Transform.ToSKMatrix(), _transformedPath);
            //}
        }

        public IStreamGeometryImpl Clone()
        {
            // TODO: there is no SKPath.Clone yet!!!!!!!!!!!!!
            //
            //return new StreamGeometryImpl
            //{
            //	_path = _path?.Clone(),
            //	_transformedPath = _transformedPath?.Clone(),
            //	_transform = Transform,
            //	Bounds = Bounds
            //};

            return this;        // this will probably end bad!!
        }

        public IStreamGeometryContextImpl Open()
        {
            _path = new SKPath();
            return new StreamContext(this);
        }

        class StreamContext : IStreamGeometryContextImpl
        {
            private readonly StreamGeometryImpl _geometryImpl;
            private SKPath _path;

            //readonly List<SkiaGeometryElement> _elements = new List<SkiaGeometryElement>();
            Point _currentPoint;
            public StreamContext(StreamGeometryImpl geometryImpl)
            {
                _geometryImpl = geometryImpl;
                _path = _geometryImpl._path;
            }

            public void Dispose()
            {
                // Not sure what we need to do here....

                //	//throw new NotImplementedException();

                //	var arr = _elements.ToArray();
                //             SkRect rc;
                //             _path?.Dispose();
                //             _path = new SKPath(new SkPath(MethodTable.Instance.CreatePath(arr, arr.Length, out rc)));
                //             _geometryImpl.ApplyTransform();

                //             _geometryImpl.Bounds = rc.ToRect();

                SKRect rc;
                _path.GetBounds(out rc);
                _geometryImpl.ApplyTransform();
                _geometryImpl.Bounds = rc.ToPerspexRect();
            }

            public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection)
            {
                throw new NotImplementedException();

                //ArcToHelper.ArcTo(this, _currentPoint, point, size, rotationAngle, isLargeArc, sweepDirection);
                //_currentPoint = point;
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
                _geometryImpl.FillRule = fillRule;
            }
        }

        public FillRule FillRule { get; set; }
    }
}
