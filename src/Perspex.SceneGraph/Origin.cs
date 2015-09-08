// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Perspex
{
    /// <summary>
    /// Defines the reference point units of an <see cref="Origin"/>.
    /// </summary>
    public enum OriginUnit
    {
        /// <summary>
        /// The origin's point is a percentage.
        /// </summary>
        Percent,

        /// <summary>
        /// The origin's point is in pixels.
        /// </summary>
        Pixels,
    }

    /// <summary>
    /// Defines an origin for a <see cref="Perspex.Media.Transform"/>.
    /// </summary>
    public struct Origin
    {
        /// <summary>
        /// The default origin, which is the center of the control.
        /// </summary>
        public static readonly Origin Default = new Origin(0.5, 0.5, OriginUnit.Percent);

        private Point _point;

        private readonly OriginUnit _unit;

        /// <summary>
        /// Initializes a new instance of the <see cref="Origin"/> struct.
        /// </summary>
        /// <param name="x">The X point.</param>
        /// <param name="y">The Y point</param>
        /// <param name="unit">The origin unit.</param>
        public Origin(double x, double y, OriginUnit unit)
            : this(new Point(x, y), unit)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Origin"/> struct.
        /// </summary>
        /// <param name="point">The origin point.</param>
        /// <param name="unit">The origin unit.</param>
        public Origin(Point point, OriginUnit unit)
        {
            _point = point;
            _unit = unit;
        }

        /// <summary>
        /// Gets the origin point.
        /// </summary>
        public Point Point => _point;

        /// <summary>
        /// Gets the origin unit.
        /// </summary>
        public OriginUnit Unit => _unit;

        /// <summary>
        /// Converts an <see cref="Origin"/> into pixels.
        /// </summary>
        /// <param name="size">The size of the visual.</param>
        /// <returns>The origin point in pixels.</returns>
        public Point ToPixels(Size size)
        {
            return _unit == OriginUnit.Pixels ?
                _point :
                new Point(_point.X * size.Width, _point.Y * size.Height);
        }
    }
}
