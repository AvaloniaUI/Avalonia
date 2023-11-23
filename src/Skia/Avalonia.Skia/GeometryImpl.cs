using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Skia.Helpers;
using Avalonia.Utilities;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// A Skia implementation of <see cref="IGeometryImpl"/>.
    /// </summary>
    internal abstract class GeometryImpl : IGeometryImpl
    {
        private PathCache _pathCache;
        private SKPathMeasure? _cachedPathMeasure;

        private SKPathMeasure CachedPathMeasure => _cachedPathMeasure ??= new SKPathMeasure(StrokePath!);

        /// <inheritdoc />
        public abstract Rect Bounds { get; }

        /// <inheritdoc />
        public double ContourLength
        {
            get
            {
                if (StrokePath is null)
                    return 0;

                return CachedPathMeasure.Length;
            }
        }

        public abstract SKPath? StrokePath { get; }
        public abstract SKPath? FillPath { get; }

        /// <inheritdoc />
        public bool FillContains(Point point)
        {
            return PathContainsCore(FillPath, point);
        }

        /// <inheritdoc />
        public bool StrokeContains(IPen? pen, Point point)
        {
            _pathCache.UpdateIfNeeded(StrokePath, pen);

            return PathContainsCore(_pathCache.ExpandedPath, point);
        }
        
        /// <summary>
        /// Check Skia path if it contains a point.
        /// </summary>
        /// <param name="path">Path to check.</param>
        /// <param name="point">Point.</param>
        /// <returns>True, if point is contained in a path.</returns>
        private static bool PathContainsCore(SKPath? path, Point point)
        {
            return path is not null && path.Contains((float)point.X, (float)point.Y);
        }

        /// <inheritdoc />
        public IGeometryImpl? Intersect(IGeometryImpl geometry)
        {
            var other = geometry as GeometryImpl;
            if (other == null)
                return null;
            return CombinedGeometryImpl.TryCreate(GeometryCombineMode.Intersect, this, other);
        }

        /// <inheritdoc />
        public Rect GetRenderBounds(IPen? pen)
        {
            _pathCache.UpdateIfNeeded(StrokePath, pen);
            return _pathCache.RenderBounds;
        }

        public IGeometryImpl GetWidenedGeometry(IPen pen)
        {
            if (StrokePath is not null && SKPathHelper.CreateStrokedPath(StrokePath, pen) is { } path)
            {
                // The path returned to us by skia here does not have closed figures.
                // Fix that by calling CreateClosedPath.
                var closed = SKPathHelper.CreateClosedPath(path);
                return new StreamGeometryImpl(closed, closed);
            }
            
            return new StreamGeometryImpl(new SKPath(), null);
        }

        /// <inheritdoc />
        public ITransformedGeometryImpl WithTransform(Matrix transform)
        {
            return new TransformedGeometryImpl(this, transform);
        }

        /// <inheritdoc />
        public bool TryGetPointAtDistance(double distance, out Point point)
        {
            if (StrokePath is null)
            {
                point = new Point();
                return false;
            }

            var res = CachedPathMeasure.GetPosition((float)distance, out var skPoint);
            point = new Point(skPoint.X, skPoint.Y);
            return res;
        }

        /// <inheritdoc />
        public bool TryGetPointAndTangentAtDistance(double distance, out Point point, out Point tangent)
        {
            if (StrokePath is null)
            {
                point = new Point();
                tangent = new Point();
                return false;
            }

            var res = CachedPathMeasure.GetPositionAndTangent((float)distance, out var skPoint, out var skTangent);
            point = new Point(skPoint.X, skPoint.Y);
            tangent = new Point(skTangent.X, skTangent.Y);
            return res;
        }

        public bool TryGetSegment(double startDistance, double stopDistance, bool startOnBeginFigure,
            [NotNullWhen(true)] out IGeometryImpl? segmentGeometry)
        {
            if (StrokePath is null)
            {
                segmentGeometry = null;
                return false;
            }

            segmentGeometry = null;

            var _skPathSegment = new SKPath();

            var res = CachedPathMeasure.GetSegment((float)startDistance, (float)stopDistance, _skPathSegment, startOnBeginFigure);

            if (res)
            {
                segmentGeometry = new StreamGeometryImpl(_skPathSegment, null);
            }

            return res;
        }

        /// <summary>
        /// Invalidate all caches. Call after chaining path contents.
        /// </summary>
        protected void InvalidateCaches()
        {
            _pathCache.Dispose();
            _pathCache = default;
        }

        private struct PathCache : IDisposable
        {
            private int _penHash;
            private SKPath? _path, _cachedFor;
            private Rect? _renderBounds;
            private static readonly SKPath s_emptyPath = new();
            
            public Rect RenderBounds => _renderBounds ??= (_path ?? _cachedFor ?? s_emptyPath).Bounds.ToAvaloniaRect();
            public SKPath ExpandedPath => _path ?? s_emptyPath;

            public void UpdateIfNeeded(SKPath? strokePath, IPen? pen)
            {
                if (PenHelper.GetHashCode(pen, includeBrush: false) is { } penHash &&
                    penHash == _penHash &&
                    strokePath == _cachedFor)
                {
                    // We are up to date
                    return;
                }

                _renderBounds = null;
                _cachedFor = strokePath;
                _penHash = penHash;
                _path?.Dispose();

                if (strokePath is not null && pen is not null)
                    _path = SKPathHelper.CreateStrokedPath(strokePath, pen);
                else
                    _path = null;

            }

            public void Dispose()
            {
                _path?.Dispose();
                _path = null;
            }
        }
    }
}
