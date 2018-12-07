// Original source was written by Romain Guy and Francois Blavoet 
// and adopted from LottieSharp Project (https://github.com/ascora/LottieSharp).

using System;
using Avalonia.Logging;
using Avalonia.Media;

namespace Avalonia.Animation
{
    /// <summary>
    /// Animator that interpolates <see cref="Color"/> through 
    /// gamma sRGB color space for better visual result.
    /// </summary>
    public class ColorAnimator : Animator<Color>
    {
        // Opto-electronic conversion function for the sRGB color space
        // Takes a gamma-encoded sRGB value and converts it to a linear sRGB value
        private static double OECF_sRGB(double linear)
        {
            // IEC 61966-2-1:1999
            return linear <= 0.0031308d ? linear * 12.92d : (double)(Math.Pow(linear, 1.0d / 2.4d) * 1.055d - 0.055d);
        }

        // Electro-optical conversion function for the sRGB color space
        // Takes a linear sRGB value and converts it to a gamma-encoded sRGB value
        private static double EOCF_sRGB(double srgb)
        {
            // IEC 61966-2-1:1999
            return srgb <= 0.04045d ? srgb / 12.92d : (double)Math.Pow((srgb + 0.055d) / 1.055d, 2.4d);
        }

        protected override Color Interpolate(double fraction, Color start, Color end)
        {
            var startA = start.A / 255d;
            var startR = start.R / 255d;
            var startG = start.G / 255d;
            var startB = start.B / 255d;

            var endA = end.A / 255d;
            var endR = end.R / 255d;
            var endG = end.G / 255d;
            var endB = end.B / 255d;

            // convert from sRGB to linear
            startR = EOCF_sRGB(startR);
            startG = EOCF_sRGB(startG);
            startB = EOCF_sRGB(startB);

            endR = EOCF_sRGB(endR);
            endG = EOCF_sRGB(endG);
            endB = EOCF_sRGB(endB);

            // compute the interpolated color in linear space
            var a = startA + fraction * (endA - startA);
            var r = startR + fraction * (endR - startR);
            var g = startG + fraction * (endG - startG);
            var b = startB + fraction * (endB - startB);

            // convert back to sRGB in the [0..255] range
            a = a * 255d;
            r = OECF_sRGB(r) * 255d;
            g = OECF_sRGB(g) * 255d;
            b = OECF_sRGB(b) * 255d;

            return new Color((byte)Math.Round(r), (byte)Math.Round(g), (byte)Math.Round(b), (byte)Math.Round(a));
        }
    }
}