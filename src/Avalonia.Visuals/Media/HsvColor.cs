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
    /// </summary>
#if !BUILDTASK
    public
#endif
    readonly struct HsvColor : IEquatable<HsvColor>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HsvColor"/> struct.
        /// </summary>
        /// <param name="alpha">The Alpha (transparency) channel value in the range from 0..1.</param>
        /// <param name="hue">The Hue channel value in the range from 0..360.
        /// Note that 360 is equivalent to 0 and will be adjusted automatically.</param>
        /// <param name="saturation">The Saturation channel value in the range from 0..1.</param>
        /// <param name="value">The Value channel value in the range from 0..1.</param>
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
        /// Whether or not the channel values are in the correct ranges must be known.
        /// </remarks>
        /// <param name="alpha">The Alpha (transparency) channel value in the range from 0..1.</param>
        /// <param name="hue">The Hue channel value in the range from 0..360.
        /// Note that 360 is equivalent to 0 and will be adjusted automatically.</param>
        /// <param name="saturation">The Saturation channel value in the range from 0..1.</param>
        /// <param name="value">The Value channel value in the range from 0..1.</param>
        /// <param name="clampValues">Whether to clamp channel values to their required ranges.</param>
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
            var hsv = HsvColor.FromRgb(color);

            A = hsv.A;
            H = hsv.H;
            S = hsv.S;
            V = hsv.V;
        }

        /// <summary>
        /// Gets the Alpha (transparency) channel value in the range from 0..1.
        /// </summary>
        public double A { get; }

        /// <summary>
        /// Gets the Hue channel value in the range from 0..360.
        /// Note that 360 is equivalent to 0 and will be adjusted automatically.
        /// </summary>
        public double H { get; }

        /// <summary>
        /// Gets the Saturation channel value in the range from 0..1.
        /// </summary>
        public double S { get; }

        /// <summary>
        /// Gets the Value channel value in the range from 0..1.
        /// </summary>
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
            // Use the by-channel conversion method directly for performance
            return HsvColor.ToRgb(H, S, V, A);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var sb = new StringBuilder();

            // Use a format similar to HSL in HTML/CSS "hsla(0, 100%, 50%, 0.5)"
            //
            // However:
            //   - To ensure precision is never lost, allow decimal places
            //   - To maintain numerical consistency do not use percent
            //
            // Example:
            //
            // hsva(hue, saturation, value, alpha)
            // hsva(230, 1.0, 0.5, 1.0)
            //
            //   Where:
            //
            //          hue : double from 0 to 360
            //   saturation : double from 0 to 1
            //                (HTML uses a percentage)
            //        value : double from 0 to 1
            //                (HTML uses a percentage)
            //        alpha : double from 0 to 1
            //                (HTML does not use a percentage for alpha)

            sb.Append("hsva(");
            sb.Append(H.ToString(CultureInfo.InvariantCulture));
            sb.Append(", ");
            sb.Append(S.ToString(CultureInfo.InvariantCulture));
            sb.Append(", ");
            sb.Append(V.ToString(CultureInfo.InvariantCulture));
            sb.Append(", ");
            sb.Append(A.ToString(CultureInfo.InvariantCulture));
            sb.Append(')');

            return sb.ToString();
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
        public static bool TryParse(string s, out HsvColor hsvColor)
        {
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

            if (workingString.Length > 6 &&
                workingString.StartsWith("hsva(", StringComparison.OrdinalIgnoreCase) &&
                workingString.EndsWith(")", StringComparison.Ordinal))
            {
                workingString = workingString.Substring(5, workingString.Length - 6);
            }

            if (workingString.Length > 5 &&
                workingString.StartsWith("hsv(", StringComparison.OrdinalIgnoreCase) &&
                workingString.EndsWith(")", StringComparison.Ordinal))
            {
                workingString = workingString.Substring(4, workingString.Length - 5);
            }

            string[] components = workingString.Split(',');

            if (components.Length == 3) // HSV
            {
                if (double.TryParse(components[0], NumberStyles.Number, CultureInfo.InvariantCulture, out double hue) &&
                    double.TryParse(components[1], NumberStyles.Number, CultureInfo.InvariantCulture, out double saturation) &&
                    double.TryParse(components[2], NumberStyles.Number, CultureInfo.InvariantCulture, out double value))
                {
                    hsvColor = new HsvColor(1.0, hue, saturation, value);
                    return true;
                }
            }
            else if (components.Length == 4) // HSVA
            {
                if (double.TryParse(components[0], NumberStyles.Number, CultureInfo.InvariantCulture, out double hue) &&
                    double.TryParse(components[1], NumberStyles.Number, CultureInfo.InvariantCulture, out double saturation) &&
                    double.TryParse(components[2], NumberStyles.Number, CultureInfo.InvariantCulture, out double value) &&
                    double.TryParse(components[3], NumberStyles.Number, CultureInfo.InvariantCulture, out double alpha))
                {
                    hsvColor = new HsvColor(alpha, hue, saturation, value);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Creates a new <see cref="HsvColor"/> from individual color channel values.
        /// </summary>
        /// <remarks>
        /// This exists for symmetry with the <see cref="Color"/> struct; however, the
        /// appropriate constructor should commonly be used instead.
        /// </remarks>
        /// <param name="a">The Alpha (transparency) channel value in the range from 0..1.</param>
        /// <param name="h">The Hue channel value in the range from 0..360.</param>
        /// <param name="s">The Saturation channel value in the range from 0..1.</param>
        /// <param name="v">The Value channel value in the range from 0..1.</param>
        /// <returns>A new <see cref="HsvColor"/> built from the individual color channel values.</returns>
        public static HsvColor FromAhsv(double a, double h, double s, double v)
        {
            return new HsvColor(a, h, s, v);
        }

        /// <summary>
        /// Converts the given HSV color to its RGB color equivalent.
        /// </summary>
        /// <param name="hsvColor">The color in the HSV color model.</param>
        /// <returns>A new RGB <see cref="Color"/> equivalent to the given HSVA values.</returns>
        public static Color ToRgb(HsvColor hsvColor)
        {
            return HsvColor.ToRgb(hsvColor.H, hsvColor.S, hsvColor.V, hsvColor.A);
        }

        /// <summary>
        /// Converts the given HSVA color channel values to its RGB color equivalent.
        /// </summary>
        /// <param name="hue">The hue channel value in the HSV color model in the range from 0..360.</param>
        /// <param name="saturation">The saturation channel value in the HSV color model in the range from 0..1.</param>
        /// <param name="value">The value channel value in the HSV color model in the range from 0..1.</param>
        /// <param name="alpha">The alpha channel value in the range from 0..1.</param>
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
            // Alpha channel support was added

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

            return Color.FromArgb(
                (byte)Math.Round(alpha * 255),
                (byte)Math.Round(r * 255),
                (byte)Math.Round(g * 255),
                (byte)Math.Round(b * 255));
        }

        /// <summary>
        /// Converts the given RGB color to its HSV color equivalent.
        /// </summary>
        /// <param name="color">The color in the RGB color model.</param>
        /// <returns>A new <see cref="HsvColor"/> equivalent to the given RGBA values.</returns>
        public static HsvColor FromRgb(Color color)
        {
            return HsvColor.FromRgb(color.R, color.G, color.B, color.A);
        }

        /// <summary>
        /// Converts the given RGBA color channel values to its HSV color equivalent.
        /// </summary>
        /// <param name="red">The red channel value in the RGB color model.</param>
        /// <param name="green">The green channel value in the RGB color model.</param>
        /// <param name="blue">The blue channel value in the RGB color model.</param>
        /// <param name="alpha">The alpha channel value.</param>
        /// <returns>A new <see cref="HsvColor"/> equivalent to the given RGBA values.</returns>
        public static HsvColor FromRgb(
            byte red,
            byte green,
            byte blue,
            byte alpha = 0xFF)
        {
            // Normalize RGBA channel values into the 0..1 range
            return HsvColor.FromRgb(
                (red / 255.0),
                (green / 255.0),
                (blue / 255.0),
                (alpha / 255.0));
        }

        // TODO: Mark the below method Internal and make Internals visible to Avalonia.Controls...

        /// <summary>
        /// Converts the given RGBA color channel values to its HSV color equivalent.
        /// </summary>
        /// <remarks>
        /// Warning: No bounds checks or clamping is done on the input channel values.
        /// This method is for internal-use only and the caller must ensure bounds.
        /// </remarks>
        /// <param name="r">The red channel value in the RGB color model within the range 0..1.</param>
        /// <param name="g">The green channel value in the RGB color model within the range 0..1.</param>
        /// <param name="b">The blue channel value in the RGB color model within the range 0..1.</param>
        /// <param name="a">The alpha channel value in the RGB color model within the range 0..1.</param>
        /// <returns>A new <see cref="HsvColor"/> equivalent to the given RGBA values.</returns>
        public static HsvColor FromRgb(
            double r,
            double g,
            double b,
            double a = 1.0)
        {
            // Note: Conversion code is originally based on the C++ in WinUI (licensed MIT)
            // https://github.com/microsoft/microsoft-ui-xaml/blob/main/dev/Common/ColorConversion.cpp
            // This was used because it is the best documented and likely most optimized for performance
            // Alpha channel support was added

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
