using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace FinancialManager.UWP
{
    /// <summary>
    /// Gets a color, either black or white, depending on the perceived brightness of the supplied color.
    /// </summary>
    public class ContrastBrushConverter : IValueConverter
    {
        /// <summary>
        /// Gets or sets the alpha channel threshold below which a default color is used instead of black/white.
        /// </summary>
        public byte AlphaThreshold { get; set; } = 128;

        /// <inheritdoc/>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            Color comparisonColor;
            Color? defaultColor = null;

            // Get the changing color to compare against
            if (value is Color valueColor)
            {
                comparisonColor = valueColor;
            }
            else if (value is HsvColor valueHsvColor)
            {
                comparisonColor = valueHsvColor.ToRgb();
            }
            else if (value is SolidColorBrush valueBrush)
            {
                comparisonColor = valueBrush.Color;
            }
            else
            {
                // Invalid color value provided
                return AvaloniaProperty.UnsetValue;
            }

            // Get the default color when transparency is high
            if (parameter is Color parameterColor)
            {
                defaultColor = parameterColor;
            }
            else if (parameter is HsvColor parameterHsvColor)
            {
                defaultColor = parameterHsvColor.ToRgb();
            }
            else if (parameter is SolidColorBrush parameterBrush)
            {
                defaultColor = parameterBrush.Color;
            }

            if (comparisonColor.A < AlphaThreshold &&
                defaultColor.HasValue)
            {
                // If the transparency is less than the threshold just use the default brush
                // This can commonly be something like the TextControlForeground brush
                return new SolidColorBrush(defaultColor.Value);
            }
            else
            {
                // Chose a white/black brush based on contrast to the base color
                if (ColorHelpers.GetRelativeLuminance(comparisonColor) > 0.5)
                {
                    // Bright color, use a dark for contrast
                    return new SolidColorBrush(Colors.Black);
                }
                else
                {
                    // Dark color, use a light for contrast
                    return new SolidColorBrush(Colors.White);
                }
            }
        }

        /// <inheritdoc/>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
