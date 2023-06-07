using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Utilities;

namespace Avalonia.Controls.Converters
{
    /// <summary>
    /// Converts the given value into a <see cref="Color"/> when a conversion is possible.
    /// </summary>
    public class ToColorConverter : IValueConverter
    {
        /// <inheritdoc/>
        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            if (value is Color valueColor)
            {
                return valueColor;
            }
            else if (value is HslColor valueHslColor)
            {
                return valueHslColor.ToRgb();
            }
            else if (value is HsvColor valueHsvColor)
            {
                return valueHsvColor.ToRgb();
            }
            else if (value is SolidColorBrush valueBrush)
            {
                // A brush may have an opacity set along with alpha transparency
                double alpha = valueBrush.Color.A * valueBrush.Opacity;

                return new Color(
                    (byte)MathUtilities.Clamp(alpha, 0x00, 0xFF),
                    valueBrush.Color.R,
                    valueBrush.Color.G,
                    valueBrush.Color.B);
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
