// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;
using Avalonia.Utilities;

namespace Avalonia
{
    /// <summary>
    /// Represents a point in device pixels.
    /// </summary>
    public readonly struct PixelPoint
    {
        /// <summary>
        /// A point representing 0,0.
        /// </summary>
        public static readonly PixelPoint Origin = new PixelPoint(0, 0);

        /// <summary>
        /// Initializes a new instance of the <see cref="PixelPoint"/> structure.
        /// </summary>
        /// <param name="x">The X co-ordinate.</param>
        /// <param name="y">The Y co-ordinate.</param>
        public PixelPoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Gets the X co-ordinate.
        /// </summary>
        public int X { get; }

        /// <summary>
        /// Gets the Y co-ordinate.
        /// </summary>
        public int Y { get; }

        /// <summary>
        /// Checks for equality between two <see cref="PixelPoint"/>s.
        /// </summary>
        /// <param name="left">The first point.</param>
        /// <param name="right">The second point.</param>
        /// <returns>True if the points are equal; otherwise false.</returns>
        public static bool operator ==(PixelPoint left, PixelPoint right)
        {
            return left.X == right.X && left.Y == right.Y;
        }

        /// <summary>
        /// Checks for inequality between two <see cref="PixelPoint"/>s.
        /// </summary>
        /// <param name="left">The first point.</param>
        /// <param name="right">The second point.</param>
        /// <returns>True if the points are unequal; otherwise false.</returns>
        public static bool operator !=(PixelPoint left, PixelPoint right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Parses a <see cref="PixelPoint"/> string.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>The <see cref="PixelPoint"/>.</returns>
        public static PixelPoint Parse(string s)
        {
            using (var tokenizer = new StringTokenizer(s, CultureInfo.InvariantCulture, exceptionMessage: "Invalid PixelPoint"))
            {
                return new PixelPoint(
                    tokenizer.ReadInt32(),
                    tokenizer.ReadInt32());
            }
        }

        /// <summary>
        /// Checks for equality between a point and an object.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>
        /// True if <paramref name="obj"/> is a point that equals the current point.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj is PixelPoint other)
            {
                return this == other;
            }

            return false;
        }

        /// <summary>
        /// Returns a hash code for a <see cref="PixelPoint"/>.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 23) + X.GetHashCode();
                hash = (hash * 23) + Y.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Returns a new <see cref="PixelPoint"/> with the same Y co-ordinate and the specified X co-ordinate.
        /// </summary>
        /// <param name="x">The X co-ordinate.</param>
        /// <returns>The new <see cref="PixelPoint"/>.</returns>
        public PixelPoint WithX(int x) => new PixelPoint(x, Y);

        /// <summary>
        /// Returns a new <see cref="PixelPoint"/> with the same X co-ordinate and the specified Y co-ordinate.
        /// </summary>
        /// <param name="y">The Y co-ordinate.</param>
        /// <returns>The new <see cref="PixelPoint"/>.</returns>
        public PixelPoint WithY(int y) => new PixelPoint(X, y);

        /// <summary>
        /// Converts the <see cref="PixelPoint"/> to a device-independent <see cref="Point"/> using the
        /// specified scaling factor.
        /// </summary>
        /// <param name="scale">The scaling factor.</param>
        /// <returns>The device-independent point.</returns>
        public Point ToPoint(double scale) => new Point(X / scale, Y / scale);

        /// <summary>
        /// Converts the <see cref="PixelPoint"/> to a device-independent <see cref="Point"/> using the
        /// specified scaling factor.
        /// </summary>
        /// <param name="scale">The scaling factor.</param>
        /// <returns>The device-independent point.</returns>
        public Point ToPoint(Vector scale) => new Point(X / scale.X, Y / scale.Y);

        /// <summary>
        /// Converts the <see cref="PixelPoint"/> to a device-independent <see cref="Point"/> using the
        /// specified dots per inch (DPI).
        /// </summary>
        /// <param name="dpi">The dots per inch of the device.</param>
        /// <returns>The device-independent point.</returns>
        public Point ToPointWithDpi(double dpi) => ToPoint(dpi / 96);

        /// <summary>
        /// Converts the <see cref="PixelPoint"/> to a device-independent <see cref="Point"/> using the
        /// specified dots per inch (DPI).
        /// </summary>
        /// <param name="dpi">The dots per inch of the device.</param>
        /// <returns>The device-independent point.</returns>
        public Point ToPointWithDpi(Vector dpi) => ToPoint(new Vector(dpi.X / 96, dpi.Y / 96));

        /// <summary>
        /// Converts a <see cref="Point"/> to device pixels using the specified scaling factor.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="scale">The scaling factor.</param>
        /// <returns>The device-independent point.</returns>
        public static PixelPoint FromPoint(Point point, double scale) => new PixelPoint(
            (int)(point.X * scale),
            (int)(point.Y * scale));

        /// <summary>
        /// Converts a <see cref="Point"/> to device pixels using the specified scaling factor.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="scale">The scaling factor.</param>
        /// <returns>The device-independent point.</returns>
        public static PixelPoint FromPoint(Point point, Vector scale) => new PixelPoint(
            (int)(point.X * scale.X),
            (int)(point.Y * scale.Y));

        /// <summary>
        /// Converts a <see cref="Point"/> to device pixels using the specified dots per inch (DPI).
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="dpi">The dots per inch of the device.</param>
        /// <returns>The device-independent point.</returns>
        public static PixelPoint FromPointWithDpi(Point point, double dpi) => FromPoint(point, dpi / 96);

        /// <summary>
        /// Converts a <see cref="Point"/> to device pixels using the specified dots per inch (DPI).
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="dpi">The dots per inch of the device.</param>
        /// <returns>The device-independent point.</returns>
        public static PixelPoint FromPointWithDpi(Point point, Vector dpi) => FromPoint(point, new Vector(dpi.X / 96, dpi.Y / 96));

        /// <summary>
        /// Returns the string representation of the point.
        /// </summary>
        /// <returns>The string representation of the point.</returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}, {1}", X, Y);
        }
    }
}
