// -----------------------------------------------------------------------
// <copyright file="StreamGeometryContextImpl.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Direct2D1.Media
{
    using Perspex.Platform;
    using SharpDX.Direct2D1;

    public class StreamGeometryContextImpl : IStreamGeometryContextImpl
    {
        private GeometrySink sink;

        public StreamGeometryContextImpl(GeometrySink sink)
        {
            this.sink = sink;
        }

        public void BeginFigure(Point startPoint, bool isFilled)
        {
            this.sink.BeginFigure(startPoint.ToSharpDX(), isFilled ? FigureBegin.Filled : FigureBegin.Hollow);
        }

        public void BezierTo(Point point1, Point point2, Point point3)
        {
            this.sink.AddBezier(new BezierSegment
            {
                Point1 = point1.ToSharpDX(),
                Point2 = point2.ToSharpDX(),
                Point3 = point3.ToSharpDX(),
            });
        }

        public void LineTo(Point point)
        {
            this.sink.AddLine(point.ToSharpDX());
        }

        public void EndFigure(bool isClosed)
        {
            this.sink.EndFigure(isClosed ? FigureEnd.Closed : FigureEnd.Open);
        }

        public void Dispose()
        {
            this.sink.Close();
            this.sink.Dispose();
        }
    }
}
