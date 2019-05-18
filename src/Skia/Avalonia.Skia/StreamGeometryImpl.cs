// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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
        private readonly SKPath _effectivePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamGeometryImpl"/> class.
        /// </summary>
        /// <param name="path">An existing Skia <see cref="SKPath"/>.</param>
        /// <param name="bounds">Precomputed path bounds.</param>
        public StreamGeometryImpl(SKPath path, Rect bounds)
        {
            _effectivePath = path;
            _bounds = bounds;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamGeometryImpl"/> class.
        /// </summary>
        /// <param name="path">An existing Skia <see cref="SKPath"/>.</param>
        public StreamGeometryImpl(SKPath path) : this(path, path.TightBounds.ToAvaloniaRect())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamGeometryImpl"/> class.
        /// </summary>
        public StreamGeometryImpl() : this(CreateEmptyPath(), Rect.Empty)
        {
        }
        
        /// <inheritdoc />
        public override SKPath EffectivePath => _effectivePath;

        /// <inheritdoc />
        public override Rect Bounds => _bounds;

        /// <inheritdoc />
        public IStreamGeometryImpl Clone()
        {
            return new StreamGeometryImpl(_effectivePath?.Clone(), Bounds);
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
            private readonly SKPath _path;

            /// <summary>
            /// Initializes a new instance of the <see cref="StreamContext"/> class.
            /// <param name="geometryImpl">Geometry to operate on.</param>
            /// </summary>
            public StreamContext(StreamGeometryImpl geometryImpl)
            {
                _geometryImpl = geometryImpl;
                _path = _geometryImpl._effectivePath;
            }
            
            /// <inheritdoc />
            /// <remarks>Will update bounds of passed geometry.</remarks>
            public void Dispose()
            {
                _geometryImpl._bounds = _path.TightBounds.ToAvaloniaRect();
                _geometryImpl.InvalidateCaches();
            }

            /// <inheritdoc />
            public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection)
            {
                _path.ArcTo(
                    (float)size.Width,
                    (float)size.Height,
                    (float)rotationAngle,
                    isLargeArc ? SKPathArcSize.Large : SKPathArcSize.Small,
                    sweepDirection == SweepDirection.Clockwise ? SKPathDirection.Clockwise : SKPathDirection.CounterClockwise,
                    (float)point.X,
                    (float)point.Y);
            }

            /// <inheritdoc />
            public void BeginFigure(Point startPoint, bool isFilled)
            {
                _path.MoveTo((float)startPoint.X, (float)startPoint.Y);
            }

            /// <inheritdoc />
            public void CubicBezierTo(Point point1, Point point2, Point point3)
            {
                _path.CubicTo((float)point1.X, (float)point1.Y, (float)point2.X, (float)point2.Y, (float)point3.X, (float)point3.Y);
            }

            /// <inheritdoc />
            public void QuadraticBezierTo(Point point1, Point point2)
            {
                _path.QuadTo((float)point1.X, (float)point1.Y, (float)point2.X, (float)point2.Y);
            }

            /// <inheritdoc />
            public void LineTo(Point point)
            {
                _path.LineTo((float)point.X, (float)point.Y);
            }

            /// <inheritdoc />
            public void EndFigure(bool isClosed)
            {
                if (isClosed)
                {
                    _path.Close();
                }
            }

            /// <inheritdoc />
            public void SetFillRule(FillRule fillRule)
            {
                _path.FillType = fillRule == FillRule.EvenOdd ? SKPathFillType.EvenOdd : SKPathFillType.Winding;
            }
        }
    }
}
