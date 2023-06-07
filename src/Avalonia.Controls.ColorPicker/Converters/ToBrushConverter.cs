using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Avalonia.Controls.Converters
{
    /// <summary>
    /// Converts the given value into an <see cref="IBrush"/> when a conversion is possible.
    /// </summary>
    public class ToBrushConverter : IValueConverter
    {
        /// <inheritdoc/>
        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            if (value is IBrush brush)
            {
                return brush;
            }
            else if (value is Color valueColor)
            {
                return new SolidColorBrush(valueColor);
            }
            else if (value is HslColor valueHslColor)
            {
                return new SolidColorBrush(valueHslColor.ToRgb());
            }
            else if (value is HsvColor valueHsvColor)
            {
                return new SolidColorBrush(valueHsvColor.ToRgb());
            }

            return AvaloniaProperty.UnsetValue;
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
