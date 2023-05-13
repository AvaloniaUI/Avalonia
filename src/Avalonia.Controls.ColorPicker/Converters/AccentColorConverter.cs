using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Avalonia.Controls.Primitives.Converters
{
    /// <summary>
    /// Creates an accent color for a given base color value and step parameter.
    /// </summary>
    /// <remarks>
    /// This is a highly-specialized converter for the color picker.
    /// </remarks>
    public class AccentColorConverter : IValueConverter
    {
        /// <inheritdoc/>
        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            int accentStep;
            Color? rgbColor = null;
            HsvColor? hsvColor = null;

            if (value is Color valueColor)
            {
                rgbColor = valueColor;
            }
            else if (value is HslColor valueHslColor)
            {
                rgbColor = valueHslColor.ToRgb();
            }
            else if (value is HsvColor valueHsvColor)
            {
                hsvColor = valueHsvColor;
            }
            else if (value is SolidColorBrush valueBrush)
            {
                rgbColor = valueBrush.Color;
            }
            else
            {
                // Invalid color value provided
                return AvaloniaProperty.UnsetValue;
            }

            // Get the value component delta
            try
            {
                accentStep = int.Parse(parameter?.ToString() ?? "", CultureInfo.InvariantCulture);
            }
            catch
            {
                // Invalid parameter provided, unable to convert to integer
                return AvaloniaProperty.UnsetValue;
            }

            if (hsvColor == null &&
                rgbColor != null)
            {
                hsvColor = rgbColor.Value.ToHsv();
            }

            if (hsvColor != null)
            {
                return new SolidColorBrush(GetAccent(hsvColor.Value, accentStep).ToRgb());
            }
            else
            {
                return AvaloniaProperty.UnsetValue;
            }
        }

        /// <inheritdoc/>
        public object? ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            return AvaloniaProperty.UnsetValue;
        }

        /// <summary>
        /// This does not account for perceptual differences and also does not match with
        /// system accent color calculation.
        /// </summary>
        /// <remarks>
        /// Use the HSV representation as it's more perceptual.
        /// In most cases only the value is changed by a fixed percentage so the algorithm is reproducible.
        /// </remarks>
        /// <param name="hsvColor">The base color to calculate the accent from.</param>
        /// <param name="accentStep">The number of accent color steps to move.</param>
        /// <returns>The new accent color.</returns>
        public static HsvColor GetAccent(HsvColor hsvColor, int accentStep)
        {
            // Accent steps are taken from the FluentTheme:
            //   SystemAccentColors.cs -> CalculateAccentShades()
            //
            // This is currently believed to be the most accurate representation of the algorithm in Windows.
            // It replaces the previous implementation which adjusted Value +/- 10% per step.
            // Any changes to FluentTheme's accent color calculation should be copied here.

            const double dark1step = 28.5 / 255d;
            const double dark2step = 49 / 255d;
            const double dark3step = 74.5 / 255d;
            const double light1step = 39 / 255d;
            const double light2step = 70 / 255d;
            const double light3step = 103 / 255d;

            if (accentStep != 0)
            {
                // Temporary colors are used to preserve Hue through the RGB conversion
                // Otherwise, at Black/White hue information would be lost
                // This should be improved in the future with direct conversions to/from HSV/HSL.
                var hslAccent = hsvColor.ToHsl();

                double lightnessAdjustment = 0.0;
                switch (accentStep)
                {
                    case -3:
                        lightnessAdjustment = -dark3step;
                        break;
                    case -2:
                        lightnessAdjustment = -dark2step;
                        break;
                    case -1:
                        lightnessAdjustment = -dark1step;
                        break;
                    case 1:
                        lightnessAdjustment = light1step;
                        break;
                    case 2:
                        lightnessAdjustment = light2step;
                        break;
                    case 3:
                        lightnessAdjustment = light3step;
                        break;
                }

                var adjustedHsl = new HslColor(hslAccent.A, hslAccent.H, hslAccent.S, hslAccent.L + lightnessAdjustment);
                // Rounding may be required here

                return adjustedHsl.ToHsv();
            }
            else
            {
                return hsvColor;
            }
        }
    }
}
