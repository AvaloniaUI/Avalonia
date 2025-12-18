using System;
using System.Globalization;
using System.Runtime.CompilerServices;
#if !BUILDTASK
using Avalonia.Animation.Animators;
#endif
using Avalonia.Utilities;

namespace Avalonia
{
    /// <summary>
    /// Defines the reference point units of an <see cref="RelativePoint"/> or 
    /// <see cref="RelativeRect"/>.
    /// </summary>
#if !BUILDTASK
    public
#endif
    enum RelativeUnit
    {
        /// <summary>
        /// The point is expressed as a fraction of the containing element's size.
        /// </summary>
        Relative,

        /// <summary>
        /// The point is absolute (i.e. in pixels).
        /// </summary>
        Absolute,
    }

    /// <summary>
    /// Defines a point that may be defined relative to a containing element.
    /// </summary>
#if !BUILDTASK
    public
#endif
    readonly struct RelativePoint : IEquatable<RelativePoint>
    {
        /// <summary>
        /// A point at the top left of the containing element.
        /// </summary>
        public static readonly RelativePoint TopLeft = new RelativePoint(0, 0, RelativeUnit.Relative);

        /// <summary>
        /// A point at the center of the containing element.
        /// </summary>
        public static readonly RelativePoint Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);

        /// <summary>
        /// A point at the bottom right of the containing element.
        /// </summary>
        public static readonly RelativePoint BottomRight = new RelativePoint(1, 1, RelativeUnit.Relative);

        private readonly Point _point;

        private readonly RelativeUnit _unit;

        /// <summary>
        /// Initializes a new instance of the <see cref="RelativePoint"/> struct.
        /// </summary>
        /// <param name="x">The X point.</param>
        /// <param name="y">The Y point</param>
        /// <param name="unit">The unit.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RelativePoint(double x, double y, RelativeUnit unit)
            : this(new Point(x, y), unit)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelativePoint"/> struct.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="unit">The unit.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RelativePoint(Point point, RelativeUnit unit)
        {
            _point = point;
            _unit = unit;
        }

        /// <summary>
        /// Gets the point.
        /// </summary>
        public Point Point => _point;

        /// <summary>
        /// Gets the unit.
        /// </summary>
        public RelativeUnit Unit => _unit;

        /// <summary>
        /// Checks for equality between two <see cref="RelativePoint"/>s.
        /// </summary>
        /// <param name="left">The first point.</param>
        /// <param name="right">The second point.</param>
        /// <returns>True if the points are equal; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(RelativePoint left, RelativePoint right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Checks for inequality between two <see cref="RelativePoint"/>s.
        /// </summary>
        /// <param name="left">The first point.</param>
        /// <param name="right">The second point.</param>
        /// <returns>True if the points are unequal; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(RelativePoint left, RelativePoint right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Checks if the <see cref="RelativePoint"/> equals another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if the objects are equal, otherwise false.</returns>
        public override bool Equals(object? obj) => obj is RelativePoint other && Equals(other);

        /// <summary>
        /// Checks if the <see cref="RelativePoint"/> equals another point.
        /// </summary>
        /// <param name="p">The other point.</param>
        /// <returns>True if the objects are equal, otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(RelativePoint p)
        {
            return Unit == p.Unit && Point == p.Point;
        }

        /// <summary>
        /// Gets a hashcode for a <see cref="RelativePoint"/>.
        /// </summary>
        /// <returns>A hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (_point.GetHashCode() * 397) ^ (int)_unit;
            }
        }

        /// <summary>
        /// Converts a <see cref="RelativePoint"/> into pixels.
        /// </summary>
        /// <param name="size">The size of the visual.</param>
        /// <returns>The origin point in pixels.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Point ToPixels(Size size)
        {
            return _unit == RelativeUnit.Absolute ?
                _point :
                new Point(_point.X * size.Width, _point.Y * size.Height);
        }
        
#if !BUILDTASK
        /// <summary>
        /// Converts a <see cref="RelativePoint"/> into pixels.
        /// </summary>
        /// <param name="rect">The bounding box of the rendering primitive.</param>
        /// <returns>The origin point in pixels.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Point ToPixels(Rect rect)
        {
            return _unit == RelativeUnit.Absolute
                ? _point
                : new Point(rect.X + _point.X * rect.Width, rect.Y + _point.Y * rect.Height);
        }
#endif
        /// <summary>
        /// Parses a <see cref="RelativePoint"/> string.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>The parsed <see cref="RelativePoint"/>.</returns>
        public static RelativePoint Parse(string s)
        {
            const char percentChar = '%';
            
            using (var tokenizer = new SpanStringTokenizer(s, CultureInfo.InvariantCulture, exceptionMessage: "Invalid RelativePoint."))
            {
                var x = tokenizer.ReadSpan();
                var y = tokenizer.ReadSpan();

                var unit = RelativeUnit.Absolute;
                var scale = 1.0;

                var xRelative = x.Length > 0 && x[x.Length - 1] == percentChar;
                var yRelative = y.Length > 0 && y[y.Length - 1] == percentChar;

                if (xRelative && yRelative)
                {
                    x = x.Slice(0, x.Length - 1);
                    y = y.Slice(0, y.Length - 1);
                    unit = RelativeUnit.Relative;
                    scale = 0.01;
                }
                else if (xRelative || yRelative)
                {
                    throw new FormatException("If one coordinate is relative, both must be.");
                }

                return new RelativePoint(
                    SpanHelpers.ParseDouble(x, CultureInfo.InvariantCulture) * scale,
                    SpanHelpers.ParseDouble(y, CultureInfo.InvariantCulture) * scale,
                    unit);
            }
        }

        /// <summary>
        /// Returns a String representing this RelativePoint instance.
        /// </summary>
        /// <returns>The string representation.</returns>
        public override string ToString()
        {
            return _unit == RelativeUnit.Absolute ?
                _point.ToString() :
                string.Format(CultureInfo.InvariantCulture, "{0}%, {1}%", _point.X * 100, _point.Y * 100);
        }
    }
}
