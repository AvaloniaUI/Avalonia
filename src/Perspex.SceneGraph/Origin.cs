﻿// -----------------------------------------------------------------------
// <copyright file="Origin.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

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

        private Point point;

        private OriginUnit unit;

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
            this.point = point;
            this.unit = unit;
        }

        /// <summary>
        /// Gets the origin point.
        /// </summary>
        public Point Point
        {
            get { return this.point; }
        }

        /// <summary>
        /// Gets the origin unit.
        /// </summary>
        public OriginUnit Unit
        {
            get { return this.unit; }
        }

        /// <summary>
        /// Converts an <see cref="Origin"/> into pixels.
        /// </summary>
        /// <param name="size">The size of the visual.</param>
        /// <returns>The origin point in pixels.</returns>
        public Point ToPixels(Size size)
        {
            return this.unit == OriginUnit.Pixels ?
                this.point :
                new Point(this.point.X * size.Width, this.point.Y * size.Height);
        }
    }
}
