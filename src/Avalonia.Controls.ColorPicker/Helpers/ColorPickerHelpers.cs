// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Collections.Pooled;
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
        /// <param name="isAlphaVisible">Whether the alpha component is visible and rendered in the bitmap.
        /// This property is ignored when the alpha component itself is being rendered.</param>
        /// <param name="isPerceptive">Whether the slider adapts rendering to improve user-perception over exactness.
        /// This will ensure colors are always discernible.</param>
        /// <returns>A new bitmap representing a gradient of color component values.</returns>
        public static Task CreateComponentBitmapAsync(
            PooledList<byte> bgraPixelData,
            int width,
            int height,
            Orientation orientation,
            ColorModel colorModel,
            ColorComponent component,
            HsvColor baseHsvColor,
            bool isAlphaVisible,
            bool isPerceptive)
        {
            if (width == 0 || height == 0)
            {
                return Task.CompletedTask;
            }

            return Task.Run(() =>
            {
                int pixelDataIndex = 0;
                double componentStep;
                Color baseRgbColor = Colors.White;
                Color rgbColor;
                int bgraPixelDataHeight;
                int bgraPixelDataWidth;

                // BGRA formatted color components 1 byte each (4 bytes in a pixel)
                bgraPixelDataHeight = height * 4;
                bgraPixelDataWidth  = width * 4;

                // Maximize alpha component value
                if (isAlphaVisible == false &&
                    component != ColorComponent.Alpha)
                {
                    baseHsvColor = new HsvColor(1.0, baseHsvColor.H, baseHsvColor.S, baseHsvColor.V);
                }

                // Convert HSV to RGB once
                if (colorModel == ColorModel.Rgba)
                {
                    baseRgbColor = baseHsvColor.ToRgb();
                }

                // Apply any perceptive adjustments to the color
                if (isPerceptive &&
                    component != ColorComponent.Alpha)
                {
                    if (colorModel == ColorModel.Hsva)
                    {
                        // Maximize Saturation and Value components
                        switch (component)
                        {
                            case ColorComponent.Component1: // Hue
                                baseHsvColor = new HsvColor(baseHsvColor.A, baseHsvColor.H, 1.0, 1.0);
                                break;
                            case ColorComponent.Component2: // Saturation
                                baseHsvColor = new HsvColor(baseHsvColor.A, baseHsvColor.H, baseHsvColor.S, 1.0);
                                break;
                            case ColorComponent.Component3: // Value
                                baseHsvColor = new HsvColor(baseHsvColor.A, baseHsvColor.H, 1.0, baseHsvColor.V);
                                break;
                        }
                    }
                    else
                    {
                        // Minimize component values other than the current one
                        switch (component)
                        {
                            case ColorComponent.Component1: // Red
                                baseRgbColor = new Color(baseRgbColor.A, baseRgbColor.R, 0, 0);
                                break;
                            case ColorComponent.Component2: // Green
                                baseRgbColor = new Color(baseRgbColor.A, 0, baseRgbColor.G, 0);
                                break;
                            case ColorComponent.Component3: // Blue
                                baseRgbColor = new Color(baseRgbColor.A, 0, 0, baseRgbColor.B);
                                break;
                        }
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
        public static unsafe Bitmap CreateBitmapFromPixelData(
            PooledList<byte> bgraPixelData,
            int pixelWidth,
            int pixelHeight)
        {
            fixed (byte* array = bgraPixelData.Span)
            {
                return new Bitmap(PixelFormat.Bgra8888, AlphaFormat.Premul, new IntPtr(array),
                    new PixelSize(pixelWidth, pixelHeight),
                    new Vector(96, 96), pixelWidth * 4);
            }
        }
    }
}
