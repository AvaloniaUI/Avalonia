using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Avalonia.Controls.Converters
{
    /// <summary>
    /// Converts one corner of a <see cref="CornerRadius"/> to its double value.
    /// </summary>
    public class CornerRadiusToDoubleConverter : IValueConverter
    {
        /// <summary>
        /// Gets or sets the specific corner of the <see cref="CornerRadius"/> to convert to double.
        /// </summary>
        public CornerRadiusCorner Corner { get; set; }

        /// <inheritdoc/>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (!(value is CornerRadius cornerRadius))
            {
                return AvaloniaProperty.UnsetValue;
            }

            switch (Corner)
            {
                case CornerRadiusCorner.TopLeft:
                    return cornerRadius.TopLeft;
                case CornerRadiusCorner.TopRight:
                    return cornerRadius.TopRight;
                case CornerRadiusCorner.BottomRight:
                    return cornerRadius.BottomRight;
                case CornerRadiusCorner.BottomLeft:
                    return cornerRadius.BottomLeft;
                default:
                    return 0.0;
            }
        }

        /// <inheritdoc/>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
