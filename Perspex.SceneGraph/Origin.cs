// -----------------------------------------------------------------------
// <copyright file="Origin.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System;
    using System.Globalization;

    public enum OriginUnit
    {
        Percent,
        Pixels,
    }

    public struct Origin
    {
        public static readonly Origin Default = new Origin(0.5, 0.5, OriginUnit.Percent);

        private Point point;

        private OriginUnit unit;

        public Origin(double x, double y, OriginUnit unit)
            : this(new Point(x, y), unit)
        {
        }

        public Origin(Point point, OriginUnit unit)
        {
            this.point = point;
            this.unit = unit;
        }

        public Point Point
        {
            get { return this.point; }
        }

        public OriginUnit Unit
        {
            get { return this.unit; }
        }

        public Point ToPixels(Size size)
        {
            return this.unit == OriginUnit.Pixels ?
                this.point :
                new Point(this.point.X * size.Width, this.point.Y * size.Height);
        }
    }
}
