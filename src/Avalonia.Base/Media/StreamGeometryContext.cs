using System;
using Avalonia.Platform;

namespace Avalonia.Media
{
    /// <summary>
    /// Describes a geometry using drawing commands.
    /// </summary>
    /// <remarks>
    /// This class is used to define the geometry of a <see cref="StreamGeometry"/>. An instance
    /// of <see cref="StreamGeometryContext"/> is obtained by calling
    /// <see cref="StreamGeometry.Open"/>.
    /// </remarks>
    public class StreamGeometryContext : IGeometryContext
    {
        private readonly IGeometryContext _context;

        private Point _currentPoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamGeometryContext"/> class.
        /// </summary>
        /// <param name="impl">The platform-specific implementation.</param>
        [Obsolete($"Use {nameof(StreamGeometry)}.{nameof(StreamGeometry.Open)} instead.")]
        public StreamGeometryContext(IStreamGeometryContextImpl impl)
        {
            _context = impl;
        }

        internal StreamGeometryContext(IGeometryContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Sets path's winding rule (default is EvenOdd). You should call this method before any calls to BeginFigure. If you wonder why, ask Direct2D guys about their design decisions.
        /// </summary>
        /// <param name="fillRule"></param>
        public void SetFillRule(FillRule fillRule)
        {
            _context.SetFillRule(fillRule);
        }

        /// <inheritdoc />
        public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection, bool isStroked = true)
        {
            _context.ArcTo(point, size, rotationAngle, isLargeArc, sweepDirection, isStroked);
            _currentPoint = point;
        }

        /// <summary>
        /// Draws an arc to the specified point using polylines, quadratic or cubic Bezier curves.
        /// This is significantly more precise when drawing elliptic arcs with extreme width:height ratios.
        /// </summary>
        /// <inheritdoc cref="ArcTo"/>
        public void PreciseArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection)
        {
            PreciseEllipticArcHelper.ArcTo(this, _currentPoint, point, size, rotationAngle, isLargeArc, sweepDirection);
        }

        /// <inheritdoc/>
        public void BeginFigure(Point startPoint, bool isFilled = true)
        {
            _context.BeginFigure(startPoint, isFilled);
            _currentPoint = startPoint;
        }

        /// <inheritdoc/>
        public void CubicBezierTo(Point controlPoint1, Point controlPoint2, Point endPoint, bool isStroked = true)
        {
            _context.CubicBezierTo(controlPoint1, controlPoint2, endPoint, isStroked);
            _currentPoint = endPoint;
        }

        /// <inheritdoc/>
        public void QuadraticBezierTo(Point controlPoint, Point endPoint, bool isStroked = true)
        {
            _context.QuadraticBezierTo(controlPoint, endPoint, isStroked);
            _currentPoint = endPoint;
        }

        /// <inheritdoc/>
        public void LineTo(Point point, bool isStroked = true)
        {
            _context.LineTo(point, isStroked);
            _currentPoint = point;
        }

        /// <inheritdoc/>
        public void EndFigure(bool isClosed)
        {
            _context.EndFigure(isClosed);
        }

        /// <summary>
        /// Finishes the drawing session.
        /// </summary>
        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
