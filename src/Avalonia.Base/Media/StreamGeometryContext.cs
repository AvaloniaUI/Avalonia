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
        private readonly IGeometryContext _impl;
        private readonly Action? _onDispose;

        private Point _currentPoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamGeometryContext"/> class.
        /// </summary>
        /// <param name="impl">The platform-specific implementation.</param>
        public StreamGeometryContext(IStreamGeometryContextImpl impl)
            : this(impl, null)
        {
        }

        internal StreamGeometryContext(IGeometryContext impl, Action? onDispose = null)
        {
            _impl = impl;
            _onDispose = onDispose;
        }

        /// <summary>
        /// Sets path's winding rule (default is EvenOdd). You should call this method before any calls to BeginFigure. If you wonder why, ask Direct2D guys about their design decisions.
        /// </summary>
        /// <param name="fillRule"></param>
        public void SetFillRule(FillRule fillRule)
        {
            _impl.SetFillRule(fillRule);
        }

        /// <inheritdoc />
        public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection, bool isStroked = true)
        {
            _impl.ArcTo(point, size, rotationAngle, isLargeArc, sweepDirection, isStroked);
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
            _impl.BeginFigure(startPoint, isFilled);
            _currentPoint = startPoint;
        }

        /// <inheritdoc/>
        public void CubicBezierTo(Point controlPoint1, Point controlPoint2, Point endPoint, bool isStroked = true)
        {
            _impl.CubicBezierTo(controlPoint1, controlPoint2, endPoint, isStroked);
            _currentPoint = endPoint;
        }

        /// <inheritdoc/>
        public void QuadraticBezierTo(Point controlPoint, Point endPoint, bool isStroked = true)
        {
            _impl.QuadraticBezierTo(controlPoint, endPoint, isStroked);
            _currentPoint = endPoint;
        }

        /// <inheritdoc/>
        public void LineTo(Point point, bool isStroked = true)
        {
            _impl.LineTo(point, isStroked);
            _currentPoint = point;
        }

        /// <inheritdoc/>
        public void EndFigure(bool isClosed)
        {
            _impl.EndFigure(isClosed);
        }

        /// <summary>
        /// Finishes the drawing session.
        /// </summary>
        public void Dispose()
        {
            _impl.Dispose();
            _onDispose?.Invoke();
        }
    }
}
