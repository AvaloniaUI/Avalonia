using System.Diagnostics.CodeAnalysis;
using Avalonia.Platform;

namespace Avalonia.Media
{
    /// <summary>
    /// An immutable <see cref="IGeometryImpl"/> that wraps a built platform geometry and exposes only
    /// its read-only surface.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="StreamGeometry"/> / <see cref="IStreamGeometryImpl"/> this cannot be
    /// re-opened or mutated (it does not implement <see cref="IStreamGeometryImpl"/>, so it cannot be
    /// down-cast and re-opened), which makes a single instance safe to cache and share — e.g. the
    /// glyph outline returned by <see cref="GlyphTypeface.GetGlyphOutline(ushort)"/>. It is also not
    /// an <see cref="AvaloniaObject"/>, so it carries none of the styling / property-system overhead.
    /// </remarks>
    internal sealed class ImmutableGeometryImpl : IGeometryImpl
    {
        private readonly IGeometryImpl _inner;

        public ImmutableGeometryImpl(IGeometryImpl inner) => _inner = inner;

        public Rect Bounds => _inner.Bounds;

        public double ContourLength => _inner.ContourLength;

        public Rect GetRenderBounds(IPen? pen) => _inner.GetRenderBounds(pen);

        public IGeometryImpl GetWidenedGeometry(IPen pen) => _inner.GetWidenedGeometry(pen);

        public bool FillContains(Point point) => _inner.FillContains(point);

        public IGeometryImpl? Intersect(IGeometryImpl geometry) => _inner.Intersect(geometry);

        public bool StrokeContains(IPen? pen, Point point) => _inner.StrokeContains(pen, point);

        public ITransformedGeometryImpl WithTransform(Matrix transform) => _inner.WithTransform(transform);

        public bool TryGetPointAtDistance(double distance, out Point point)
            => _inner.TryGetPointAtDistance(distance, out point);

        public bool TryGetPointAndTangentAtDistance(double distance, out Point point, out Point tangent)
            => _inner.TryGetPointAndTangentAtDistance(distance, out point, out tangent);

        public bool TryGetSegment(double startDistance, double stopDistance, bool startOnBeginFigure,
            [NotNullWhen(true)] out IGeometryImpl? segmentGeometry)
            => _inner.TryGetSegment(startDistance, stopDistance, startOnBeginFigure, out segmentGeometry);
    }
}
