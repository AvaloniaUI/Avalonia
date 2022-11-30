using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Avalonia.Controls.Converters
{
    /// <summary>
    /// Converts a <see cref="DateTime"/> into a <see cref="DateTimeOffset"/> and vice versa.
    /// </summary>
    public class DateTimeToDateTimeOffsetConverter : IValueConverter
    {
        /// <inheritdoc/>
        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                return new DateTimeOffset(dateTime);
            }
            else if (value is DateTimeOffset dateTimeOffset)
            {
                return dateTimeOffset.DateTime;
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
            if (value is DateTimeOffset dateTimeOffset)
            {
                return dateTimeOffset.DateTime;
            }
            else if (value is DateTime dateTime)
            {
                return new DateTimeOffset(dateTime);
            }
            else
            {
                return AvaloniaProperty.UnsetValue;
            }
        }
    }
}
