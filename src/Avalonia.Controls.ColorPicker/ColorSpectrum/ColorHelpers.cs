// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Avalonia.Controls.Primitives
{
    internal static class ColorHelpers
    {
        public const int CheckerSize = 4;

        public static bool ToDisplayNameExists
        {
            get => false;
        }

        public static string ToDisplayName(Color color)
        {
            return string.Empty;
        }

        public static Hsv IncrementColorChannel(
            Hsv originalHsv,
            HsvChannel channel,
            IncrementDirection direction,
            IncrementAmount amount,
            bool shouldWrap,
            double minBound,
            double maxBound)
        {
            Hsv newHsv = originalHsv;

            if (amount == IncrementAmount.Small || !ToDisplayNameExists)
            {
                // In order to avoid working with small values that can incur rounding issues,
                // we'll multiple saturation and value by 100 to put them in the range of 0-100 instead of 0-1.
                newHsv.S *= 100;
                newHsv.V *= 100;

                // Note: *valueToIncrement replaced with ref local variable for C#, must be initialized
                ref double valueToIncrement = ref newHsv.H;
                double incrementAmount = 0.0;

                // If we're adding a small increment, then we'll just add or subtract 1.
                // If we're adding a large increment, then we want to snap to the next
                // or previous major value - for hue, this is every increment of 30;
                // for saturation and value, this is every increment of 10.
                switch (channel)
                {
                    case HsvChannel.Hue:
                        valueToIncrement = ref newHsv.H;
                        incrementAmount = amount == IncrementAmount.Small ? 1 : 30;
                        break;

                    case HsvChannel.Saturation:
                        valueToIncrement = ref newHsv.S;
                        incrementAmount = amount == IncrementAmount.Small ? 1 : 10;
                        break;

                    case HsvChannel.Value:
                        valueToIncrement = ref newHsv.V;
                        incrementAmount = amount == IncrementAmount.Small ? 1 : 10;
                        break;

                    default:
                        throw new InvalidOperationException("Invalid HsvChannel.");
                }

                double previousValue = valueToIncrement;

                valueToIncrement += (direction == IncrementDirection.Lower ? -incrementAmount : incrementAmount);

                // If the value has reached outside the bounds, we were previous at the boundary, and we should wrap,
                // then we'll place the selection on the other side of the spectrum.
                // Otherwise, we'll place it on the boundary that was exceeded.
                if (valueToIncrement < minBound)
                {
                    valueToIncrement = (shouldWrap && previousValue == minBound) ? maxBound : minBound;
                }

                if (valueToIncrement > maxBound)
                {
                    valueToIncrement = (shouldWrap && previousValue == maxBound) ? minBound : maxBound;
                }

                // We multiplied saturation and value by 100 previously, so now we want to put them back in the 0-1 range.
                newHsv.S /= 100;
                newHsv.V /= 100;
            }
            else
            {
                // While working with named colors, we're going to need to be working in actual HSV units,
                // so we'll divide the min bound and max bound by 100 in the case of saturation or value,
                // since we'll have received units between 0-100 and we need them within 0-1.
                if (channel == HsvChannel.Saturation ||
                    channel == HsvChannel.Value)
                {
                    minBound /= 100;
                    maxBound /= 100;
                }

                newHsv = FindNextNamedColor(originalHsv, channel, direction, shouldWrap, minBound, maxBound);
            }

            return newHsv;
        }

        public static Hsv FindNextNamedColor(
            Hsv originalHsv,
            HsvChannel channel,
            IncrementDirection direction,
            bool shouldWrap,
            double minBound,
            double maxBound)
        {
            // There's no easy way to directly get the next named color, so what we'll do
            // is just iterate in the direction that we want to find it until we find a color
            // in that direction that has a color name different than our current color name.
            // Once we find a new color name, then we'll iterate across that color name until
            // we find its bounds on the other side, and then select the color that is exactly
            // in the middle of that color's bounds.
            Hsv newHsv = originalHsv;
            
            string originalColorName = ColorHelpers.ToDisplayName(originalHsv.ToRgb().ToColor());
            string newColorName = originalColorName;

            // Note: *newValue replaced with ref local variable for C#, must be initialized
            double originalValue = 0.0;
            ref double newValue = ref newHsv.H;
            double incrementAmount = 0.0;

            switch (channel)
            {
                case HsvChannel.Hue:
                    originalValue = originalHsv.H;
                    newValue = ref newHsv.H;
                    incrementAmount = 1;
                    break;

                case HsvChannel.Saturation:
                    originalValue = originalHsv.S;
                    newValue = ref newHsv.S;
                    incrementAmount = 0.01;
                    break;

                case HsvChannel.Value:
                    originalValue = originalHsv.V;
                    newValue = ref newHsv.V;
                    incrementAmount = 0.01;
                    break;

                default:
                    throw new InvalidOperationException("Invalid HsvChannel.");
            }

            bool shouldFindMidPoint = true;

            while (newColorName == originalColorName)
            {
                double previousValue = newValue;
                newValue += (direction == IncrementDirection.Lower ? -1 : 1) * incrementAmount;

                bool justWrapped = false;

                // If we've hit a boundary, then either we should wrap or we shouldn't.
                // If we should, then we'll perform that wrapping if we were previously up against
                // the boundary that we've now hit.  Otherwise, we'll stop at that boundary.
                if (newValue > maxBound)
                {
                    if (shouldWrap)
                    {
                        newValue = minBound;
                        justWrapped = true;
                    }
                    else
                    {
                        newValue = maxBound;
                        shouldFindMidPoint = false;
                        newColorName = ColorHelpers.ToDisplayName(newHsv.ToRgb().ToColor());
                        break;
                    }
                }
                else if (newValue < minBound)
                {
                    if (shouldWrap)
                    {
                        newValue = maxBound;
                        justWrapped = true;
                    }
                    else
                    {
                        newValue = minBound;
                        shouldFindMidPoint = false;
                        newColorName = ColorHelpers.ToDisplayName(newHsv.ToRgb().ToColor());
                        break;
                    }
                }

                if (!justWrapped &&
                    previousValue != originalValue &&
                    Math.Sign(newValue - originalValue) != Math.Sign(previousValue - originalValue))
                {
                    // If we've wrapped all the way back to the start and have failed to find a new color name,
                    // then we'll just quit - there isn't a new color name that we're going to find.
                    shouldFindMidPoint = false;
                    break;
                }

                newColorName = ColorHelpers.ToDisplayName(newHsv.ToRgb().ToColor());
            }

            if (shouldFindMidPoint)
            {
                Hsv startHsv = newHsv;
                Hsv currentHsv = startHsv;
                double startEndOffset = 0;
                string currentColorName = newColorName;

                // Note: *startValue/*currentValue replaced with ref local variables for C#, must be initialized
                ref double startValue = ref startHsv.H;
                ref double currentValue = ref currentHsv.H;
                double wrapIncrement = 0;

                switch (channel)
                {
                    case HsvChannel.Hue:
                        startValue = ref startHsv.H;
                        currentValue = ref currentHsv.H;
                        wrapIncrement = 360.0;
                        break;

                    case HsvChannel.Saturation:
                        startValue = ref startHsv.S;
                        currentValue = ref currentHsv.S;
                        wrapIncrement = 1.0;
                        break;

                    case HsvChannel.Value:
                        startValue = ref startHsv.V;
                        currentValue = ref currentHsv.V;
                        wrapIncrement = 1.0;
                        break;

                    default:
                        throw new InvalidOperationException("Invalid HsvChannel.");
                }

                while (newColorName == currentColorName)
                {
                    currentValue += (direction == IncrementDirection.Lower ? -1 : 1) * incrementAmount;

                    // If we've hit a boundary, then either we should wrap or we shouldn't.
                    // If we should, then we'll perform that wrapping if we were previously up against
                    // the boundary that we've now hit.  Otherwise, we'll stop at that boundary.
                    if (currentValue > maxBound)
                    {
                        if (shouldWrap)
                        {
                            currentValue = minBound;
                            startEndOffset = maxBound - minBound;
                        }
                        else
                        {
                            currentValue = maxBound;
                            break;
                        }
                    }
                    else if (currentValue < minBound)
                    {
                        if (shouldWrap)
                        {
                            currentValue = maxBound;
                            startEndOffset = minBound - maxBound;
                        }
                        else
                        {
                            currentValue = minBound;
                            break;
                        }
                    }

                    currentColorName = ColorHelpers.ToDisplayName(currentHsv.ToRgb().ToColor());
                }

                newValue = (startValue + currentValue + startEndOffset) / 2;

                // Dividing by 2 may have gotten us halfway through a single step, so we'll
                // remove that half-step if it exists.
                double leftoverValue = Math.Abs(newValue);

                while (leftoverValue > incrementAmount)
                {
                    leftoverValue -= incrementAmount;
                }

                newValue -= leftoverValue;

                while (newValue < minBound)
                {
                    newValue += wrapIncrement;
                }

                while (newValue > maxBound)
                {
                    newValue -= wrapIncrement;
                }
            }

            return newHsv;
        }

        public static double IncrementAlphaChannel(
            double originalAlpha,
            IncrementDirection direction,
            IncrementAmount amount,
            bool shouldWrap,
            double minBound,
            double maxBound)
        {
            // In order to avoid working with small values that can incur rounding issues,
            // we'll multiple alpha by 100 to put it in the range of 0-100 instead of 0-1.
            originalAlpha *= 100;

            const double smallIncrementAmount = 1;
            const double largeIncrementAmount = 10;

            if (amount == IncrementAmount.Small)
            {
                originalAlpha += (direction == IncrementDirection.Lower ? -1 : 1) * smallIncrementAmount;
            }
            else
            {
                if (direction == IncrementDirection.Lower)
                {
                    originalAlpha = Math.Ceiling((originalAlpha - largeIncrementAmount) / largeIncrementAmount) * largeIncrementAmount;
                }
                else
                {
                    originalAlpha = Math.Floor((originalAlpha + largeIncrementAmount) / largeIncrementAmount) * largeIncrementAmount;
                }
            }

            // If the value has reached outside the bounds and we should wrap, then we'll place the selection
            // on the other side of the spectrum.  Otherwise, we'll place it on the boundary that was exceeded.
            if (originalAlpha < minBound)
            {
                originalAlpha = shouldWrap ? maxBound : minBound;
            }

            if (originalAlpha > maxBound)
            {
                originalAlpha = shouldWrap ? minBound : maxBound;
            }

            // We multiplied alpha by 100 previously, so now we want to put it back in the 0-1 range.
            return originalAlpha / 100;
        }

        public static WriteableBitmap CreateBitmapFromPixelData(
            int pixelWidth,
            int pixelHeight,
            List<byte> bgraPixelData)
        {
            Vector dpi = new Vector(96, 96); // Standard may need to change on some devices

            WriteableBitmap bitmap = new WriteableBitmap(
                new PixelSize(pixelWidth, pixelHeight),
                dpi,
                PixelFormat.Bgra8888,
                AlphaFormat.Premul);

            // Warning: This is highly questionable
            using (var frameBuffer = bitmap.Lock())
            {
                Marshal.Copy(bgraPixelData.ToArray(), 0, frameBuffer.Address, bgraPixelData.Count);
            }

            return bitmap;
        }

        /// <summary>
        /// Gets the relative (perceptual) luminance/brightness of the given color.
        /// 1 is closer to white while 0 is closer to black.
        /// </summary>
        /// <param name="color">The color to calculate relative luminance for.</param>
        /// <returns>The relative (perceptual) luminance/brightness of the given color.</returns>
        public static double GetRelativeLuminance(Color color)
        {
            // The equation for relative luminance is given by
            //
            // L = 0.2126 * Rg + 0.7152 * Gg + 0.0722 * Bg
            //
            // where Xg = { X/3294 if X <= 10, (R/269 + 0.0513)^2.4 otherwise }
            //
            // If L is closer to 1, then the color is closer to white; if it is closer to 0,
            // then the color is closer to black.  This is based on the fact that the human
            // eye perceives green to be much brighter than red, which in turn is perceived to be
            // brighter than blue.

            double rg = color.R <= 10 ? color.R / 3294.0 : Math.Pow(color.R / 269.0 + 0.0513, 2.4);
            double gg = color.G <= 10 ? color.G / 3294.0 : Math.Pow(color.G / 269.0 + 0.0513, 2.4);
            double bg = color.B <= 10 ? color.B / 3294.0 : Math.Pow(color.B / 269.0 + 0.0513, 2.4);

            return (0.2126 * rg + 0.7152 * gg + 0.0722 * bg);
        }
    }
}
