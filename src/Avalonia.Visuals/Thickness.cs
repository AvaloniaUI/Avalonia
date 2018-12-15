// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;
using Avalonia.Animation;
using Avalonia.Animation.Animators;
using Avalonia.Utilities;

namespace Avalonia
{
    /// <summary>
    /// Describes the thickness of a frame around a rectangle.
    /// </summary>
    public readonly struct Thickness
    {
        static Thickness()
        {
            Animation.Animation.RegisterAnimator<ThicknessAnimator>(prop => typeof(Thickness).IsAssignableFrom(prop.PropertyType));
        }

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
        public bool IsEmpty => Left.Equals(0) && IsUniform;

        /// <summary>
        /// Gets a value indicating whether all sides are equal.
        /// </summary>
        public bool IsUniform => Left.Equals(Right) && Top.Equals(Bottom) && Right.Equals(Bottom);

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
        /// <returns>The inequality.</returns>
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
        /// Subtracts two Thicknesses.
        /// </summary>
        /// <param name="a">The first thickness.</param>
        /// <param name="b">The second thickness.</param>
        /// <returns>The equality.</returns>
        public static Thickness operator -(Thickness a, Thickness b)
        {
            return new Thickness(
                a.Left - b.Left,
                a.Top - b.Top,
                a.Right - b.Right,
                a.Bottom - b.Bottom);
        }

        /// <summary>
        /// Multiplies a Thickness to a scalar.
        /// </summary>
        /// <param name="a">The thickness.</param>
        /// <param name="b">The scalar.</param>
        /// <returns>The equality.</returns>
        public static Thickness operator *(Thickness a, double b)
        {
            return new Thickness(
                a.Left * b,
                a.Top * b,
                a.Right * b,
                a.Bottom * b);
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
        /// <returns>The <see cref="Thickness"/>.</returns>
        public static Thickness Parse(string s)
        {
            using (var tokenizer = new StringTokenizer(s, CultureInfo.InvariantCulture, exceptionMessage: "Invalid Thickness"))
            {
                if (tokenizer.TryReadDouble(out var a))
                {
                    if (tokenizer.TryReadDouble(out var b))
                    {
                        if (tokenizer.TryReadDouble(out var c))
                        {
                            return new Thickness(a, b, c, tokenizer.ReadDouble());
                        }

                        return new Thickness(a, b);
                    }

                    return new Thickness(a);
                }

                throw new FormatException("Invalid Thickness.");
            }
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
