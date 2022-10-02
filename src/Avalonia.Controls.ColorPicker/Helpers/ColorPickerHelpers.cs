﻿// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Contains internal, special-purpose helpers used with the color picker.
    /// </summary>
    internal static class ColorPickerHelpers
    {
        /// <summary>
        /// Generates a new bitmap of the specified size by changing a specific color component.
        /// This will produce a gradient representing a sweep of all possible values of the color component.
        /// </summary>
        /// <param name="width">The pixel width (X, horizontal) of the resulting bitmap.</param>
        /// <param name="height">The pixel height (Y, vertical) of the resulting bitmap.</param>
        /// <param name="orientation">The orientation of the resulting bitmap (gradient direction).</param>
        /// <param name="colorModel">The color model being used: RGBA or HSVA.</param>
        /// <param name="component">The specific color component to sweep.</param>
        /// <param name="baseHsvColor">The base HSV color used for components not being changed.</param>
        /// <param name="isAlphaMaxForced">Fix the alpha component value to maximum during calculation.
        /// This will remove any alpha/transparency from the other component backgrounds.</param>
        /// <param name="isSaturationValueMaxForced">Fix the saturation and value components to maximum
        /// during calculation with the HSVA color model.
        /// This will ensure colors are always discernible regardless of saturation/value.</param>
        /// <returns>A new bitmap representing a gradient of color component values.</returns>
        public static async Task<ArrayList<byte>> CreateComponentBitmapAsync(
            int width,
            int height,
            Orientation orientation,
            ColorModel colorModel,
            ColorComponent component,
            HsvColor baseHsvColor,
            bool isAlphaMaxForced,
            bool isSaturationValueMaxForced)
        {
            if (width == 0 || height == 0)
            {
                return new ArrayList<byte>(0);
            }

            var bitmap = await Task.Run<ArrayList<byte>>(() =>
            {
                int pixelDataIndex = 0;
                double componentStep;
                ArrayList<byte> bgraPixelData;
                Color baseRgbColor = Colors.White;
                Color rgbColor;
                int bgraPixelDataHeight;
                int bgraPixelDataWidth;

                // Allocate the buffer
                // BGRA formatted color components 1 byte each (4 bytes in a pixel)
                bgraPixelData       = new ArrayList<byte>(width * height * 4);
                bgraPixelDataHeight = height * 4;
                bgraPixelDataWidth  = width * 4;

                // Maximize alpha component value
                if (isAlphaMaxForced &&
                    component != ColorComponent.Alpha)
                {
                    baseHsvColor = new HsvColor(1.0, baseHsvColor.H, baseHsvColor.S, baseHsvColor.V);
                }

                // Convert HSV to RGB once
                if (colorModel == ColorModel.Rgba)
                {
                    baseRgbColor = baseHsvColor.ToRgb();
                }

                // Maximize Saturation and Value components when in HSVA mode
                if (isSaturationValueMaxForced &&
                    colorModel == ColorModel.Hsva &&
                    component != ColorComponent.Alpha)
                {
                    switch (component)
                    {
                        case ColorComponent.Component1:
                            baseHsvColor = new HsvColor(baseHsvColor.A, baseHsvColor.H, 1.0, 1.0);
                            break;
                        case ColorComponent.Component2:
                            baseHsvColor = new HsvColor(baseHsvColor.A, baseHsvColor.H, baseHsvColor.S, 1.0);
                            break;
                        case ColorComponent.Component3:
                            baseHsvColor = new HsvColor(baseHsvColor.A, baseHsvColor.H, 1.0, baseHsvColor.V);
                            break;
                    }
                }

                // Create the color component gradient
                if (orientation == Orientation.Horizontal)
                {
                    // Determine the numerical increment of the color steps within the component
                    if (colorModel == ColorModel.Hsva)
                    {
                        if (component == ColorComponent.Component1)
                        {
                            componentStep = 360.0 / width;
                        }
                        else
                        {
                            componentStep = 1.0 / width;
                        }
                    }
                    else
                    {
                        componentStep = 255.0 / width;
                    }

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            if (y == 0)
                            {
                                rgbColor = GetColor(x * componentStep);

                                // Get a new color
                                bgraPixelData[pixelDataIndex + 0] = Convert.ToByte(rgbColor.B * rgbColor.A / 255);
                                bgraPixelData[pixelDataIndex + 1] = Convert.ToByte(rgbColor.G * rgbColor.A / 255);
                                bgraPixelData[pixelDataIndex + 2] = Convert.ToByte(rgbColor.R * rgbColor.A / 255);
                                bgraPixelData[pixelDataIndex + 3] = rgbColor.A;
                            }
                            else
                            {
                                // Use the color in the row above
                                // Remember the pixel data is 1 dimensional instead of 2
                                bgraPixelData[pixelDataIndex + 0] = bgraPixelData[pixelDataIndex + 0 - bgraPixelDataWidth];
                                bgraPixelData[pixelDataIndex + 1] = bgraPixelData[pixelDataIndex + 1 - bgraPixelDataWidth];
                                bgraPixelData[pixelDataIndex + 2] = bgraPixelData[pixelDataIndex + 2 - bgraPixelDataWidth];
                                bgraPixelData[pixelDataIndex + 3] = bgraPixelData[pixelDataIndex + 3 - bgraPixelDataWidth];
                            }

                            pixelDataIndex += 4;
                        }
                    }
                }
                else
                {
                    // Determine the numerical increment of the color steps within the component
                    if (colorModel == ColorModel.Hsva)
                    {
                        if (component == ColorComponent.Component1)
                        {
                            componentStep = 360.0 / height;
                        }
                        else
                        {
                            componentStep = 1.0 / height;
                        }
                    }
                    else
                    {
                        componentStep = 255.0 / height;
                    }

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            if (x == 0)
                            {
                                // The lowest component value should be at the 'bottom' of the bitmap
                                rgbColor = GetColor((height - 1 - y) * componentStep);

                                // Get a new color
                                bgraPixelData[pixelDataIndex + 0] = Convert.ToByte(rgbColor.B * rgbColor.A / 255);
                                bgraPixelData[pixelDataIndex + 1] = Convert.ToByte(rgbColor.G * rgbColor.A / 255);
                                bgraPixelData[pixelDataIndex + 2] = Convert.ToByte(rgbColor.R * rgbColor.A / 255);
                                bgraPixelData[pixelDataIndex + 3] = rgbColor.A;
                            }
                            else
                            {
                                // Use the color in the column to the left
                                // Remember the pixel data is 1 dimensional instead of 2
                                bgraPixelData[pixelDataIndex + 0] = bgraPixelData[pixelDataIndex - 4];
                                bgraPixelData[pixelDataIndex + 1] = bgraPixelData[pixelDataIndex - 3];
                                bgraPixelData[pixelDataIndex + 2] = bgraPixelData[pixelDataIndex - 2];
                                bgraPixelData[pixelDataIndex + 3] = bgraPixelData[pixelDataIndex - 1];
                            }

                            pixelDataIndex += 4;
                        }
                    }
                }

                Color GetColor(double componentValue)
                {
                    Color newRgbColor = Colors.White;

                    switch (component)
                    {
                        case ColorComponent.Component1:
                            {
                                if (colorModel == ColorModel.Hsva)
                                {
                                    // Sweep hue
                                    newRgbColor = HsvColor.ToRgb(
                                        MathUtilities.Clamp(componentValue, 0.0, 360.0),
                                        baseHsvColor.S,
                                        baseHsvColor.V,
                                        baseHsvColor.A);
                                }
                                else
                                {
                                    // Sweep red
                                    newRgbColor = new Color(
                                        baseRgbColor.A,
                                        Convert.ToByte(MathUtilities.Clamp(componentValue, 0.0, 255.0)),
                                        baseRgbColor.G,
                                        baseRgbColor.B);
                                }

                                break;
                            }
                        case ColorComponent.Component2:
                            {
                                if (colorModel == ColorModel.Hsva)
                                {
                                    // Sweep saturation
                                    newRgbColor = HsvColor.ToRgb(
                                        baseHsvColor.H,
                                        MathUtilities.Clamp(componentValue, 0.0, 1.0),
                                        baseHsvColor.V,
                                        baseHsvColor.A);
                                }
                                else
                                {
                                    // Sweep green
                                    newRgbColor = new Color(
                                        baseRgbColor.A,
                                        baseRgbColor.R,
                                        Convert.ToByte(MathUtilities.Clamp(componentValue, 0.0, 255.0)),
                                        baseRgbColor.B);
                                }

                                break;
                            }
                        case ColorComponent.Component3:
                            {
                                if (colorModel == ColorModel.Hsva)
                                {
                                    // Sweep value
                                    newRgbColor = HsvColor.ToRgb(
                                        baseHsvColor.H,
                                        baseHsvColor.S,
                                        MathUtilities.Clamp(componentValue, 0.0, 1.0),
                                        baseHsvColor.A);
                                }
                                else
                                {
                                    // Sweep blue
                                    newRgbColor = new Color(
                                        baseRgbColor.A,
                                        baseRgbColor.R,
                                        baseRgbColor.G,
                                        Convert.ToByte(MathUtilities.Clamp(componentValue, 0.0, 255.0)));
                                }

                                break;
                            }
                        case ColorComponent.Alpha:
                            {
                                if (colorModel == ColorModel.Hsva)
                                {
                                    // Sweep alpha
                                    newRgbColor = HsvColor.ToRgb(
                                        baseHsvColor.H,
                                        baseHsvColor.S,
                                        baseHsvColor.V,
                                        MathUtilities.Clamp(componentValue, 0.0, 1.0));
                                }
                                else
                                {
                                    // Sweep alpha
                                    newRgbColor = new Color(
                                        Convert.ToByte(MathUtilities.Clamp(componentValue, 0.0, 255.0)),
                                        baseRgbColor.R,
                                        baseRgbColor.G,
                                        baseRgbColor.B);
                                }

                                break;
                            }
                    }

                    return newRgbColor;
                }

                return bgraPixelData;
            });

            return bitmap;
        }

        public static Hsv IncrementColorComponent(
            Hsv originalHsv,
            HsvComponent component,
            IncrementDirection direction,
            IncrementAmount amount,
            bool shouldWrap,
            double minBound,
            double maxBound)
        {
            Hsv newHsv = originalHsv;

            if (amount == IncrementAmount.Small || !ColorHelper.ToDisplayNameExists)
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
                switch (component)
                {
                    case HsvComponent.Hue:
                        valueToIncrement = ref newHsv.H;
                        incrementAmount = amount == IncrementAmount.Small ? 1 : 30;
                        break;

                    case HsvComponent.Saturation:
                        valueToIncrement = ref newHsv.S;
                        incrementAmount = amount == IncrementAmount.Small ? 1 : 10;
                        break;

                    case HsvComponent.Value:
                        valueToIncrement = ref newHsv.V;
                        incrementAmount = amount == IncrementAmount.Small ? 1 : 10;
                        break;

                    default:
                        throw new InvalidOperationException("Invalid HsvComponent.");
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
                if (component == HsvComponent.Saturation ||
                    component == HsvComponent.Value)
                {
                    minBound /= 100;
                    maxBound /= 100;
                }

                newHsv = FindNextNamedColor(originalHsv, component, direction, shouldWrap, minBound, maxBound);
            }

            return newHsv;
        }

        public static Hsv FindNextNamedColor(
            Hsv originalHsv,
            HsvComponent component,
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
            
            string originalColorName = ColorHelper.ToDisplayName(originalHsv.ToRgb().ToColor());
            string newColorName = originalColorName;

            // Note: *newValue replaced with ref local variable for C#, must be initialized
            double originalValue = 0.0;
            ref double newValue = ref newHsv.H;
            double incrementAmount = 0.0;

            switch (component)
            {
                case HsvComponent.Hue:
                    originalValue = originalHsv.H;
                    newValue = ref newHsv.H;
                    incrementAmount = 1;
                    break;

                case HsvComponent.Saturation:
                    originalValue = originalHsv.S;
                    newValue = ref newHsv.S;
                    incrementAmount = 0.01;
                    break;

                case HsvComponent.Value:
                    originalValue = originalHsv.V;
                    newValue = ref newHsv.V;
                    incrementAmount = 0.01;
                    break;

                default:
                    throw new InvalidOperationException("Invalid HsvComponent.");
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
                        newColorName = ColorHelper.ToDisplayName(newHsv.ToRgb().ToColor());
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
                        newColorName = ColorHelper.ToDisplayName(newHsv.ToRgb().ToColor());
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

                newColorName = ColorHelper.ToDisplayName(newHsv.ToRgb().ToColor());
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

                switch (component)
                {
                    case HsvComponent.Hue:
                        startValue = ref startHsv.H;
                        currentValue = ref currentHsv.H;
                        wrapIncrement = 360.0;
                        break;

                    case HsvComponent.Saturation:
                        startValue = ref startHsv.S;
                        currentValue = ref currentHsv.S;
                        wrapIncrement = 1.0;
                        break;

                    case HsvComponent.Value:
                        startValue = ref startHsv.V;
                        currentValue = ref currentHsv.V;
                        wrapIncrement = 1.0;
                        break;

                    default:
                        throw new InvalidOperationException("Invalid HsvComponent.");
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

                    currentColorName = ColorHelper.ToDisplayName(currentHsv.ToRgb().ToColor());
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

        /// <summary>
        /// Converts the given raw BGRA pre-multiplied alpha pixel data into a bitmap.
        /// </summary>
        /// <param name="bgraPixelData">The bitmap (in raw BGRA pre-multiplied alpha pixels).</param>
        /// <param name="pixelWidth">The pixel width of the bitmap.</param>
        /// <param name="pixelHeight">The pixel height of the bitmap.</param>
        /// <returns>A new <see cref="WriteableBitmap"/>.</returns>
        public static WriteableBitmap CreateBitmapFromPixelData(
            ArrayList<byte> bgraPixelData,
            int pixelWidth,
            int pixelHeight)
        {
            // Standard may need to change on some devices
            Vector dpi = new Vector(96, 96);

            var bitmap = new WriteableBitmap(
                new PixelSize(pixelWidth, pixelHeight),
                dpi,
                PixelFormat.Bgra8888,
                AlphaFormat.Premul);

            using (var frameBuffer = bitmap.Lock())
            {
                Marshal.Copy(bgraPixelData.Array, 0, frameBuffer.Address, bgraPixelData.Array.Length);
            }

            return bitmap;
        }

        /// <summary>
        /// Updates the given <see cref="WriteableBitmap"/> with new, raw BGRA pre-multiplied alpha pixel data.
        /// WARNING: THIS METHOD IS CURRENTLY PROVIDED AS REFERENCE BUT CAUSES INTERMITTENT CRASHES IF USED.
        /// WARNING: The bitmap's width, height and byte count MUST not have changed and MUST be enforced externally.
        /// </summary>
        /// <param name="bitmap">The existing <see cref="WriteableBitmap"/> to update.</param>
        /// <param name="bgraPixelData">The bitmap (in raw BGRA pre-multiplied alpha pixels).</param>
        public static void UpdateBitmapFromPixelData(
            WriteableBitmap bitmap,
            ArrayList<byte> bgraPixelData)
        {
            using (var frameBuffer = bitmap.Lock())
            {
                Marshal.Copy(bgraPixelData.Array, 0, frameBuffer.Address, bgraPixelData.Array.Length);
            }

            return;
        }
    }
}
