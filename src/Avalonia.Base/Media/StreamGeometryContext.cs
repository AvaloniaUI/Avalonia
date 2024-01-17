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
        private readonly IStreamGeometryContextImpl _impl;

        private Point _currentPoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamGeometryContext"/> class.
        /// </summary>
        /// <param name="impl">The platform-specific implementation.</param>
        public StreamGeometryContext(IStreamGeometryContextImpl impl)
        {
            _impl = impl;
        }

        /// <summary>
        /// Sets path's winding rule (default is EvenOdd). You should call this method before any calls to BeginFigure. If you wonder why, ask Direct2D guys about their design decisions.
        /// </summary>
        /// <param name="fillRule"></param>
        public void SetFillRule(FillRule fillRule)
        {
            _impl.SetFillRule(fillRule);
        }


        /// <inheritdoc/>
        public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection)
        {
            _impl.ArcTo(point, size, rotationAngle, isLargeArc, sweepDirection);
            _currentPoint = point;
        }


        /// <summary>
        /// Draws an arc to the specified point using polylines, quadratic or cubic Bezier curves
        /// Significantly more precise when drawing elliptic arcs with extreme width:height ratios.
        /// </summary>
        /// <param name="point">The destination point.</param>
        /// <param name="size">The radii of an oval whose perimeter is used to draw the angle.</param>
        /// <param name="rotationAngle">The rotation angle (in radians) of the oval that specifies the curve.</param>
        /// <param name="isLargeArc">true to draw the arc greater than 180 degrees; otherwise, false.</param>
        /// <param name="sweepDirection">
        /// A value that indicates whether the arc is drawn in the Clockwise or Counterclockwise direction.
        /// </param>
        public void PreciseArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection)
        {
            PreciseEllipticArcHelper.ArcTo(this, _currentPoint, point, size, rotationAngle, isLargeArc, sweepDirection);
        }


        /// <inheritdoc/>
        public void BeginFigure(Point startPoint, bool isFilled)
        {
            _impl.BeginFigure(startPoint, isFilled);
            _currentPoint = startPoint;
        }

        /// <inheritdoc/>
        public void CubicBezierTo(Point controlPoint1, Point controlPoint2, Point endPoint)
        {
            _impl.CubicBezierTo(controlPoint1, controlPoint2, endPoint);
            _currentPoint = endPoint;
        }

        /// <inheritdoc/>
        public void QuadraticBezierTo(Point controlPoint , Point endPoint)
        {
            _impl.QuadraticBezierTo(controlPoint , endPoint);
            _currentPoint = endPoint;
        }


        /// <inheritdoc/>
        public void LineTo(Point endPoint)
        {
            _impl.LineTo(endPoint);
            _currentPoint = endPoint;
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
        }
    }
}
