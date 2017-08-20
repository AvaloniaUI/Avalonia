// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;
using System.Linq;

namespace Avalonia
{
    /// <summary>
    /// Defines a rectangle that may be defined relative to a containing element.
    /// </summary>
    public struct RelativeRect : IEquatable<RelativeRect>
    {
        /// <summary>
        /// A rectangle that represents 100% of an area.
        /// </summary>
        public static readonly RelativeRect Fill = new RelativeRect(0, 0, 1, 1, RelativeUnit.Relative);

        /// <summary>
        /// Initializes a new instance of the <see cref="RelativeRect"/> structure.
        /// </summary>
        /// <param name="x">The X position.</param>
        /// <param name="y">The Y position.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="unit">The unit of the rect.</param>
        public RelativeRect(double x, double y, double width, double height, RelativeUnit unit)
        {
            Rect = new Rect(x, y, width, height);
            Unit = unit;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelativeRect"/> structure.
        /// </summary>
        /// <param name="rect">The rectangle.</param>
        /// <param name="unit">The unit of the rect.</param>
        public RelativeRect(Rect rect, RelativeUnit unit)
        {
            Rect = rect;
            Unit = unit;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelativeRect"/> structure.
        /// </summary>
        /// <param name="size">The size of the rectangle.</param>
        /// <param name="unit">The unit of the rect.</param>
        public RelativeRect(Size size, RelativeUnit unit)
        {
            Rect = new Rect(size);
            Unit = unit;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelativeRect"/> structure.
        /// </summary>
        /// <param name="position">The position of the rectangle.</param>
        /// <param name="size">The size of the rectangle.</param>
        /// <param name="unit">The unit of the rect.</param>
        public RelativeRect(Point position, Size size, RelativeUnit unit)
        {
            Rect = new Rect(position, size);
            Unit = unit;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelativeRect"/> structure.
        /// </summary>
        /// <param name="topLeft">The top left position of the rectangle.</param>
        /// <param name="bottomRight">The bottom right position of the rectangle.</param>
        /// <param name="unit">The unit of the rect.</param>
        public RelativeRect(Point topLeft, Point bottomRight, RelativeUnit unit)
        {
            Rect = new Rect(topLeft, bottomRight);
            Unit = unit;
        }

        /// <summary>
        /// Gets the unit of the rectangle.
        /// </summary>
        public RelativeUnit Unit { get; }

        /// <summary>
        /// Gets the rectangle.
        /// </summary>
        public Rect Rect { get; }

        /// <summary>
        /// Checks for equality between two <see cref="RelativeRect"/>s.
        /// </summary>
        /// <param name="left">The first rectangle.</param>
        /// <param name="right">The second rectangle.</param>
        /// <returns>True if the rectangles are equal; otherwise false.</returns>
        public static bool operator ==(RelativeRect left, RelativeRect right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Checks for unequality between two <see cref="RelativeRect"/>s.
        /// </summary>
        /// <param name="left">The first rectangle.</param>
        /// <param name="right">The second rectangle.</param>
        /// <returns>True if the rectangles are unequal; otherwise false.</returns>
        public static bool operator !=(RelativeRect left, RelativeRect right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Checks if the <see cref="RelativeRect"/> equals another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if the objects are equal, otherwise false.</returns>
        public override bool Equals(object obj)
        {
            return (obj is RelativeRect) && Equals((RelativeRect)obj);
        }

        /// <summary>
        /// Checks if the <see cref="RelativeRect"/> equals another rectangle.
        /// </summary>
        /// <param name="p">The other rectangle.</param>
        /// <returns>True if the objects are equal, otherwise false.</returns>
        public bool Equals(RelativeRect p)
        {
            return Unit == p.Unit && Rect == p.Rect;
        }

        /// <summary>
        /// Gets a hashcode for a <see cref="RelativeRect"/>.
        /// </summary>
        /// <returns>A hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 23) + Unit.GetHashCode();
                hash = (hash * 23) + Rect.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Converts a <see cref="RelativeRect"/> into pixels.
        /// </summary>
        /// <param name="size">The size of the visual.</param>
        /// <returns>The origin point in pixels.</returns>
        public Rect ToPixels(Size size)
        {
            return Unit == RelativeUnit.Absolute ?
                Rect :
                new Rect(
                    Rect.X * size.Width,
                    Rect.Y * size.Height,
                    Rect.Width * size.Width,
                    Rect.Height * size.Height);
        }

        /// <summary>
        /// Parses a <see cref="RelativeRect"/> string.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <param name="culture">The current culture.</param>
        /// <returns>The parsed <see cref="RelativeRect"/>.</returns>
        public static RelativeRect Parse(string s, CultureInfo culture)
        {
            var parts = s.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .ToList();

            if (parts.Count == 4)
            {
                var unit = RelativeUnit.Absolute;
                var scale = 1.0;

                if (parts[0].EndsWith("%"))
                {
                    if (!parts[1].EndsWith("%") 
                        || !parts[2].EndsWith("%")
                        || !parts[3].EndsWith("%"))
                    {
                        throw new FormatException("If one coordinate is relative, all other must be too.");
                    }

                    parts[0] = parts[0].TrimEnd('%');
                    parts[1] = parts[1].TrimEnd('%');
                    parts[2] = parts[2].TrimEnd('%');
                    parts[3] = parts[3].TrimEnd('%');
                    unit = RelativeUnit.Relative;
                    scale = 0.01;
                }

                return new RelativeRect(
                    double.Parse(parts[0], culture) * scale,
                    double.Parse(parts[1], culture) * scale,
                    double.Parse(parts[2], culture) * scale,
                    double.Parse(parts[3], culture) * scale,
                    unit);
            }
            else
            {
                throw new FormatException("Invalid RelativeRect.");
            }
        }
    }
}
