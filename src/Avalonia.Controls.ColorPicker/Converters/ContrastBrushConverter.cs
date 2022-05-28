using System;
using System.Globalization;
using Avalonia.Controls.Converters;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Avalonia.Controls.Primitives.Converters
{
    /// <summary>
    /// Gets a <see cref="SolidColorBrush"/>, either black or white, depending on the luminance of the supplied color.
    /// A default color supplied in the converter parameter may be returned if alpha is below the set threshold.
    /// </summary>
    /// <remarks>
    /// This is a highly-specialized converter for the color picker.
    /// </remarks>
    public class ContrastBrushConverter : IValueConverter
    {
        private ToColorConverter toColorConverter = new ToColorConverter();

        /// <summary>
        /// Gets or sets the alpha channel threshold below which a default color is used instead of black/white.
        /// </summary>
        public byte AlphaThreshold { get; set; } = 128;

        /// <inheritdoc/>
        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            Color comparisonColor;
            Color? defaultColor = null;

            // Get the changing color to compare against
            var convertedValue = toColorConverter.Convert(value, targetType, parameter, culture);
            if (convertedValue is Color valueColor)
            {
                comparisonColor = valueColor;
            }
            else
            {
                // Invalid color value provided
                return AvaloniaProperty.UnsetValue;
            }

            // Get the default color when transparency is high
            var convertedParameter = toColorConverter.Convert(parameter, targetType, parameter, culture);
            if (convertedParameter is Color parameterColor)
            {
                defaultColor = parameterColor;
            }

            if (comparisonColor.A < AlphaThreshold &&
                defaultColor.HasValue)
            {
                // If the transparency is less than the threshold, just use the default brush
                // This can commonly be something like the TextControlForeground brush
                return new SolidColorBrush(defaultColor.Value);
            }
            else
            {
                // Chose a white/black brush based on contrast to the base color
                if (ColorHelper.GetRelativeLuminance(comparisonColor) <= 0.5)
                {
                    // Dark color, return light for contrast
                    return new SolidColorBrush(Colors.White);
                }
                else
                {
                    // Bright color, return dark for contrast
                    return new SolidColorBrush(Colors.Black);
                }
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
    }
}
