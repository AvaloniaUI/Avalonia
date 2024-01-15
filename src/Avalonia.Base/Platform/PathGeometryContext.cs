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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public void BeginFigure(Point startPoint, bool isFilled)
        {
            ThrowIfDisposed();

            _currentFigure = new PathFigure { StartPoint = startPoint, IsClosed = false, IsFilled = isFilled };

            _pathGeometry.Figures ??= new();
            _pathGeometry.Figures.Add(_currentFigure);
        }

        /// <inheritdoc/>
        public void CubicBezierTo(Point controlPoint1, Point controlPoint2, Point endPoint)
        {
            var bezierSegment = new BezierSegment { Point1 = controlPoint1, Point2 = controlPoint2, Point3 = endPoint };

            CurrentFigureSegments().Add(bezierSegment);
        }

        /// <inheritdoc/>
        public void QuadraticBezierTo(Point controlPoint , Point endPoint)
        {
            var quadraticBezierSegment = new QuadraticBezierSegment { Point1 = controlPoint , Point2 = endPoint };

            CurrentFigureSegments().Add(quadraticBezierSegment);
        }

        /// <inheritdoc/>
        public void LineTo(Point endPoint)
        {
            var lineSegment = new LineSegment
            {
                Point = endPoint
            };

            CurrentFigureSegments().Add(lineSegment);
        }

        /// <inheritdoc/>
        public void EndFigure(bool isClosed)
        {
            if (_currentFigure != null)
            {
                _currentFigure.IsClosed = isClosed;
            }

            _currentFigure = null;
        }

        /// <inheritdoc/>
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
