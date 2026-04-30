using Avalonia.Media;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// A Skia implementation of a <see cref="IStreamGeometryImpl"/>.
    /// </summary>
    internal class StreamGeometryImpl : GeometryImpl, IStreamGeometryImpl
    {
        private Rect _bounds;
        private SKPath _strokePath;
        private SKPath? _fillPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamGeometryImpl"/> class.
        /// </summary>
        /// <param name="stroke">An existing Skia <see cref="SKPath"/> for the stroke.</param>
        /// <param name="fill">An existing Skia <see cref="SKPath"/> for the fill, can also be null or the same as the stroke</param>
        /// <param name="bounds">Precomputed path bounds.</param>
        public StreamGeometryImpl(SKPath stroke, SKPath? fill, Rect? bounds = null)
        {
            _strokePath = stroke;
            _fillPath = fill;
            _bounds = bounds ?? stroke.TightBounds.ToAvaloniaRect();
        }

        private StreamGeometryImpl(SKPath path) : this(path, path, default(Rect))
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamGeometryImpl"/> class.
        /// </summary>
        public StreamGeometryImpl() : this(CreateEmptyPath())
        {
        }

        /// <inheritdoc />
        public override SKPath? StrokePath => _strokePath;

        /// <inheritdoc />
        public override SKPath? FillPath => _fillPath;

        /// <inheritdoc />
        public override Rect Bounds => _bounds;

        /// <inheritdoc />
        public IStreamGeometryImpl Clone()
        {
            var stroke = _strokePath.Clone();
            var fill = _fillPath == _strokePath ? stroke : _fillPath.Clone();
            return new StreamGeometryImpl(stroke, fill, Bounds);
        }

        /// <inheritdoc />
        public IStreamGeometryContextImpl Open()
        {
            return new StreamContext(this);
        }

        /// <summary>
        /// Create new empty <see cref="SKPath"/>.
        /// </summary>
        /// <returns>Empty <see cref="SKPath"/></returns>
        private static SKPath CreateEmptyPath()
        {
            return new SKPath
            {
                FillType = SKPathFillType.EvenOdd
            };
        }

        /// <summary>
        /// A Skia implementation of a <see cref="IStreamGeometryContextImpl"/>.
        /// </summary>
        private class StreamContext : IStreamGeometryContextImpl
        {
            private readonly StreamGeometryImpl _geometryImpl;
            private readonly SKPathBuilder _strokeBuilder;
            private SKPathBuilder? _fillBuilder;
            private SKPathFillType _fillType;
            private bool _isFilled;
            private Point _startPoint;
            private bool _isFigureBroken;
            private bool Duplicate => _isFilled && _fillBuilder != null;

            private void EnsureSeparateFillBuilder()
            {
                if (_fillBuilder == null)
                {
                    using var snapshot = _strokeBuilder.Snapshot();
                    _fillBuilder = new SKPathBuilder(snapshot) { FillType = _fillType };
                }
            }

            private void BreakFigure()
            {
                if (!_isFigureBroken)
                {
                    _isFigureBroken = true;
                    EnsureSeparateFillBuilder();
                }

            }

            /// <summary>
            /// Initializes a new instance of the <see cref="StreamContext"/> class.
            /// <param name="geometryImpl">Geometry to operate on.</param>
            /// </summary>
            public StreamContext(StreamGeometryImpl geometryImpl)
            {
                _geometryImpl = geometryImpl;
                _strokeBuilder = new SKPathBuilder(geometryImpl._strokePath);
                _fillType = geometryImpl._fillPath?.FillType ?? geometryImpl._strokePath.FillType;
                _strokeBuilder.FillType = _fillType;
                if (geometryImpl._fillPath != null && !ReferenceEquals(geometryImpl._fillPath, geometryImpl._strokePath))
                    _fillBuilder = new SKPathBuilder(geometryImpl._fillPath) { FillType = _fillType };
            }

            /// <inheritdoc />
            /// <remarks>Will update bounds of passed geometry.</remarks>
            public void Dispose()
            {
                var oldStroke = _geometryImpl._strokePath;
                var oldFill = _geometryImpl._fillPath;

                var newStroke = _strokeBuilder.Detach();
                newStroke.FillType = _fillType;
                _strokeBuilder.Dispose();

                SKPath newFill;
                if (_fillBuilder != null)
                {
                    newFill = _fillBuilder.Detach();
                    newFill.FillType = _fillType;
                    _fillBuilder.Dispose();
                }
                else
                {
                    newFill = newStroke;
                }

                _geometryImpl._strokePath = newStroke;
                _geometryImpl._fillPath = newFill;
                _geometryImpl._bounds = newStroke.TightBounds.ToAvaloniaRect();
                _geometryImpl.InvalidateCaches();

                if (oldFill != null && !ReferenceEquals(oldFill, oldStroke))
                    oldFill.Dispose();
                oldStroke.Dispose();
            }

            /// <inheritdoc />
            public void BeginFigure(Point startPoint, bool isFilled = true)
            {
                if (!isFilled)
                    EnsureSeparateFillBuilder();

                _isFilled = isFilled;
                _startPoint = startPoint;
                _isFigureBroken = false;
                _strokeBuilder.MoveTo((float)startPoint.X, (float)startPoint.Y);
                if (Duplicate)
                    _fillBuilder!.MoveTo((float)startPoint.X, (float)startPoint.Y);
            }

            /// <inheritdoc />
            public void EndFigure(bool isClosed)
            {
                if (isClosed)
                {
                    if (_isFigureBroken)
                    {
                        _strokeBuilder.LineTo(_startPoint.ToSKPoint());
                        _isFigureBroken = false;
                    }
                    else
                        _strokeBuilder.Close();
                    if (Duplicate)
                        _fillBuilder!.Close();
                }
            }

            /// <inheritdoc />
            public void SetFillRule(FillRule fillRule)
            {
                _fillType = fillRule == FillRule.EvenOdd ? SKPathFillType.EvenOdd : SKPathFillType.Winding;
                _strokeBuilder.FillType = _fillType;
                if (_fillBuilder != null)
                    _fillBuilder.FillType = _fillType;
            }

            /// <inheritdoc />
            public void LineTo(Point point, bool isStroked = true)
            {
                if (isStroked)
                {
                    _strokeBuilder.LineTo((float)point.X, (float)point.Y);
                }
                else
                {
                    BreakFigure();
                    _strokeBuilder.MoveTo((float)point.X, (float)point.Y);
                }
                if (Duplicate)
                    _fillBuilder!.LineTo((float)point.X, (float)point.Y);
            }

            /// <inheritdoc />
            public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection, bool isStroked = true)
            {
                var arc = isLargeArc ? SKPathArcSize.Large : SKPathArcSize.Small;
                var sweep = sweepDirection == SweepDirection.Clockwise
                    ? SKPathDirection.Clockwise
                    : SKPathDirection.CounterClockwise;

                if (isStroked)
                {
                    _strokeBuilder.ArcTo(
                        (float)size.Width,
                        (float)size.Height,
                        (float)rotationAngle,
                        arc,
                        sweep,
                        (float)point.X,
                        (float)point.Y);
                }
                else
                {
                    BreakFigure();
                    _strokeBuilder.MoveTo((float)point.X, (float)point.Y);
                }
                if (Duplicate)
                    _fillBuilder!.ArcTo(
                        (float)size.Width,
                        (float)size.Height,
                        (float)rotationAngle,
                        arc,
                        sweep,
                        (float)point.X,
                        (float)point.Y);
            }

            /// <inheritdoc />
            public void CubicBezierTo(Point point1, Point point2, Point point3, bool isStroked = true)
            {
                if (isStroked)
                {
                    _strokeBuilder.CubicTo((float)point1.X, (float)point1.Y, (float)point2.X, (float)point2.Y, (float)point3.X, (float)point3.Y);
                }
                else
                {
                    BreakFigure();
                    _strokeBuilder.MoveTo((float)point3.X, (float)point3.Y);
                }
                if (Duplicate)
                    _fillBuilder!.CubicTo((float)point1.X, (float)point1.Y, (float)point2.X, (float)point2.Y, (float)point3.X, (float)point3.Y);
            }

            /// <inheritdoc />
            public void QuadraticBezierTo(Point point1, Point point2, bool isStroked = true)
            {
                if (isStroked)
                {
                    _strokeBuilder.QuadTo((float)point1.X, (float)point1.Y, (float)point2.X, (float)point2.Y);
                }
                else
                {
                    BreakFigure();
                    _strokeBuilder.MoveTo((float)point2.X, (float)point2.Y);
                }
                if (Duplicate)
                    _fillBuilder!.QuadTo((float)point1.X, (float)point1.Y, (float)point2.X, (float)point2.Y);
            }
        }
    }
}
