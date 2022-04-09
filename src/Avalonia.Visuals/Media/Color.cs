// Color conversion portions of this source file are adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
// and the Windows Community Toolkit project.
// (https://github.com/CommunityToolkit/WindowsCommunityToolkit)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
using System.Globalization;
#if !BUILDTASK
using Avalonia.Animation.Animators;
#endif

namespace Avalonia.Media
{
    /// <summary>
    /// An ARGB color.
    /// </summary>
#if !BUILDTASK
    public
#endif
    readonly struct Color : IEquatable<Color>
    {
        static Color()
        {
#if !BUILDTASK
            Animation.Animation.RegisterAnimator<ColorAnimator>(prop => typeof(Color).IsAssignableFrom(prop.PropertyType));
#endif
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
            if (s is null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            if (TryParse(s, out Color color))
            {
                return color;
            }

            throw new FormatException($"Invalid color string: '{s}'.");
        }

        /// <summary>
        /// Parses a color string.
        /// </summary>
        /// <param name="s">The color string.</param>
        /// <returns>The <see cref="Color"/>.</returns>
        public static Color Parse(ReadOnlySpan<char> s)
        {
            if (TryParse(s, out Color color))
            {
                return color;
            }

            throw new FormatException($"Invalid color string: '{s.ToString()}'.");
        }

        /// <summary>
        /// Parses a color string.
        /// </summary>
        /// <param name="s">The color string.</param>
        /// <param name="color">The parsed color</param>
        /// <returns>The status of the operation.</returns>
        public static bool TryParse(string s, out Color color)
        {
            color = default;

            if (s is null)
            {
                return false;
            }

            if (s.Length == 0)
            {
                return false;
            }

            if (s[0] == '#' && TryParseInternal(s.AsSpan(), out color))
            {
                return true;
            }

            var knownColor = KnownColors.GetKnownColor(s);

            if (knownColor != KnownColor.None)
            {
                color = knownColor.ToColor();

                return true;
            }

            return false;
        }

        /// <summary>
        /// Parses a color string.
        /// </summary>
        /// <param name="s">The color string.</param>
        /// <param name="color">The parsed color</param>
        /// <returns>The status of the operation.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, out Color color)
        {
            if (s.Length == 0)
            {
                color = default;

                return false;
            }

            if (s[0] == '#')
            {
                return TryParseInternal(s, out color);
            }

            var knownColor = KnownColors.GetKnownColor(s.ToString());

            if (knownColor != KnownColor.None)
            {
                color = knownColor.ToColor();

                return true;
            }

            color = default;

            return false;
        }

        private static bool TryParseInternal(ReadOnlySpan<char> s, out Color color)
        {
            static bool TryParseCore(ReadOnlySpan<char> input, ref Color color)
            {
                var alphaComponent = 0u;

                if (input.Length == 6)
                {
                    alphaComponent = 0xff000000;
                }
                else if (input.Length != 8)
                {
                    return false;
                }

                // TODO: (netstandard 2.1) Can use allocation free parsing.
                if (!uint.TryParse(input.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture,
                    out var parsed))
                {
                    return false;
                }

                color = FromUInt32(parsed | alphaComponent);

                return true;
            }

            color = default;

            ReadOnlySpan<char> input = s.Slice(1);

            // Handle shorthand cases like #FFF (RGB) or #FFFF (ARGB).
            if (input.Length == 3 || input.Length == 4)
            {
                var extendedLength = 2 * input.Length;
                
#if !BUILDTASK
                Span<char> extended = stackalloc char[extendedLength];
#else
                char[] extended = new char[extendedLength];
#endif

                for (int i = 0; i < input.Length; i++)
                {
                    extended[2 * i + 0] = input[i];
                    extended[2 * i + 1] = input[i];
                }

                return TryParseCore(extended, ref color);
            }

            return TryParseCore(input, ref color);
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
            return KnownColors.GetKnownColorName(rgb) ?? $"#{rgb.ToString("x8", CultureInfo.InvariantCulture)}";
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
        /// Returns the HSV color model equivalent of this RGB color.
        /// </summary>
        /// <returns>The HSV equivalent color.</returns>
        public HsvColor ToHsv()
        {
            // Use the by-component conversion method directly for performance
            // Don't use the HsvColor(Color) constructor to avoid an extra HsvColor
            return Color.ToHsv(R, G, B, A);
        }

        /// <inheritdoc/>
        public bool Equals(Color other)
        {
            return A == other.A && R == other.R && G == other.G && B == other.B;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is Color other && Equals(other);
        }

        /// <inheritdoc/>
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

        /// <summary>
        /// Converts the given RGB color to it's HSV color equivalent.
        /// </summary>
        /// <param name="color">The color in the RGB color model.</param>
        /// <returns>A new <see cref="HsvColor"/> equivalent to the given RGBA values.</returns>
        public static HsvColor ToHsv(Color color)
        {
            return Color.ToHsv(color.R, color.G, color.B, color.A);
        }

        /// <summary>
        /// Converts the given RGBA color component values to its HSV color equivalent.
        /// </summary>
        /// <param name="red">The red component in the RGB color model.</param>
        /// <param name="green">The green component in the RGB color model.</param>
        /// <param name="blue">The blue component in the RGB color model.</param>
        /// <param name="alpha">The alpha component.</param>
        /// <returns>A new <see cref="HsvColor"/> equivalent to the given RGBA values.</returns>
        public static HsvColor ToHsv(
            byte red,
            byte green,
            byte blue,
            byte alpha = 0xFF)
        {
            // Note: Conversion code is originally based on the C++ in WinUI (licensed MIT)
            // https://github.com/microsoft/microsoft-ui-xaml/blob/main/dev/Common/ColorConversion.cpp
            // This was used because it is the best documented and likely most optimized for performance
            // Alpha support was added

            // Normalize RGBA components into the 0..1 range used by this algorithm
            double r = red / 255.0;
            double g = green / 255.0;
            double b = blue / 255.0;
            double a = alpha / 255.0;

            double hue;
            double saturation;
            double value;

            double max = r >= g ? (r >= b ? r : b) : (g >= b ? g : b);
            double min = r <= g ? (r <= b ? r : b) : (g <= b ? g : b);

            // The value, a number between 0 and 1, is the largest of R, G, and B (divided by 255).
            // Conceptually speaking, it represents how much color is present.
            // If at least one of R, G, B is 255, then there exists as much color as there can be.
            // If RGB = (0, 0, 0), then there exists no color at all - a value of zero corresponds
            // to black (i.e., the absence of any color).
            value = max;

            // The "chroma" of the color is a value directly proportional to the extent to which
            // the color diverges from greyscale.  If, for example, we have RGB = (255, 255, 0),
            // then the chroma is maximized - this is a pure yellow, no gray of any kind.
            // On the other hand, if we have RGB = (128, 128, 128), then the chroma being zero
            // implies that this color is pure greyscale, with no actual hue to be found.
            var chroma = max - min;

            // If the chrome is zero, then hue is technically undefined - a greyscale color
            // has no hue.  For the sake of convenience, we'll just set hue to zero, since
            // it will be unused in this circumstance.  Since the color is purely gray,
            // saturation is also equal to zero - you can think of saturation as basically
            // a measure of hue intensity, such that no hue at all corresponds to a
            // nonexistent intensity.
            if (chroma == 0)
            {
                hue = 0.0;
                saturation = 0.0;
            }
            else
            {
                // In this block, hue is properly defined, so we'll extract both hue
                // and saturation information from the RGB color.

                // Hue can be thought of as a cyclical thing, between 0 degrees and 360 degrees.
                // A hue of 0 degrees is red; 120 degrees is green; 240 degrees is blue; and 360 is back to red.
                // Every other hue is somewhere between either red and green, green and blue, and blue and red,
                // so every other hue can be thought of as an angle on this color wheel.
                // These if/else statements determines where on this color wheel our color lies.
                if (r == max)
                {
                    // If the red channel is the most pronounced channel, then we exist
                    // somewhere between (-60, 60) on the color wheel - i.e., the section around 0 degrees
                    // where red dominates.  We figure out where in that section we are exactly
                    // by considering whether the green or the blue channel is greater - by subtracting green from blue,
                    // then if green is greater, we'll nudge ourselves closer to 60, whereas if blue is greater, then
                    // we'll nudge ourselves closer to -60.  We then divide by chroma (which will actually make the result larger,
                    // since chroma is a value between 0 and 1) to normalize the value to ensure that we get the right hue
                    // even if we're very close to greyscale.
                    hue = 60 * (g - b) / chroma;
                }
                else if (g == max)
                {
                    // We do the exact same for the case where the green channel is the most pronounced channel,
                    // only this time we want to see if we should tilt towards the blue direction or the red direction.
                    // We add 120 to center our value in the green third of the color wheel.
                    hue = 120 + (60 * (b - r) / chroma);
                }
                else // blue == max
                {
                    // And we also do the exact same for the case where the blue channel is the most pronounced channel,
                    // only this time we want to see if we should tilt towards the red direction or the green direction.
                    // We add 240 to center our value in the blue third of the color wheel.
                    hue = 240 + (60 * (r - g) / chroma);
                }

                // Since we want to work within the range [0, 360), we'll add 360 to any value less than zero -
                // this will bump red values from within -60 to -1 to 300 to 359.  The hue is the same at both values.
                if (hue < 0.0)
                {
                    hue += 360.0;
                }

                // The saturation, our final HSV axis, can be thought of as a value between 0 and 1 indicating how intense our color is.
                // To find it, we divide the chroma - the distance between the minimum and the maximum RGB channels - by the maximum channel (i.e., the value).
                // This effectively normalizes the chroma - if the maximum is 0.5 and the minimum is 0, the saturation will be (0.5 - 0) / 0.5 = 1,
                // meaning that although this color is not as bright as it can be, the dark color is as intense as it possibly could be.
                // If, on the other hand, the maximum is 0.5 and the minimum is 0.25, then the saturation will be (0.5 - 0.25) / 0.5 = 0.5,
                // meaning that this color is partially washed out.
                // A saturation value of 0 corresponds to a greyscale color, one in which the color is *completely* washed out and there is no actual hue.
                saturation = chroma / value;
            }

            return new HsvColor(a, hue, saturation, value, false);
        }

        /// <summary>
        /// Indicates whether the values of two specified <see cref="Color"/> objects are equal.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns>True if left and right are equal; otherwise, false.</returns>
        public static bool operator ==(Color left, Color right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Indicates whether the values of two specified <see cref="Color"/> objects are not equal.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns>True if left and right are not equal; otherwise, false.</returns>
        public static bool operator !=(Color left, Color right)
        {
            return !left.Equals(right);
        }
    }
}
