// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;
using System.Linq;

namespace Avalonia
{
    /// <summary>
    /// Describes the thickness of a frame around a rectangle.
    /// </summary>
    public struct Thickness
    {
        /// <summary>
        /// The thickness on the left.
        /// </summary>
        private readonly double _left;

        /// <summary>
        /// The thickness on the top.
        /// </summary>
        private readonly double _top;

        /// <summary>
        /// The thickness on the right.
        /// </summary>
        private readonly double _right;

        /// <summary>
        /// The thickness on the bottom.
        /// </summary>
        private readonly double _bottom;

        /// <summary>
        /// Initializes a new instance of the <see cref="Thickness"/> structure.
        /// </summary>
        /// <param name="uniformLength">The length that should be applied to all sides.</param>
        public Thickness(double uniformLength)
        {
            _left = _top = _right = _bottom = uniformLength;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Thickness"/> structure.
        /// </summary>
        /// <param name="horizontal">The thickness on the left and right.</param>
        /// <param name="vertical">The thickness on the top and bottom.</param>
        public Thickness(double horizontal, double vertical)
        {
            _left = _right = horizontal;
            _top = _bottom = vertical;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Thickness"/> structure.
        /// </summary>
        /// <param name="left">The thickness on the left.</param>
        /// <param name="top">The thickness on the top.</param>
        /// <param name="right">The thickness on the right.</param>
        /// <param name="bottom">The thickness on the bottom.</param>
        public Thickness(double left, double top, double right, double bottom)
        {
            _left = left;
            _top = top;
            _right = right;
            _bottom = bottom;
        }

        /// <summary>
        /// Gets the thickness on the left.
        /// </summary>
        public double Left => _left;

        /// <summary>
        /// Gets the thickness on the top.
        /// </summary>
        public double Top => _top;

        /// <summary>
        /// Gets the thickness on the right.
        /// </summary>
        public double Right => _right;

        /// <summary>
        /// Gets the thickness on the bottom.
        /// </summary>
        public double Bottom => _bottom;

        /// <summary>
        /// Gets a value indicating whether all sides are set to 0.
        /// </summary>
        public bool IsEmpty => Left == 0 && Top == 0 && Right == 0 && Bottom == 0;

        /// <summary>
        /// Compares two Thicknesses.
        /// </summary>
        /// <param name="a">The first thickness.</param>
        /// <param name="b">The second thickness.</param>
        /// <returns>The equality.</returns>
        public static bool operator ==(Thickness a, Thickness b)
        {
            return a.Equals(b);
        }

        /// <summary>
        /// Compares two Thicknesses.
        /// </summary>
        /// <param name="a">The first thickness.</param>
        /// <param name="b">The second thickness.</param>
        /// <returns>The unequality.</returns>
        public static bool operator !=(Thickness a, Thickness b)
        {
            return !a.Equals(b);
        }

        /// <summary>
        /// Adds two Thicknesses.
        /// </summary>
        /// <param name="a">The first thickness.</param>
        /// <param name="b">The second thickness.</param>
        /// <returns>The equality.</returns>
        public static Thickness operator +(Thickness a, Thickness b)
        {
            return new Thickness(
                a.Left + b.Left,
                a.Top + b.Top,
                a.Right + b.Right,
                a.Bottom + b.Bottom);
        }

        /// <summary>
        /// Adds a Thickness to a Size.
        /// </summary>
        /// <param name="size">The size.</param>
        /// <param name="thickness">The thickness.</param>
        /// <returns>The equality.</returns>
        public static Size operator +(Size size, Thickness thickness)
        {
            return new Size(
                size.Width + thickness.Left + thickness.Right,
                size.Height + thickness.Top + thickness.Bottom);
        }

        /// <summary>
        /// Subtracts a Thickness from a Size.
        /// </summary>
        /// <param name="size">The size.</param>
        /// <param name="thickness">The thickness.</param>
        /// <returns>The equality.</returns>
        public static Size operator -(Size size, Thickness thickness)
        {
            return new Size(
                size.Width - (thickness.Left + thickness.Right),
                size.Height - (thickness.Top + thickness.Bottom));
        }

        /// <summary>
        /// Parses a <see cref="Thickness"/> string.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <param name="culture">The current culture.</param>
        /// <returns>The <see cref="Thickness"/>.</returns>
        public static Thickness Parse(string s, CultureInfo culture)
        {
            var parts = s.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .ToList();

            switch (parts.Count)
            {
                case 1:
                    var uniform = double.Parse(parts[0], culture);
                    return new Thickness(uniform);
                case 2:
                    var horizontal = double.Parse(parts[0], culture);
                    var vertical = double.Parse(parts[1], culture);
                    return new Thickness(horizontal, vertical);
                case 4:
                    var left = double.Parse(parts[0], culture);
                    var top = double.Parse(parts[1], culture);
                    var right = double.Parse(parts[2], culture);
                    var bottom = double.Parse(parts[3], culture);
                    return new Thickness(left, top, right, bottom);
            }

            throw new FormatException("Invalid Thickness.");
        }

        /// <summary>
        /// Checks for equality between a thickness and an object.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>
        /// True if <paramref name="obj"/> is a size that equals the current size.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj is Thickness)
            {
                Thickness other = (Thickness)obj;
                return Left == other.Left &&
                       Top == other.Top &&
                       Right == other.Right &&
                       Bottom == other.Bottom;
            }

            return false;
        }

        /// <summary>
        /// Returns a hash code for a <see cref="Thickness"/>.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 23) + Left.GetHashCode();
                hash = (hash * 23) + Top.GetHashCode();
                hash = (hash * 23) + Right.GetHashCode();
                hash = (hash * 23) + Bottom.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Returns the string representation of the thickness.
        /// </summary>
        /// <returns>The string representation of the thickness.</returns>
        public override string ToString()
        {
            return $"{_left},{_top},{_right},{_bottom}";
        }
    }
}
