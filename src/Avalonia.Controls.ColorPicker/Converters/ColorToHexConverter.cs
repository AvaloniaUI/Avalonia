using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Avalonia.Controls.Converters
{
    /// <summary>
    /// Converts a color to a hex string and vice versa.
    /// </summary>
    public class ColorToHexConverter : IValueConverter
    {
        /// <inheritdoc/>
        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            Color color;
            bool includeSymbol = parameter as bool? ?? false;

            if (value is Color valueColor)
            {
                color = valueColor;
            }
            else if (value is HslColor valueHslColor)
            {
                color = valueHslColor.ToRgb();
            }
            else if (value is HsvColor valueHsvColor)
            {
                color = valueHsvColor.ToRgb();
            }
            else if (value is SolidColorBrush valueBrush)
            {
                color = valueBrush.Color;
            }
            else
            {
                // Invalid color value provided
                return AvaloniaProperty.UnsetValue;
            }

            string hexColor = color.ToUint32().ToString("x8", CultureInfo.InvariantCulture).ToUpperInvariant();

            if (includeSymbol == false)
            {
                // TODO: When .net standard 2.0 is dropped, replace the below line
                //hexColor = hexColor.Replace("#", string.Empty, StringComparison.Ordinal);
                hexColor = hexColor.Replace("#", string.Empty);
            }

            return hexColor;
        }

        /// <inheritdoc/>
        public object? ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            string hexValue = value?.ToString() ?? string.Empty;

            if (Color.TryParse(hexValue, out Color color))
            {
                return color;
            }
            else if (hexValue.StartsWith("#", StringComparison.Ordinal) == false &&
                     Color.TryParse("#" + hexValue, out Color color2))
            {
                return color2;
            }
            else
            {
                // Invalid hex color value provided
                return AvaloniaProperty.UnsetValue;
            }
        }
    }
}
