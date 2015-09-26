using CoreGraphics;
using Perspex.Media;
using Perspex.Platform;
using System;
using System.Collections.Generic;
using System.Text;

namespace Perspex.iOS.Rendering
{
    public class StreamGeometryImpl : IStreamGeometryImpl
    {
        public StreamGeometryImpl()
        {
            _impl = new StreamGeometryContextImpl(null);
        }

        public StreamGeometryImpl(StreamGeometryContextImpl impl)
        {
            _impl = impl;
        }

        public Rect Bounds
        {
            get { return _impl.Bounds; }
        }

        public CGPath Path
        {
            get { return _impl.Path; }
        }

        private readonly StreamGeometryContextImpl _impl;

        private Matrix _transform = Matrix.Identity;
        public Matrix Transform
        {
            get { return _transform; }
            set
            {
                if (value != Transform)
                {
                    if (!value.IsIdentity)
                    {
                        _transform = value;
                    }
                }
            }
        }

        public IStreamGeometryImpl Clone()
        {
            return new StreamGeometryImpl(_impl);
        }

        public Rect GetRenderBounds(double strokeThickness)
        {
            // TODO: Calculate properly.
            return Bounds.Inflate(strokeThickness);
        }

        public IStreamGeometryContextImpl Open()
        {
            return _impl;
        }
    }

    public class StreamGeometryContextImpl : IStreamGeometryContextImpl
    {
        public StreamGeometryContextImpl(CGPath path = null)
        {
            this.Path = path != null ? new CGPath(path) : new CGPath();
        }

        public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection)
        {
            // TODO: not sure how to translate these DirectX oriented arguments into CG
            //this.Path.AddArcToPoint()
            throw new NotImplementedException();
        }

        public void BeginFigure(Point startPoint, bool isFilled)
        {
            this.Path.MoveToPoint(startPoint.ToCoreGraphics());
        }

        public void BezierTo(Point point1, Point point2, Point point3)
        {
            this.Path.AddCurveToPoint(point1.ToCoreGraphics(), point2.ToCoreGraphics(), point3.ToCoreGraphics());
        }

        public void LineTo(Point point)
        {
            this.Path.AddLineToPoint(point.ToCoreGraphics());
        }

        public CGPath Path { get; private set; }
        public Rect Bounds { get { return this.Path.BoundingBox.ToPerspex(); } }

        public void EndFigure(bool isClosed)
        {
            if (this.Path == null)
            {
                if (isClosed)
                    this.Path.CloseSubpath();

                //Path = _context.CopyPath();

                // only update when called! But maybe we cache there
                //Bounds = _context.FillExtents().ToPerspex();
            }
        }

        public void Dispose()
        {
            // tricky thing is knowing when to dispose this. It appears Stream geometry concatentates
            // a sequence of ops into a single path?
            //
            //this.Path.Dispose();
            //this.Path = null;
        }
    }
}
