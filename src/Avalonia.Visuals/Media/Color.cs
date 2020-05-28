using System;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Animation.Animators;

namespace Avalonia.Media
{
    /// <summary>
    /// An ARGB color.
    /// </summary>
    public readonly struct Color : IEquatable<Color>
    {
        static Color()
        {
            Animation.Animation.RegisterAnimator<ColorAnimator>(prop => typeof(Color).IsAssignableFrom(prop.PropertyType));
        }

        /// <summary>
        /// Gets the Alpha component of the color.
        /// </summary>
        public byte A { get; }

        /// <summary>
        /// Gets the Red component of the color.
        /// </summary>
        public byte R { get; }

        /// <summary>
        /// Gets the Green component of the color.
        /// </summary>
        public byte G { get; }

        /// <summary>
        /// Gets the Blue component of the color.
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
            if (s == null) throw new ArgumentNullException(nameof(s));
            if (s.Length == 0) throw new FormatException();

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

            var knownColor = KnownColors.GetKnownColor(s);

            if (knownColor != KnownColor.None)
            {
                return knownColor.ToColor();
            }

            throw new FormatException($"Invalid color string: '{s}'.");
        }

        /// <summary>
        /// Parses a color string.
        /// </summary>
        /// <param name="s">The color string.</param>
        /// <param name="color">The parsed color</param>
        /// <returns>The status of the operation.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, out Color color)
        {
            color = default;
            if (s == null)
                return false;
            if (s.Length == 0)
                return false;

            if (s[0] == '#')
            {
                var or = 0u;

                if (s.Length == 7)
                {
                    or = 0xff000000;
                }
                else if (s.Length != 9)
                {
                    return false;
                }

                if(!uint.TryParse(s.Slice(1).ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var parsed))
                    return false;
                color = FromUInt32(parsed| or);
                return true;
            }

            var knownColor = KnownColors.GetKnownColor(s.ToString());

            if (knownColor != KnownColor.None)
            {
                color = knownColor.ToColor();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns the string representation of the color.
        /// </summary>
        /// <returns>
        /// The string representation of the color.
        /// </returns>
        public override string ToString()
        {
            uint rgb = ToUint32();
            return KnownColors.GetKnownColorName(rgb) ?? $"#{rgb:x8}";
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
        /// Check if two colors are equal.
        /// </summary>
        public bool Equals(Color other)
        {
            return A == other.A && R == other.R && G == other.G && B == other.B;
        }

        public override bool Equals(object obj)
        {
            return obj is Color other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = A.GetHashCode();
                hashCode = (hashCode * 397) ^ R.GetHashCode();
                hashCode = (hashCode * 397) ^ G.GetHashCode();
                hashCode = (hashCode * 397) ^ B.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(Color left, Color right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Color left, Color right)
        {
            return !left.Equals(right);
        }
    }
}
