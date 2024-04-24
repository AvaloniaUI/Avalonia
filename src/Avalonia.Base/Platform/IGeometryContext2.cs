using Avalonia.Media;

namespace Avalonia.Platform
{
    // TODO12 combine with IGeometryContext
    public interface IGeometryContext2 : IGeometryContext
    {
        /// <summary>
        /// Draws a line to the specified point.
        /// </summary>
        /// <param name="point">The destination point.</param>
        /// <param name="isStroked">Whether the segment is stroked</param>
        void LineTo(Point point, bool isStroked);

        /// <summary>
        /// Draws an arc to the specified point.
        /// </summary>
        /// <param name="point">The destination point.</param>
        /// <param name="size">The radii of an oval whose perimeter is used to draw the angle.</param>
        /// <param name="rotationAngle">The rotation angle (in radians) of the oval that specifies the curve.</param>
        /// <param name="isLargeArc">true to draw the arc greater than 180 degrees; otherwise, false.</param>
        /// <param name="sweepDirection">
        /// A value that indicates whether the arc is drawn in the Clockwise or Counterclockwise direction.
        /// </param>
        /// <param name="isStroked">Whether the segment is stroked</param>
        void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection, bool isStroked);

        /// <summary>
        /// Draws a Bezier curve to the specified point.
        /// </summary>
        /// <param name="controlPoint1">The first control point used to specify the shape of the curve.</param>
        /// <param name="controlPoint2">The second control point used to specify the shape of the curve.</param>
        /// <param name="endPoint">The destination point for the end of the curve.</param>
        /// <param name="isStroked">Whether the segment is stroked</param>
        void CubicBezierTo(Point controlPoint1, Point controlPoint2, Point endPoint, bool isStroked);

        /// <summary>
        /// Draws a quadratic Bezier curve to the specified point
        /// </summary>
        /// <param name="controlPoint ">Control point</param>
        /// <param name="endPoint">DestinationPoint</param>
        /// <param name="isStroked">Whether the segment is stroked</param>
        void QuadraticBezierTo(Point controlPoint, Point endPoint, bool isStroked);
    }

}
