using System.Collections.Generic;
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
    public class StreamGeometryContext : IGeometryContext, IGeometryContext2
    {
        private readonly IStreamGeometryContextImpl _impl;
        private abstract record Command
        {
            public sealed record Fill(FillRule rule) : Command;
            public sealed record ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection) : Command;
            public sealed record BeginFigure(Point startPoint, bool isFilled) : Command;
            public sealed record CubicBezierTo(Point controlPoint1, Point controlPoint2, Point endPoint) : Command;
            public sealed record QuadraticBezierTo(Point controlPoint, Point endPoint) : Command;
            public sealed record LineTo(Point endPoint) : Command;
            public sealed record EndFigure(bool isClosed) : Command;
        }
        private readonly List<Command> _commands = new();

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
        public void SetFillRule(FillRule fillRule) =>
            _commands.Add(new Command.Fill(fillRule));

        /// <inheritdoc/>
        public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection)
        {
            _commands.Add(new Command.ArcTo(point, size, rotationAngle, isLargeArc, sweepDirection));
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
            _currentPoint = point;
        }

        /// <inheritdoc/>
        public void BeginFigure(Point startPoint, bool isFilled)
        {
            _commands.Add(new Command.BeginFigure(startPoint, isFilled));
            _currentPoint = startPoint;
        }

        /// <inheritdoc/>
        public void CubicBezierTo(Point controlPoint1, Point controlPoint2, Point endPoint)
        {
            _commands.Add(new Command.CubicBezierTo(controlPoint1, controlPoint2, endPoint));
            _currentPoint = endPoint;
        }

        /// <inheritdoc/>
        public void QuadraticBezierTo(Point controlPoint, Point endPoint)
        {
            _commands.Add(new Command.QuadraticBezierTo(controlPoint, endPoint));
            _currentPoint = endPoint;
        }

        /// <inheritdoc/>
        public void LineTo(Point endPoint)
        {
            _commands.Add(new Command.LineTo(endPoint));
            _currentPoint = endPoint;
        }

        /// <inheritdoc/>
        public void EndFigure(bool isClosed)
        {
            var endFigureIndex = _commands.Count;
            _commands.Add(new Command.EndFigure(isClosed));
            var nCommands = endFigureIndex + 1;
            if (nCommands > 1)
            {
                var empty = false;
                for (int i = 0; !empty == i < nCommands; i++)
                {
                    var currentCommand = _commands[i];
                    switch (currentCommand)
                    {
                        case Command.BeginFigure begin:
                            {
                                if (i + 1 == endFigureIndex)
                                {
                                    empty = true;
                                }
                                else
                                {
                                    _impl.BeginFigure(begin.startPoint, begin.isFilled);
                                }
                            }
                            break;
                        case Command.EndFigure end:
                            _impl.EndFigure(end.isClosed);
                            break;
                        case Command.ArcTo arc:
                            _impl.ArcTo(arc.point, arc.size, arc.rotationAngle, arc.isLargeArc, arc.sweepDirection);
                            break;
                        case Command.CubicBezierTo cBezier:
                            _impl.CubicBezierTo(cBezier.controlPoint1, cBezier.controlPoint2, cBezier.endPoint);
                            break;
                        case Command.Fill fill:
                            _impl.SetFillRule(fill.rule);
                            break;
                        case Command.LineTo line:
                            _impl.LineTo(line.endPoint);
                            break;
                        case Command.QuadraticBezierTo qBezier:
                            _impl.QuadraticBezierTo(qBezier.controlPoint, qBezier.endPoint);
                            break;
                        default:
                            break;
                    }
                }
            }
            _commands.Clear();
        }

        /// <summary>
        /// Finishes the drawing session.
        /// </summary>
        public void Dispose()
        {
            _impl.Dispose();
        }

        /// <inheritdoc/>
        public void LineTo(Point point, bool isStroked)
        {
            if (_impl is IGeometryContext2 context2)
                context2.LineTo(point, isStroked);
            else
                _impl.LineTo(point);

            _currentPoint = point;
        }

        public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection, bool isStroked)
        {
            if (_impl is IGeometryContext2 context2)
                context2.ArcTo(point, size, rotationAngle, isLargeArc, sweepDirection, isStroked);
            else
                _impl.ArcTo(point, size, rotationAngle, isLargeArc, sweepDirection);

            _currentPoint = point;
        }

        public void CubicBezierTo(Point controlPoint1, Point controlPoint2, Point endPoint, bool isStroked)
        {
            if (_impl is IGeometryContext2 context2)
                context2.CubicBezierTo(controlPoint1, controlPoint2, endPoint, isStroked);
            else
                _impl.CubicBezierTo(controlPoint1, controlPoint2, endPoint);

            _currentPoint = endPoint;
        }

        public void QuadraticBezierTo(Point controlPoint, Point endPoint, bool isStroked)
        {
            if (_impl is IGeometryContext2 context2)
                context2.QuadraticBezierTo(controlPoint, endPoint, isStroked);
            else
                _impl.QuadraticBezierTo(controlPoint, endPoint);

            _currentPoint = endPoint;
        }
    }
}
