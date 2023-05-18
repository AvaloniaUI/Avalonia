// Color conversion portions of this source file are adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
using System.Globalization;
using System.Text;
using Avalonia.Utilities;

namespace Avalonia.Media
{
    /// <summary>
    /// Defines a color using the hue/saturation/value (HSV) model.
    /// This uses a cylindrical-coordinate representation of a color.
    /// </summary>
#if !BUILDTASK
    public
#endif
    readonly struct HsvColor : IEquatable<HsvColor>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HsvColor"/> struct.
        /// </summary>
        /// <param name="alpha">The Alpha (transparency) component in the range from 0..1.</param>
        /// <param name="hue">The Hue component in the range from 0..360.
        /// Note that 360 is equivalent to 0 and will be adjusted automatically.</param>
        /// <param name="saturation">The Saturation component in the range from 0..1.</param>
        /// <param name="value">The Value component in the range from 0..1.</param>
        public HsvColor(
            double alpha,
            double hue,
            double saturation,
            double value)
        {
            A = MathUtilities.Clamp(alpha,      0.0, 1.0);
            H = MathUtilities.Clamp(hue,        0.0, 360.0);
            S = MathUtilities.Clamp(saturation, 0.0, 1.0);
            V = MathUtilities.Clamp(value,      0.0, 1.0);

            // The maximum value of Hue is technically 360 minus epsilon (just below 360).
            // This is because, in a color circle, 360 degrees is equivalent to 0 degrees.
            // However, that is too tricky to work with in code and isn't as intuitive.
            // Therefore, since 360 == 0, just wrap 360 if needed back to 0.
            H = (H == 360.0 ? 0 : H);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HsvColor"/> struct.
        /// </summary>
        /// <remarks>
        /// This constructor exists only for internal use where performance is critical.
        /// Whether or not the component values are in the correct ranges must be known.
        /// </remarks>
        /// <param name="alpha">The Alpha (transparency) component in the range from 0..1.</param>
        /// <param name="hue">The Hue component in the range from 0..360.
        /// Note that 360 is equivalent to 0 and will be adjusted automatically.</param>
        /// <param name="saturation">The Saturation component in the range from 0..1.</param>
        /// <param name="value">The Value component in the range from 0..1.</param>
        /// <param name="clampValues">Whether to clamp component values to their required ranges.</param>
        internal HsvColor(
            double alpha,
            double hue,
            double saturation,
            double value,
            bool clampValues)
        {
            if (clampValues)
            {
                A = MathUtilities.Clamp(alpha,      0.0, 1.0);
                H = MathUtilities.Clamp(hue,        0.0, 360.0);
                S = MathUtilities.Clamp(saturation, 0.0, 1.0);
                V = MathUtilities.Clamp(value,      0.0, 1.0);

                // See comments in constructor above
                H = (H == 360.0 ? 0 : H);
            }
            else
            {
                A = alpha;
                H = hue;
                S = saturation;
                V = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HsvColor"/> struct.
        /// </summary>
        /// <param name="color">The RGB color to convert to HSV.</param>
        public HsvColor(Color color)
        {
            var hsv = color.ToHsv();

            A = hsv.A;
            H = hsv.H;
            S = hsv.S;
            V = hsv.V;
        }

        /// <summary>
        /// Gets the Alpha (transparency) component in the range from 0..1 (percentage).
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        ///   <item>0 is fully transparent.</item>
        ///   <item>1 is fully opaque.</item>
        /// </list>
        /// </remarks>
        public double A { get; }

        /// <summary>
        /// Gets the Hue component in the range from 0..360 (degrees).
        /// This is the color's location, in degrees, on a color wheel/circle from 0 to 360.
        /// Note that 360 is equivalent to 0 and will be adjusted automatically.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        ///   <item>0/360 degrees is Red.</item>
        ///   <item>60 degrees is Yellow.</item>
        ///   <item>120 degrees is Green.</item>
        ///   <item>180 degrees is Cyan.</item>
        ///   <item>240 degrees is Blue.</item>
        ///   <item>300 degrees is Magenta.</item>
        /// </list>
        /// </remarks>
        public double H { get; }

        /// <summary>
        /// Gets the Saturation component in the range from 0..1 (percentage).
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        ///   <item>0 is fully white (or a shade of gray) and shows no color.</item>
        ///   <item>1 is the full color.</item>
        /// </list>
        /// </remarks>
        public double S { get; }

        /// <summary>
        /// Gets the Value (or Brightness/Intensity) component in the range from 0..1 (percentage).
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        ///   <item>0 is fully black and shows no color.</item>
        ///   <item>1 is the brightest and shows full color.</item>
        /// </list>
        /// </remarks>
        public double V { get; }

        /// <inheritdoc/>
        public bool Equals(HsvColor other)
        {
            return other.A == A &&
                   other.H == H &&
                   other.S == S &&
                   other.V == V;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj is HsvColor hsvColor)
            {
                return Equals(hsvColor);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a hashcode for this object.
        /// Hashcode is not guaranteed to be unique.
        /// </summary>
        /// <returns>The hashcode for this object.</returns>
        public override int GetHashCode()
        {
            // Same algorithm as Color
            // This is used instead of HashCode.Combine() due to .NET Standard 2.0 requirements
            unchecked
            {
                int hashCode = A.GetHashCode();
                hashCode = (hashCode * 397) ^ H.GetHashCode();
                hashCode = (hashCode * 397) ^ S.GetHashCode();
                hashCode = (hashCode * 397) ^ V.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// Returns the RGB color model equivalent of this HSV color.
        /// </summary>
        /// <returns>The RGB equivalent color.</returns>
        public Color ToRgb()
        {
            return HsvColor.ToRgb(H, S, V, A);
        }

        /// <summary>
        /// Returns the HSL color model equivalent of this HSV color.
        /// </summary>
        /// <returns>The HSL equivalent color.</returns>
        public HslColor ToHsl()
        {
            return HsvColor.ToHsl(H, S, V, A);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var sb = StringBuilderCache.Acquire();

            // Use a format similar to CSS. However:
            //   - To ensure precision is never lost, allow decimal places.
            //     This is especially important for round-trip serialization.
            //   - To maintain numerical consistency, do not use percent.
            //
            // Example:
            //
            // hsva(hue, saturation, value, alpha)
            // hsva(230, 1.0, 0.5, 1.0)
            //

            sb.Append("hsva(");
            sb.Append(H.ToString(CultureInfo.InvariantCulture));
            sb.Append(", ");
            sb.Append(S.ToString(CultureInfo.InvariantCulture));
            sb.Append(", ");
            sb.Append(V.ToString(CultureInfo.InvariantCulture));
            sb.Append(", ");
            sb.Append(A.ToString(CultureInfo.InvariantCulture));
            sb.Append(')');

            return StringBuilderCache.GetStringAndRelease(sb);
        }

        /// <summary>
        /// Parses an HSV color string.
        /// </summary>
        /// <param name="s">The HSV color string to parse.</param>
        /// <returns>The parsed <see cref="HsvColor"/>.</returns>
        public static HsvColor Parse(string s)
        {
            if (s is null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            if (TryParse(s, out HsvColor hsvColor))
            {
                return hsvColor;
            }

            throw new FormatException($"Invalid HSV color string: '{s}'.");
        }

        /// <summary>
        /// Parses an HSV color string.
        /// </summary>
        /// <param name="s">The HSV color string to parse.</param>
        /// <param name="hsvColor">The parsed <see cref="HsvColor"/>.</param>
        /// <returns>True if parsing was successful; otherwise, false.</returns>
        public static bool TryParse(string? s, out HsvColor hsvColor)
        {
            bool prefixMatched = false;

            hsvColor = default;

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

            // Note: The length checks are also an important optimization.
            // The shortest possible format is "hsv(0,0,0)", Length = 10.

            if (workingString.Length >= 11 &&
                workingString.StartsWith("hsva(", StringComparison.OrdinalIgnoreCase) &&
                workingString.EndsWith(")", StringComparison.Ordinal))
            {
                workingString = workingString.Substring(5, workingString.Length - 6);
                prefixMatched = true;
            }

            if (prefixMatched == false &&
                workingString.Length >= 10 &&
                workingString.StartsWith("hsv(", StringComparison.OrdinalIgnoreCase) &&
                workingString.EndsWith(")", StringComparison.Ordinal))
            {
                workingString = workingString.Substring(4, workingString.Length - 5);
                prefixMatched = true;
            }

            if (prefixMatched == false)
            {
                return false;
            }

            string[] components = workingString.Split(',');

            if (components.Length == 3) // HSV
            {
                if (components[0].AsSpan().TryParseDouble(NumberStyles.Number, CultureInfo.InvariantCulture, out double hue) &&
                    TryInternalParse(components[1].AsSpan(), out double saturation) &&
                    TryInternalParse(components[2].AsSpan(), out double value))
                {
                    hsvColor = new HsvColor(1.0, hue, saturation, value);
                    return true;
                }
            }
            else if (components.Length == 4) // HSVA
            {
                if (components[0].AsSpan().TryParseDouble(NumberStyles.Number, CultureInfo.InvariantCulture, out double hue) &&
                    TryInternalParse(components[1].AsSpan(), out double saturation) &&
                    TryInternalParse(components[2].AsSpan(), out double value) &&
                    TryInternalParse(components[3].AsSpan(), out double alpha))
                {
                    hsvColor = new HsvColor(alpha, hue, saturation, value);
                    return true;
                }
            }

            // Local function to specially parse a double value with an optional percentage sign
            bool TryInternalParse(ReadOnlySpan<char> inString, out double outDouble)
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
        /// Creates a new <see cref="HsvColor"/> from individual color component values.
        /// </summary>
        /// <remarks>
        /// This exists for symmetry with the <see cref="Color"/> struct; however, the
        /// appropriate constructor should commonly be used instead.
        /// </remarks>
        /// <param name="a">The Alpha (transparency) component in the range from 0..1.</param>
        /// <param name="h">The Hue component in the range from 0..360.</param>
        /// <param name="s">The Saturation component in the range from 0..1.</param>
        /// <param name="v">The Value component in the range from 0..1.</param>
        /// <returns>A new <see cref="HsvColor"/> built from the individual color component values.</returns>
        public static HsvColor FromAhsv(double a, double h, double s, double v)
        {
            return new HsvColor(a, h, s, v);
        }

        /// <summary>
        /// Creates a new <see cref="HsvColor"/> from individual color component values.
        /// </summary>
        /// <remarks>
        /// This exists for symmetry with the <see cref="Color"/> struct; however, the
        /// appropriate constructor should commonly be used instead.
        /// </remarks>
        /// <param name="h">The Hue component in the range from 0..360.</param>
        /// <param name="s">The Saturation component in the range from 0..1.</param>
        /// <param name="v">The Value component in the range from 0..1.</param>
        /// <returns>A new <see cref="HsvColor"/> built from the individual color component values.</returns>
        public static HsvColor FromHsv(double h, double s, double v)
        {
            return new HsvColor(1.0, h, s, v);
        }

        /// <summary>
        /// Converts the given HSVA color component values to their RGB color equivalent.
        /// </summary>
        /// <param name="hue">The Hue component in the HSV color model in the range from 0..360.</param>
        /// <param name="saturation">The Saturation component in the HSV color model in the range from 0..1.</param>
        /// <param name="value">The Value component in the HSV color model in the range from 0..1.</param>
        /// <param name="alpha">The Alpha component in the range from 0..1.</param>
        /// <returns>A new RGB <see cref="Color"/> equivalent to the given HSVA values.</returns>
        public static Color ToRgb(
            double hue,
            double saturation,
            double value,
            double alpha = 1.0)
        {
            // Note: Conversion code is originally based on the C++ in WinUI (licensed MIT)
            // https://github.com/microsoft/microsoft-ui-xaml/blob/main/dev/Common/ColorConversion.cpp
            // This was used because it is the best documented and likely most optimized for performance
            // Alpha support was added

            // We want the hue to be between 0 and 359,
            // so we first ensure that that's the case.
            while (hue >= 360.0)
            {
                hue -= 360.0;
            }

            while (hue < 0.0)
            {
                hue += 360.0;
            }

            // We similarly clamp saturation, value and alpha between 0 and 1.
            saturation = saturation < 0.0 ? 0.0 : saturation;
            saturation = saturation > 1.0 ? 1.0 : saturation;

            value = value < 0.0 ? 0.0 : value;
            value = value > 1.0 ? 1.0 : value;

            alpha = alpha < 0.0 ? 0.0 : alpha;
            alpha = alpha > 1.0 ? 1.0 : alpha;

            // The first thing that we need to do is to determine the chroma (see above for its definition).
            // Remember from above that:
            //
            // 1. The chroma is the difference between the maximum and the minimum of the RGB channels,
            // 2. The value is the maximum of the RGB channels, and
            // 3. The saturation comes from dividing the chroma by the maximum of the RGB channels (i.e., the value).
            //
            // From these facts, you can see that we can retrieve the chroma by simply multiplying the saturation and the value,
            // and we can retrieve the minimum of the RGB channels by subtracting the chroma from the value.
            var chroma = saturation * value;
            var min = value - chroma;

            // If the chroma is zero, then we have a greyscale color.  In that case, the maximum and the minimum RGB channels
            // have the same value (and, indeed, all of the RGB channels are the same), so we can just immediately return
            // the minimum value as the value of all the channels.
            if (chroma == 0)
            {
                return Color.FromArgb(
                    (byte)Math.Round(alpha * 255),
                    (byte)Math.Round(min * 255),
                    (byte)Math.Round(min * 255),
                    (byte)Math.Round(min * 255));
            }

            // If the chroma is not zero, then we need to continue.  The first step is to figure out
            // what section of the color wheel we're located in.  In order to do that, we'll divide the hue by 60.
            // The resulting value means we're in one of the following locations:
            //
            // 0 - Between red and yellow.
            // 1 - Between yellow and green.
            // 2 - Between green and cyan.
            // 3 - Between cyan and blue.
            // 4 - Between blue and purple.
            // 5 - Between purple and red.
            //
            // In each of these sextants, one of the RGB channels is completely present, one is partially present, and one is not present.
            // For example, as we transition between red and yellow, red is completely present, green is becoming increasingly present, and blue is not present.
            // Then, as we transition from yellow and green, green is now completely present, red is becoming decreasingly present, and blue is still not present.
            // As we transition from green to cyan, green is still completely present, blue is becoming increasingly present, and red is no longer present.  And so on.
            //
            // To convert from hue to RGB value, we first need to figure out which of the three channels is in which configuration
            // in the sextant that we're located in.  Next, we figure out what value the completely-present color should have.
            // We know that chroma = (max - min), and we know that this color is the max color, so to find its value we simply add
            // min to chroma to retrieve max.  Finally, we consider how far we've transitioned from the pure form of that color
            // to the next color (e.g., how far we are from pure red towards yellow), and give a value to the partially present channel
            // equal to the minimum plus the chroma (i.e., the max minus the min), multiplied by the percentage towards the new color.
            // This gets us a value between the maximum and the minimum representing the partially present channel.
            // Finally, the not-present color must be equal to the minimum value, since it is the one least participating in the overall color.
            int sextant = (int)(hue / 60);
            double intermediateColorPercentage = (hue / 60) - sextant;
            double max = chroma + min;

            double r = 0;
            double g = 0;
            double b = 0;

            switch (sextant)
            {
                case 0:
                    r = max;
                    g = min + (chroma * intermediateColorPercentage);
                    b = min;
                    break;
                case 1:
                    r = min + (chroma * (1 - intermediateColorPercentage));
                    g = max;
                    b = min;
                    break;
                case 2:
                    r = min;
                    g = max;
                    b = min + (chroma * intermediateColorPercentage);
                    break;
                case 3:
                    r = min;
                    g = min + (chroma * (1 - intermediateColorPercentage));
                    b = max;
                    break;
                case 4:
                    r = min + (chroma * intermediateColorPercentage);
                    g = min;
                    b = max;
                    break;
                case 5:
                    r = max;
                    g = min;
                    b = min + (chroma * (1 - intermediateColorPercentage));
                    break;
            }

            return new Color(
                (byte)Math.Round(alpha * 255),
                (byte)Math.Round(r * 255),
                (byte)Math.Round(g * 255),
                (byte)Math.Round(b * 255));
        }

        /// <summary>
        /// Converts the given HSVA color component values to their HSL color equivalent.
        /// </summary>
        /// <param name="hue">The Hue component in the HSV color model in the range from 0..360.</param>
        /// <param name="saturation">The Saturation component in the HSV color model in the range from 0..1.</param>
        /// <param name="value">The Value component in the HSV color model in the range from 0..1.</param>
        /// <param name="alpha">The Alpha component in the range from 0..1.</param>
        /// <returns>A new <see cref="HslColor"/> equivalent to the given HSVA values.</returns>
        public static HslColor ToHsl(
            double hue,
            double saturation,
            double value,
            double alpha = 1.0)
        {
            // We want the hue to be between 0 and 359,
            // so we first ensure that that's the case.
            while (hue >= 360.0)
            {
                hue -= 360.0;
            }

            while (hue < 0.0)
            {
                hue += 360.0;
            }

            // We similarly clamp saturation, value and alpha between 0 and 1.
            saturation = saturation < 0.0 ? 0.0 : saturation;
            saturation = saturation > 1.0 ? 1.0 : saturation;

            value = value < 0.0 ? 0.0 : value;
            value = value > 1.0 ? 1.0 : value;

            alpha = alpha < 0.0 ? 0.0 : alpha;
            alpha = alpha > 1.0 ? 1.0 : alpha;

            // The conversion algorithm is from the below link
            // https://en.wikipedia.org/wiki/HSL_and_HSV#Interconversion

            double s;
            double l = value * (1.0 - (saturation / 2.0));

            if (l <= 0 || l >= 1)
            {
                s = 0.0;
            }
            else
            {
                s = (value - l) / Math.Min(l, 1.0 - l);
            }

            return new HslColor(alpha, hue, s, l);
        }

        /// <summary>
        /// Indicates whether the values of two specified <see cref="HsvColor"/> objects are equal.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns>True if left and right are equal; otherwise, false.</returns>
        public static bool operator ==(HsvColor left, HsvColor right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Indicates whether the values of two specified <see cref="HsvColor"/> objects are not equal.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns>True if left and right are not equal; otherwise, false.</returns>
        public static bool operator !=(HsvColor left, HsvColor right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Explicit conversion from an <see cref="HsvColor"/> to a <see cref="Color"/>.
        /// </summary>
        /// <param name="hsvColor">The <see cref="HsvColor"/> to convert.</param>
        public static explicit operator Color(HsvColor hsvColor)
        {
            return hsvColor.ToRgb();
        }
    }
}
