// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Avalonia.Media
{
    /// <summary>
    /// An ARGB color.
    /// </summary>
    public struct Color : IEquatable<Color>
    {
        /// <summary>
        /// Gets or sets the Alpha component of the color.
        /// </summary>
        public byte A { get; }

        /// <summary>
        /// Gets or sets the Red component of the color.
        /// </summary>
        public byte R { get; }

        /// <summary>
        /// Gets or sets the Green component of the color.
        /// </summary>
        public byte G { get; }

        /// <summary>
        /// Gets or sets the Blue component of the color.
        /// </summary>
        public byte B { get; }

        public Color(byte a, byte r, byte g, byte b)
        {
            A = a;
            R = r;
            G = g;
            B = b;
        }

        /// <summary>
        /// Creates a <see cref="Color"/> from alpha, red, green and blue components.
        /// </summary>
        /// <param name="a">The alpha component.</param>
        /// <param name="r">The red component.</param>
        /// <param name="g">The green component.</param>
        /// <param name="b">The blue component.</param>
        /// <returns>The color.</returns>
        public static Color FromArgb(byte a, byte r, byte g, byte b)
        {
            return new Color(a, r, g, b);
        }

        /// <summary>
        /// Creates a <see cref="Color"/> from red, green and blue components.
        /// </summary>
        /// <param name="r">The red component.</param>
        /// <param name="g">The green component.</param>
        /// <param name="b">The blue component.</param>
        /// <returns>The color.</returns>
        public static Color FromRgb(byte r, byte g, byte b)
        {
            return new Color(0xff, r, g, b);
        }

        /// <summary>
        /// Creates a <see cref="Color"/> from an integer.
        /// </summary>
        /// <param name="value">The integer value.</param>
        /// <returns>The color.</returns>
        public static Color FromUInt32(uint value)
        {
            return new Color(
                (byte)((value >> 24) & 0xff),
                (byte)((value >> 16) & 0xff),
                (byte)((value >> 8) & 0xff),
                (byte)(value & 0xff)
            );
        }

        /// <summary>
        /// Parses a color string.
        /// </summary>
        /// <param name="s">The color string.</param>
        /// <returns>The <see cref="Color"/>.</returns>
        public static Color Parse(string s)
        {
            if (s[0] == '#')
            {
                var or = 0u;

                if (s.Length == 7)
                {
                    or = 0xff000000;
                }
                else if (s.Length != 9)
                {
                    throw new FormatException($"Invalid color string: '{s}'.");
                }

                return FromUInt32(uint.Parse(s.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture) | or);
            }
            else
            {
                var upper = s.ToUpperInvariant();
                var member = typeof(Colors).GetTypeInfo().DeclaredProperties
                    .FirstOrDefault(x => x.Name.ToUpperInvariant() == upper);

                if (member != null)
                {
                    return (Color)member.GetValue(null);
                }
                else
                {
                    throw new FormatException($"Invalid color string: '{s}'.");
                }
            }
        }

        /// <summary>
        /// Tests whether the specified object is a <see cref="Color"/> structure and is equal to
        /// this color.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>
        /// True if the specified object is a color and the colors are equal, otherwise false.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj is Color c)
            {
                return Equals(c);
            }

            return false;
        }

        /// <summary>
        /// Tests whether the specified <see cref="Color"/> structure is equal to this color.
        /// </summary>
        /// <param name="c">The color to compare.</param>
        /// <returns>True if the colors are equal, otherwise false.</returns>
        public bool Equals(Color c) => A == c.A && R == c.R && G == c.G && B == c.B;

        /// <summary>
        /// Gets a hash code for the <see cref="Color"/> structure.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() => unchecked((int)ToUint32());

        /// <summary>
        /// Returns the string representation of the color.
        /// </summary>
        /// <returns>
        /// The string representation of the color.
        /// </returns>
        public override string ToString()
        {
            uint rgb = ((uint)A << 24) | ((uint)R << 16) | ((uint)G << 8) | (uint)B;
            return $"#{rgb:x8}";
        }

        /// <summary>
        /// Returns the integer representation of the color.
        /// </summary>
        /// <returns>
        /// The integer representation of the color.
        /// </returns>
        public uint ToUint32()
        {
            return ((uint)A << 24) | ((uint)R << 16) | ((uint)G << 8) | (uint)B;
        }

        /// <summary>
        /// Tests whether two colors are equal.
        /// </summary>
        /// <param name="a">The first color.</param>
        /// <param name="b">The second color.</param>
        /// <returns>True if the colors are equal, otherwise false.</returns>
        public static bool operator ==(Color a, Color b) => a.Equals(b);

        /// <summary>
        /// Tests whether two colors are unequal.
        /// </summary>
        /// <param name="a">The first color.</param>
        /// <param name="b">The second color.</param>
        /// <returns>True if the colors are unequal, otherwise false.</returns>
        public static bool operator !=(Color a, Color b) => !a.Equals(b);
    }
}
