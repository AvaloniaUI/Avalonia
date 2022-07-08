using System;
using Avalonia.Media;
using Avalonia.Native.Interop;
using Avalonia.Native.Interop.Impl;
using Avalonia.Platform;

namespace Avalonia.NativeGraphics.Backend
{
    internal class StreamGeometryImpl : GeometryImpl, IStreamGeometryImpl
    {
        private Rect _bounds;
        private readonly IAvgPath _effectivePath;
        public StreamGeometryImpl(IAvgFactory factory) : base(factory)
        {
            _effectivePath = _factory.CreateAvgPath();
        }
        
        public IStreamGeometryImpl Clone()
        {
            return new StreamGeometryImpl(_factory);
        }

        class Context : IStreamGeometryContextImpl
        {
            private readonly StreamGeometryImpl _geometryImpl;
            private readonly IAvgPath _avgPath;
            public Context(StreamGeometryImpl geometryImpl)
            {
                _geometryImpl = geometryImpl;
                _avgPath = geometryImpl._avgPath;
            }
            public void Dispose()
            {
                //_avgPath.Dispose();
            }

            public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection)
            {
                AvgPoint p = new AvgPoint {X=point.X, Y=point.Y};
                AvgSize s = new AvgSize {Width = size.Width, Height = size.Height};
                AvgSweepDirection d = sweepDirection == SweepDirection.Clockwise
                    ? AvgSweepDirection.ClockWise
                    : AvgSweepDirection.CounterClockwise;
                _avgPath.ArcTo(p, s, rotationAngle, Convert.ToInt32(isLargeArc), d);
            }

            public void BeginFigure(Point startPoint, bool isFilled = true)
            {
                AvgPoint p = new AvgPoint
                {
                    X = startPoint.X,
                    Y = startPoint.Y
                };
                _avgPath.BeginFigure(p, Convert.ToInt32(isFilled));
            }

            public void CubicBezierTo(Point point1, Point point2, Point point3)
            {
                AvgPoint ap1 = new AvgPoint
                {
                    X = point1.X,
                    Y = point1.Y,
                };
                
                AvgPoint ap2 = new AvgPoint
                {
                    X = point2.X,
                    Y = point2.Y,
                };
                AvgPoint ap3 = new AvgPoint
                {
                    X = point3.X,
                    Y = point3.Y,
                };
                _avgPath.CubicBezierTo(ap1, ap2, ap3);
            }

            public void QuadraticBezierTo(Point point1, Point point2)
            {
                AvgPoint ap1 = new AvgPoint
                {
                    X = point1.X,
                    Y = point1.Y,
                };
                
                AvgPoint ap2 = new AvgPoint
                {
                    X = point2.X,
                    Y = point2.Y,
                };
                _avgPath.QuadraticBezierTo(ap1, ap2);
            }

            public void LineTo(Point point)
            {
                _avgPath.LineTo(new AvgPoint {X=point.X, Y=point.Y});
            }

            public void EndFigure(bool isClosed)
            {
                _avgPath.EndFigure(Convert.ToInt32(isClosed));
            }

            public void SetFillRule(FillRule fillRule)
            {
                AvgFillRule r = fillRule == FillRule.EvenOdd ? AvgFillRule.EvenOdd : AvgFillRule.NonZero;
                _avgPath.SetFillRule(r);
            }
        }

        public IStreamGeometryContextImpl Open()
        {
            return new Context(this);
        }
    }
}