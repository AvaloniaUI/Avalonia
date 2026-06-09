using Avalonia.Platform;

namespace Avalonia.Media.Fonts
{
    /// <summary>
    /// An <see cref="IGeometryContext"/> decorator that forwards every call to an inner context while
    /// tallying the number of path segments emitted. Lets the outline build estimate its payload's
    /// retained size (the cache <see cref="GlyphCacheEntry.Cost"/>) as a by-product, with no second pass.
    /// </summary>
    internal sealed class SegmentCountingGeometryContext : IGeometryContext
    {
        private readonly IGeometryContext _inner;

        public SegmentCountingGeometryContext(IGeometryContext inner) => _inner = inner;

        /// <summary>The number of line / curve / arc segments emitted so far.</summary>
        public int SegmentCount { get; private set; }

        public void BeginFigure(Point startPoint, bool isFilled = true) => _inner.BeginFigure(startPoint, isFilled);

        public void LineTo(Point point, bool isStroked = true)
        {
            SegmentCount++;
            _inner.LineTo(point, isStroked);
        }

        public void CubicBezierTo(Point controlPoint1, Point controlPoint2, Point endPoint, bool isStroked = true)
        {
            SegmentCount++;
            _inner.CubicBezierTo(controlPoint1, controlPoint2, endPoint, isStroked);
        }

        public void QuadraticBezierTo(Point controlPoint, Point endPoint, bool isStroked = true)
        {
            SegmentCount++;
            _inner.QuadraticBezierTo(controlPoint, endPoint, isStroked);
        }

        public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc,
            SweepDirection sweepDirection, bool isStroked = true)
        {
            SegmentCount++;
            _inner.ArcTo(point, size, rotationAngle, isLargeArc, sweepDirection, isStroked);
        }

        public void EndFigure(bool isClosed) => _inner.EndFigure(isClosed);

        public void SetFillRule(FillRule fillRule) => _inner.SetFillRule(fillRule);

        // The inner context is owned by the caller's using-block; this decorator does not dispose it.
        public void Dispose() { }
    }
}
