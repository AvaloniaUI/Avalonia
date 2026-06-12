using System;
using Avalonia.Platform;

namespace Avalonia.Media.Fonts
{
    /// <summary>
    /// An <see cref="IGeometryContext"/> that accumulates the min/max of every point it is handed
    /// instead of building geometry — the shared bounds-only interpreter sink for the outline tables
    /// (CFF / CFF2 charstrings and <c>glyf</c> contours). Off-curve (control) points are included, so
    /// the result is the control-point bounding box, matching the semantics of the <c>glyf</c> header
    /// box. Lets a glyph's bounds be computed without a render backend.
    /// </summary>
    internal sealed class BoundsGeometryContext : IGeometryContext
    {
        private double _minX = double.MaxValue;
        private double _minY = double.MaxValue;
        private double _maxX = double.MinValue;
        private double _maxY = double.MinValue;
        private bool _hasPoints;

        private void Add(Point point)
        {
            _hasPoints = true;
            if (point.X < _minX) _minX = point.X;
            if (point.Y < _minY) _minY = point.Y;
            if (point.X > _maxX) _maxX = point.X;
            if (point.Y > _maxY) _maxY = point.Y;
        }

        /// <summary>The accumulated control-point bounding box, or the zero box for an empty glyph.</summary>
        public GlyphBounds ToGlyphBounds()
            => _hasPoints
                ? new GlyphBounds(ClampToShort(Math.Floor(_minX)), ClampToShort(Math.Floor(_minY)),
                    ClampToShort(Math.Ceiling(_maxX)), ClampToShort(Math.Ceiling(_maxY)))
                : default;

        private static short ClampToShort(double value) => (short)Math.Clamp(value, short.MinValue, short.MaxValue);

        public void BeginFigure(Point startPoint, bool isFilled = true) => Add(startPoint);

        public void LineTo(Point point, bool isStroked = true) => Add(point);

        public void CubicBezierTo(Point controlPoint1, Point controlPoint2, Point endPoint, bool isStroked = true)
        {
            Add(controlPoint1);
            Add(controlPoint2);
            Add(endPoint);
        }

        public void QuadraticBezierTo(Point controlPoint, Point endPoint, bool isStroked = true)
        {
            Add(controlPoint);
            Add(endPoint);
        }

        public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc,
            SweepDirection sweepDirection, bool isStroked = true) => Add(point);

        public void EndFigure(bool isClosed) { }

        public void SetFillRule(FillRule fillRule) { }

        public void Dispose() { }
    }
}
