// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Perspex
{
    /// <summary>
    /// Defines the reference point units of an <see cref="RelativePoint"/> or 
    /// <see cref="RelativeRect"/>.
    /// </summary>
    public enum RelativeUnit
    {
        /// <summary>
        /// The point is expressed as a percentage of the containing element's size.
        /// </summary>
        Percent,

        /// <summary>
        /// The origin's point is in pixels.
        /// </summary>
        Pixels,
    }

    /// <summary>
    /// Defines a point that may be defined relative to a containing element.
    /// </summary>
    public struct RelativePoint
    {
        /// <summary>
        /// A point at the top left of the containing element.
        /// </summary>
        public static readonly RelativePoint TopLeft = new RelativePoint(0, 0, RelativeUnit.Percent);

        /// <summary>
        /// A point at the center of the containing element.
        /// </summary>
        public static readonly RelativePoint Center = new RelativePoint(0.5, 0.5, RelativeUnit.Percent);

        /// <summary>
        /// A point at the bottom right of the containing element.
        /// </summary>
        public static readonly RelativePoint BottomRight = new RelativePoint(1, 1, RelativeUnit.Percent);

        private Point _point;

        private readonly RelativeUnit _unit;

        /// <summary>
        /// Initializes a new instance of the <see cref="RelativePoint"/> struct.
        /// </summary>
        /// <param name="x">The X point.</param>
        /// <param name="y">The Y point</param>
        /// <param name="unit">The unit.</param>
        public RelativePoint(double x, double y, RelativeUnit unit)
            : this(new Point(x, y), unit)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelativePoint"/> struct.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="unit">The unit.</param>
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
        /// Converts a <see cref="RelativePoint"/> into pixels.
        /// </summary>
        /// <param name="size">The size of the visual.</param>
        /// <returns>The origin point in pixels.</returns>
        public Point ToPixels(Size size)
        {
            return _unit == RelativeUnit.Pixels ?
                _point :
                new Point(_point.X * size.Width, _point.Y * size.Height);
        }
    }
}
