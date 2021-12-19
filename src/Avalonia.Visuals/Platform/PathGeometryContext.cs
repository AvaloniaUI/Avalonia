using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Visuals.Platform
{
    public class PathGeometryContext : IGeometryContext
    {
        private PathFigure? _currentFigure;
        private PathGeometry? _pathGeometry;

        public PathGeometryContext(PathGeometry pathGeometry)
        {
            _pathGeometry = pathGeometry ?? throw new ArgumentNullException(nameof(pathGeometry));
        }

        public void Dispose()
        {
            _pathGeometry = null;
        }

        public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection)
        {
            var arcSegment = new ArcSegment
            {
                Size = size,
                RotationAngle = rotationAngle,
                IsLargeArc = isLargeArc,
                SweepDirection = sweepDirection,
                Point = point
            };

            CurrentFigureSegments().Add(arcSegment);
        }

        public void BeginFigure(Point startPoint, bool isFilled)
        {
            ThrowIfDisposed();

            _currentFigure = new PathFigure { StartPoint = startPoint, IsClosed = false, IsFilled = isFilled };

            _pathGeometry.Figures ??= new();
            _pathGeometry.Figures.Add(_currentFigure);
        }

        public void CubicBezierTo(Point point1, Point point2, Point point3)
        {
            var bezierSegment = new BezierSegment { Point1 = point1, Point2 = point2, Point3 = point3 };

            CurrentFigureSegments().Add(bezierSegment);
        }

        public void QuadraticBezierTo(Point control, Point endPoint)
        {
            var quadraticBezierSegment = new QuadraticBezierSegment { Point1 = control, Point2 = endPoint };

            CurrentFigureSegments().Add(quadraticBezierSegment);
        }

        public void LineTo(Point point)
        {
            var lineSegment = new LineSegment
            {
                Point = point
            };

            CurrentFigureSegments().Add(lineSegment);
        }

        public void EndFigure(bool isClosed)
        {
            if (_currentFigure != null)
            {
                _currentFigure.IsClosed = isClosed;
            }

            _currentFigure = null;
        }

        public void SetFillRule(FillRule fillRule)
        {
            ThrowIfDisposed();
            _pathGeometry.FillRule = fillRule;
        }

        [MemberNotNull(nameof(_pathGeometry))]
        private void ThrowIfDisposed()
        {
            if (_pathGeometry is null)
                throw new ObjectDisposedException(nameof(PathGeometryContext));
        }

        private PathSegments CurrentFigureSegments()
        {
            ThrowIfDisposed();

            if (_currentFigure is null)
                throw new InvalidOperationException("No figure in progress.");
            if (_currentFigure.Segments is null)
                throw new InvalidOperationException("Current figure's segments cannot be null.");
            return _currentFigure.Segments;
        }
    }
}
