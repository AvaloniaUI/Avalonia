using Avalonia.Media.Fonts.Tables.Cff;
using Avalonia.Platform;

namespace Avalonia.Media.Fonts
{
    /// <summary>
    /// An <see cref="IGeometryContext"/> decorator that forwards every call to an inner context while
    /// tallying the number of path segments emitted and accumulating the control-point bounding box.
    /// Lets the outline build estimate its payload's retained size (the cache
    /// <see cref="GlyphCacheEntry.Cost"/>) and reuse the box as the entry's ink bounds, with no
    /// second pass.
    /// </summary>
    /// <remarks>
    /// The box is accumulated through the same <see cref="BoundsGeometryContext"/> the CFF / CFF2
    /// bounds-only interpret pass uses, so both producers of <see cref="GlyphCacheEntry.Bounds"/>
    /// write bit-identical values — the benign-race contract of
    /// <see cref="GlyphCacheEntry.SetBoundsOnce"/> — independent of any backend's notion of bounds.
    /// </remarks>
    internal sealed class SegmentCountingGeometryContext : IGeometryContext
    {
        private readonly IGeometryContext _inner;
        private readonly BoundsGeometryContext _bounds = new();

        public SegmentCountingGeometryContext(IGeometryContext inner) => _inner = inner;

        /// <summary>The number of line / curve / arc segments emitted so far.</summary>
        public int SegmentCount { get; private set; }

        /// <summary>
        /// The control-point box of every point emitted so far, or the zero box for an empty glyph.
        /// </summary>
        public GlyphBounds GetControlBounds() => _bounds.ToGlyphBounds();

        public void BeginFigure(Point startPoint, bool isFilled = true)
        {
            _bounds.BeginFigure(startPoint, isFilled);
            _inner.BeginFigure(startPoint, isFilled);
        }

        public void LineTo(Point point, bool isStroked = true)
        {
            SegmentCount++;
            _bounds.LineTo(point, isStroked);
            _inner.LineTo(point, isStroked);
        }

        public void CubicBezierTo(Point controlPoint1, Point controlPoint2, Point endPoint, bool isStroked = true)
        {
            SegmentCount++;
            _bounds.CubicBezierTo(controlPoint1, controlPoint2, endPoint, isStroked);
            _inner.CubicBezierTo(controlPoint1, controlPoint2, endPoint, isStroked);
        }

        public void QuadraticBezierTo(Point controlPoint, Point endPoint, bool isStroked = true)
        {
            SegmentCount++;
            _bounds.QuadraticBezierTo(controlPoint, endPoint, isStroked);
            _inner.QuadraticBezierTo(controlPoint, endPoint, isStroked);
        }

        public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc,
            SweepDirection sweepDirection, bool isStroked = true)
        {
            SegmentCount++;
            _bounds.ArcTo(point, size, rotationAngle, isLargeArc, sweepDirection, isStroked);
            _inner.ArcTo(point, size, rotationAngle, isLargeArc, sweepDirection, isStroked);
        }

        public void EndFigure(bool isClosed) => _inner.EndFigure(isClosed);

        public void SetFillRule(FillRule fillRule) => _inner.SetFillRule(fillRule);

        // The inner context is owned by the caller's using-block; this decorator does not dispose it.
        public void Dispose() { }
    }
}
