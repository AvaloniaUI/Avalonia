// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Platform;
using SharpDX.Direct2D1;

namespace Perspex.Direct2D1.Media
{
    public class StreamGeometryContextImpl : IStreamGeometryContextImpl
    {
        private readonly GeometrySink _sink;

        public StreamGeometryContextImpl(GeometrySink sink)
        {
            _sink = sink;
        }

        public void ArcTo(
            Point point,
            Size size,
            double rotationAngle,
            bool isLargeArc,
            Perspex.Media.SweepDirection sweepDirection)
        {
            _sink.AddArc(new ArcSegment
            {
                Point = point.ToSharpDX(),
                Size = size.ToSharpDX(),
                RotationAngle = (float)rotationAngle,
                ArcSize = isLargeArc ? ArcSize.Large : ArcSize.Small,
                SweepDirection = (SweepDirection)sweepDirection,
            });
        }

        public void BeginFigure(Point startPoint, bool isFilled)
        {
            _sink.BeginFigure(startPoint.ToSharpDX(), isFilled ? FigureBegin.Filled : FigureBegin.Hollow);
        }

        public void BezierTo(Point point1, Point point2, Point point3)
        {
            _sink.AddBezier(new BezierSegment
            {
                Point1 = point1.ToSharpDX(),
                Point2 = point2.ToSharpDX(),
                Point3 = point3.ToSharpDX(),
            });
        }

        public void QuadTo(Point control, Point dest)
        {
            _sink.AddQuadraticBezier(new QuadraticBezierSegment
            {
                Point1 = control.ToSharpDX(),
                Point2 = dest.ToSharpDX()
            });
        }

        public void LineTo(Point point)
        {
            _sink.AddLine(point.ToSharpDX());
        }

        public void EndFigure(bool isClosed)
        {
            _sink.EndFigure(isClosed ? FigureEnd.Closed : FigureEnd.Open);
        }

        public void Dispose()
        {
            _sink.Close();
            _sink.Dispose();
        }
    }
}
