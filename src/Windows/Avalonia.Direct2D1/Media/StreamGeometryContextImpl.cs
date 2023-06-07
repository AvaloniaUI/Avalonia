using System;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Platform;
using SharpDX.Direct2D1;
using D2D = SharpDX.Direct2D1;
using SweepDirection = SharpDX.Direct2D1.SweepDirection;

namespace Avalonia.Direct2D1.Media
{
    internal class StreamGeometryContextImpl : IStreamGeometryContextImpl
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
            Avalonia.Media.SweepDirection sweepDirection)
        {
            _sink.AddArc(new D2D.ArcSegment
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

        public void CubicBezierTo(Point point1, Point point2, Point point3)
        {
            _sink.AddBezier(new D2D.BezierSegment
            {
                Point1 = point1.ToSharpDX(),
                Point2 = point2.ToSharpDX(),
                Point3 = point3.ToSharpDX(),
            });
        }

        public void QuadraticBezierTo(Point control, Point dest)
        {
            _sink.AddQuadraticBezier(new D2D.QuadraticBezierSegment
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

        public void SetFillRule(FillRule fillRule)
        {
            _sink.SetFillMode(fillRule == FillRule.EvenOdd ? FillMode.Alternate : FillMode.Winding);
        }

        public void Dispose()
        {
            // Put a catch around sink.Close as it may throw if there were an error e.g. parsing a path.
            try
            {
                _sink.Close();
            }
            catch (Exception ex)
            {
                Logger.TryGet(LogEventLevel.Error, LogArea.Visual)?.Log(
                    this,
                    "GeometrySink.Close exception: {Exception}",
                    ex);
            }

            _sink.Dispose();
        }
    }
}
