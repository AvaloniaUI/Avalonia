using System;
using Avalonia.Media;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// A Skia implementation of <see cref="IGeometryImpl"/>.
    /// </summary>
    internal abstract class GeometryImpl : IGeometryImpl
    {
        private PathCache _pathCache;
        private SKPathMeasure _pathMeasureCache;

        private SKPathMeasure CachedPathMeasure
        {
            get
            {
                if (_pathMeasureCache is null)
                {
                    _pathMeasureCache = new SKPathMeasure(EffectivePath);
                }

                return _pathMeasureCache;
            }
        }

        /// <inheritdoc />
        public abstract Rect Bounds { get; }

        /// <inheritdoc />
        public double ContourLength
        {
            get
            {
                if (EffectivePath is null)
                    return 0;

                return (double)CachedPathMeasure?.Length;
            }
        }

        public abstract SKPath EffectivePath { get; }

        /// <inheritdoc />
        public bool FillContains(Point point)
        {
            return PathContainsCore(EffectivePath, point);
        }

        /// <inheritdoc />
        public bool StrokeContains(IPen pen, Point point)
        {
            // Skia requires to compute stroke path to check for point containment.
            // Due to that we are caching using stroke width.
            // Usually this function is being called with same stroke width per path, so this saves a lot of Skia traffic.

            var strokeWidth = (float)(pen?.Thickness ?? 0);

            if (!_pathCache.HasCacheFor(strokeWidth))
            {
                UpdatePathCache(strokeWidth);
            }

            return PathContainsCore(_pathCache.CachedStrokePath, point);
        }

        /// <summary>
        /// Update path cache for given stroke width.
        /// </summary>
        /// <param name="strokeWidth">Stroke width.</param>
        private void UpdatePathCache(float strokeWidth)
        {
            var strokePath = new SKPath();

            // For stroke widths close to 0 simply use empty path. Render bounds are cached from fill path.
            if (Math.Abs(strokeWidth) < float.Epsilon)
            {
                _pathCache.Cache(strokePath, strokeWidth, Bounds);
            }
            else
            {
                using (var paint = new SKPaint())
                {
                    paint.IsStroke = true;
                    paint.StrokeWidth = strokeWidth;

                    paint.GetFillPath(EffectivePath, strokePath);

                    _pathCache.Cache(strokePath, strokeWidth, strokePath.TightBounds.ToAvaloniaRect());
                }
            }
        }

        /// <summary>
        /// Check Skia path if it contains a point.
        /// </summary>
        /// <param name="path">Path to check.</param>
        /// <param name="point">Point.</param>
        /// <returns>True, if point is contained in a path.</returns>
        private static bool PathContainsCore(SKPath path, Point point)
        {
            return path.Contains((float)point.X, (float)point.Y);
        }

        /// <inheritdoc />
        public IGeometryImpl Intersect(IGeometryImpl geometry)
        {
            var result = EffectivePath.Op(((GeometryImpl)geometry).EffectivePath, SKPathOp.Intersect);

            return result == null ? null : new StreamGeometryImpl(result);
        }

        /// <inheritdoc />
        public Rect GetRenderBounds(IPen pen)
        {
            var strokeWidth = (float)(pen?.Thickness ?? 0);

            if (!_pathCache.HasCacheFor(strokeWidth))
            {
                UpdatePathCache(strokeWidth);
            }

            return _pathCache.CachedGeometryRenderBounds;
        }

        /// <inheritdoc />
        public ITransformedGeometryImpl WithTransform(Matrix transform)
        {
            return new TransformedGeometryImpl(this, transform);
        }

        /// <inheritdoc />
        public bool TryGetPointAtDistance(double distance, out Point point)
        {
            if (EffectivePath is null)
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
            if (EffectivePath is null)
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
            out IGeometryImpl segmentGeometry)
        {
            if (EffectivePath is null)
            {
                segmentGeometry = null;
                return false;
            }

            segmentGeometry = null;

            var _skPathSegment = new SKPath();

            var res = CachedPathMeasure.GetSegment((float)startDistance, (float)stopDistance, _skPathSegment, startOnBeginFigure);

            if (res)
            {
                segmentGeometry = new StreamGeometryImpl(_skPathSegment);
            }

            return res;
        }

        /// <summary>
        /// Invalidate all caches. Call after chaining path contents.
        /// </summary>
        protected void InvalidateCaches()
        {
            _pathCache.Invalidate();
        }

        private struct PathCache
        {
            private float _cachedStrokeWidth;

            /// <summary>
            /// Tolerance for two stroke widths to be deemed equal
            /// </summary>
            public const float Tolerance = float.Epsilon;

            /// <summary>
            /// Cached contour path.
            /// </summary>
            public SKPath CachedStrokePath { get; private set; }

            /// <summary>
            /// Cached geometry render bounds.
            /// </summary>
            public Rect CachedGeometryRenderBounds { get; private set; }

            /// <summary>
            /// Is cached valid for given stroke width.
            /// </summary>
            /// <param name="strokeWidth">Stroke width to check.</param>
            /// <returns>True, if CachedStrokePath can be used for given stroke width.</returns>
            public bool HasCacheFor(float strokeWidth)
            {
                return CachedStrokePath != null && Math.Abs(_cachedStrokeWidth - strokeWidth) < Tolerance;
            }

            /// <summary>
            /// Cache path for given stroke width. Takes ownership of a passed path.
            /// </summary>
            /// <param name="path">Path to cache.</param>
            /// <param name="strokeWidth">Stroke width to cache.</param>
            /// <param name="geometryRenderBounds">Render bounds to use.</param>
            public void Cache(SKPath path, float strokeWidth, Rect geometryRenderBounds)
            {
                if (CachedStrokePath != path)
                {
                    CachedStrokePath?.Dispose();
                }

                CachedStrokePath = path;
                CachedGeometryRenderBounds = geometryRenderBounds;
                _cachedStrokeWidth = strokeWidth;
            }

            /// <summary>
            /// Invalidate cache state.
            /// </summary>
            public void Invalidate()
            {
                CachedStrokePath?.Dispose();
                CachedGeometryRenderBounds = Rect.Empty;
                _cachedStrokeWidth = default(float);
            }
        }
    }
}
