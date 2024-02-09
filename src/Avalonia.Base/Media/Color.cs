// Color conversion portions of this source file are adapted from the WinUI project
// (https://github.com/microsoft/microsoft-ui-xaml)
// and the Windows Community Toolkit project.
// (https://github.com/CommunityToolkit/WindowsCommunityToolkit)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
using System.ComponentModel;
using System.Globalization;
#if !BUILDTASK
using Avalonia.Animation.Animators;
#endif
using static Avalonia.Utilities.SpanHelpers;

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
        private const double byteToDouble = 1.0 / 255;

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

        /// <summary>
        /// Initializes a new instance of the <see cref="Color"/> struct.
        /// </summary>
        /// <param name="a">The alpha component.</param>
        /// <param name="r">The red component.</param>
        /// <param name="g">The green component.</param>
        /// <param name="b">The blue component.</param>
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
        public static bool TryParse(string? s, out Color color)
        {
            color = default;

            if (string.IsNullOrEmpty(s))
            {
                return false;
            }

            if (s[0] == '#' &&
                TryParseHexFormat(s.AsSpan(), out color))
            {
                return true;
            }

            // Note: The length checks are also an important optimization.
            // The shortest possible CSS format is "rbg(0,0,0)", Length = 10.

            if (s.Length >= 10 &&
                (s[0] == 'r' || s[0] == 'R') &&
                (s[1] == 'g' || s[1] == 'G') &&
                (s[2] == 'b' || s[2] == 'B') &&
                TryParseCssFormat(s, out color))
            {
                return true;
            }

            if (s.Length >= 10 &&
                (s[0] == 'h' || s[0] == 'H') &&
                (s[1] == 's' || s[1] == 'S') &&
                (s[2] == 'l' || s[2] == 'L') &&
                HslColor.TryParse(s, out HslColor hslColor))
            {
                color = hslColor.ToRgb();
                return true;
            }

            if (s.Length >= 10 &&
                (s[0] == 'h' || s[0] == 'H') &&
                (s[1] == 's' || s[1] == 'S') &&
                (s[2] == 'v' || s[2] == 'V') &&
                HsvColor.TryParse(s, out HsvColor hsvColor))
            {
                color = hsvColor.ToRgb();
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

            if (s[0] == '#' &&
                TryParseHexFormat(s, out color))
            {
                return true;
            }

            // At this point all parsing uses strings
            var str = s.ToString();

            // Note: The length checks are also an important optimization.
            // The shortest possible CSS format is "rbg(0,0,0)", Length = 10.

            if (s.Length >= 10 &&
                (s[0] == 'r' || s[0] == 'R') &&
                (s[1] == 'g' || s[1] == 'G') &&
                (s[2] == 'b' || s[2] == 'B') &&
                TryParseCssFormat(str, out color))
            {
                return true;
            }

            if (s.Length >= 10 &&
                (s[0] == 'h' || s[0] == 'H') &&
                (s[1] == 's' || s[1] == 'S') &&
                (s[2] == 'l' || s[2] == 'L') &&
                HslColor.TryParse(str, out HslColor hslColor))
            {
                color = hslColor.ToRgb();
                return true;
            }

            if (s.Length >= 10 &&
                (s[0] == 'h' || s[0] == 'H') &&
                (s[1] == 's' || s[1] == 'S') &&
                (s[2] == 'v' || s[2] == 'V') &&
                HsvColor.TryParse(str, out HsvColor hsvColor))
            {
                color = hsvColor.ToRgb();
                return true;
            }

            var knownColor = KnownColors.GetKnownColor(str);

            if (knownColor != KnownColor.None)
            {
                color = knownColor.ToColor();
                return true;
            }

            color = default;

            return false;
        }

        /// <summary>
        /// Parses the given span of characters representing a hex color value into a new <see cref="Color"/>.
        /// </summary>
        private static bool TryParseHexFormat(ReadOnlySpan<char> s, out Color color)
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

                if (!input.TryParseUInt(NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var parsed))
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
        /// Parses the given string representing a CSS color value into a new <see cref="Color"/>.
        /// </summary>
        private static bool TryParseCssFormat(string? s, out Color color)
        {
            bool prefixMatched = false;

            color = default;

            if (s is null)
            {
                return false;
            }

            string workingString = s.Trim();

            if (workingString.Length == 0 ||
                workingString.IndexOf(",", StringComparison.Ordinal) < 0)
            {
                return false;
            }

            if (workingString.Length >= 11 &&
                workingString.StartsWith("rgba(", StringComparison.OrdinalIgnoreCase) &&
                workingString.EndsWith(')'))
            {
                workingString = workingString.Substring(5, workingString.Length - 6);
                prefixMatched = true;
            }

            if (prefixMatched == false &&
                workingString.Length >= 10 &&
                workingString.StartsWith("rgb(", StringComparison.OrdinalIgnoreCase) &&
                workingString.EndsWith(')'))
            {
                workingString = workingString.Substring(4, workingString.Length - 5);
                prefixMatched = true;
            }

            if (prefixMatched == false)
            {
                return false;
            }

            string[] components = workingString.Split(',');

            if (components.Length == 3) // RGB
            {
                if (InternalTryParseByte(components[0].AsSpan(), out byte red) &&
                    InternalTryParseByte(components[1].AsSpan(), out byte green) &&
                    InternalTryParseByte(components[2].AsSpan(), out byte blue))
                {
                    color = new Color(0xFF, red, green, blue);
                    return true;
                }
            }
            else if (components.Length == 4) // RGBA
            {
                if (InternalTryParseByte(components[0].AsSpan(), out byte red) &&
                    InternalTryParseByte(components[1].AsSpan(), out byte green) &&
                    InternalTryParseByte(components[2].AsSpan(), out byte blue) &&
                    InternalTryParseDouble(components[3].AsSpan(), out double alpha))
                {
                    color = new Color((byte)Math.Round(alpha * 255.0), red, green, blue);
                    return true;
                }
            }

            // Local function to specially parse a byte value with an optional percentage sign
            bool InternalTryParseByte(ReadOnlySpan<char> inString, out byte outByte)
            {
                // The percent sign, if it exists, must be at the end of the number
                int percentIndex = inString.IndexOf("%".AsSpan(), StringComparison.Ordinal);

                if (percentIndex >= 0)
                {
                    var result = inString.Slice(0, percentIndex).TryParseDouble(NumberStyles.Number, CultureInfo.InvariantCulture,
                        out double percentage);

                    outByte = (byte)Math.Round((percentage / 100.0) * 255.0);
                    return result;
                }
                else
                {
                    return inString.TryParseByte(NumberStyles.Number, CultureInfo.InvariantCulture,
                        out outByte);
                }
            }

            // Local function to specially parse a double value with an optional percentage sign
            bool InternalTryParseDouble(ReadOnlySpan<char> inString, out double outDouble)
            {
                // The percent sign, if it exists, must be at the end of the number
                int percentIndex = inString.IndexOf("%".AsSpan(), StringComparison.Ordinal);

                if (percentIndex >= 0)
                {
                    var result = inString.Slice(0, percentIndex).TryParseDouble(NumberStyles.Number, CultureInfo.InvariantCulture,
                         out double percentage);

                    outDouble = percentage / 100.0;
                    return result;
                }
                else
                {
                    return inString.TryParseDouble(NumberStyles.Number, CultureInfo.InvariantCulture,
                        out outDouble);
                }
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
            uint rgb = ToUInt32();
            return KnownColors.GetKnownColorName(rgb) ?? $"#{rgb.ToString("x8", CultureInfo.InvariantCulture)}";
        }

        internal void ToString(System.Text.StringBuilder builder)
        {
            uint rgb = ToUInt32();
            if(KnownColors.TryGetKnownColorName(rgb, out var name))
            {
                builder.Append(name);
            }
            else
            {
                builder.Append('#');
                builder.AppendFormat(CultureInfo.InvariantCulture, "{0:x8}", rgb);
            }
        }

        /// <summary>
        /// Returns the integer representation of the color.
        /// </summary>
        /// <returns>
        /// The integer representation of the color.
        /// </returns>
        public uint ToUInt32()
        {
            return ((uint)A << 24) | ((uint)R << 16) | ((uint)G << 8) | (uint)B;
        }

        /// <inheritdoc cref="Color.ToUInt32"/>
        [Obsolete("Use Color.ToUInt32() instead."), EditorBrowsable(EditorBrowsableState.Never)]
        public uint ToUint32()
        {
            return ToUInt32();
        }

        /// <summary>
        /// Returns the HSL color model equivalent of this RGB color.
        /// </summary>
        /// <returns>The HSL equivalent color.</returns>
        public HslColor ToHsl()
        {
            return Color.ToHsl(R, G, B, A);
        }

        /// <summary>
        /// Returns the HSV color model equivalent of this RGB color.
        /// </summary>
        /// <returns>The HSV equivalent color.</returns>
        public HsvColor ToHsv()
        {
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
        /// Converts the given RGBA color component values to their HSL color equivalent.
        /// </summary>
        /// <param name="red">The Red component in the RGB color model.</param>
        /// <param name="green">The Green component in the RGB color model.</param>
        /// <param name="blue">The Blue component in the RGB color model.</param>
        /// <param name="alpha">The Alpha component.</param>
        /// <returns>A new <see cref="HslColor"/> equivalent to the given RGBA values.</returns>
        public static HslColor ToHsl(
            byte red,
            byte green,
            byte blue,
            byte alpha = 0xFF)
        {
            // Normalize RGBA components into the 0..1 range
            return Color.ToHsl(
                (byteToDouble * red),
                (byteToDouble * green),
                (byteToDouble * blue),
                (byteToDouble * alpha));
        }

        /// <summary>
        /// Converts the given RGBA color component values to their HSL color equivalent.
        /// </summary>
        /// <remarks>
        /// Warning: No bounds checks or clamping is done on the input component values.
        /// This method is for internal-use only and the caller must ensure bounds.
        /// </remarks>
        /// <param name="r">The Red component in the RGB color model within the range 0..1.</param>
        /// <param name="g">The Green component in the RGB color model within the range 0..1.</param>
        /// <param name="b">The Blue component in the RGB color model within the range 0..1.</param>
        /// <param name="a">The Alpha component in the RGB color model within the range 0..1.</param>
        /// <returns>A new <see cref="HslColor"/> equivalent to the given RGBA values.</returns>
        internal static HslColor ToHsl(
            double r,
            double g,
            double b,
            double a = 1.0)
        {
            // Note: Conversion code is originally based on ColorHelper in the Windows Community Toolkit (licensed MIT)
            // https://github.com/CommunityToolkit/WindowsCommunityToolkit/blob/main/Microsoft.Toolkit.Uwp/Helpers/ColorHelper.cs
            // It has been modified.

            double max = r >= g ? (r >= b ? r : b) : (g >= b ? g : b);
            double min = r <= g ? (r <= b ? r : b) : (g <= b ? g : b);
            double chroma = max - min;
            double h1;

            if (chroma == 0)
            {
                h1 = 0;
            }
            else if (max == r)
            {
                // The % operator doesn't do proper modulo on negative
                // numbers, so we'll add 6 before using it
                h1 = (((g - b) / chroma) + 6) % 6;
            }
            else if (max == g)
            {
                h1 = 2 + ((b - r) / chroma);
            }
            else
            {
                h1 = 4 + ((r - g) / chroma);
            }

            double lightness = 0.5 * (max + min);
            double saturation = chroma == 0 ? 0 : chroma / (1 - Math.Abs((2 * lightness) - 1));

            return new HslColor(a, 60 * h1, saturation, lightness, clampValues: false);
        }

        /// <summary>
        /// Converts the given RGBA color component values to their HSV color equivalent.
        /// </summary>
        /// <param name="red">The Red component in the RGB color model.</param>
        /// <param name="green">The Green component in the RGB color model.</param>
        /// <param name="blue">The Blue component in the RGB color model.</param>
        /// <param name="alpha">The Alpha component.</param>
        /// <returns>A new <see cref="HsvColor"/> equivalent to the given RGBA values.</returns>
        public static HsvColor ToHsv(
            byte red,
            byte green,
            byte blue,
            byte alpha = 0xFF)
        {
            // Normalize RGBA components into the 0..1 range
            return Color.ToHsv(
                (byteToDouble * red),
                (byteToDouble * green),
                (byteToDouble * blue),
                (byteToDouble * alpha));
        }

        /// <summary>
        /// Converts the given RGBA color component values to their HSV color equivalent.
        /// </summary>
        /// <remarks>
        /// Warning: No bounds checks or clamping is done on the input component values.
        /// This method is for internal-use only and the caller must ensure bounds.
        /// </remarks>
        /// <param name="r">The Red component in the RGB color model within the range 0..1.</param>
        /// <param name="g">The Green component in the RGB color model within the range 0..1.</param>
        /// <param name="b">The Blue component in the RGB color model within the range 0..1.</param>
        /// <param name="a">The Alpha component in the RGB color model within the range 0..1.</param>
        /// <returns>A new <see cref="HsvColor"/> equivalent to the given RGBA values.</returns>
        internal static HsvColor ToHsv(
            double r,
            double g,
            double b,
            double a = 1.0)
        {
            // Note: Conversion code is originally based on the C++ in WinUI (licensed MIT)
            // https://github.com/microsoft/microsoft-ui-xaml/blob/main/dev/Common/ColorConversion.cpp
            // This was used because it is the best documented and likely most optimized for performance
            // Alpha support was added

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

            return new HsvColor(a, hue, saturation, value, clampValues: false);
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
