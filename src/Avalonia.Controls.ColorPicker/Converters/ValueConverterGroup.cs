using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Avalonia.Controls.Primitives.Converters
{
    /// <summary>
    /// Converter to chain together multiple converters.
    /// </summary>
    public class ValueConverterGroup : List<IValueConverter>, IValueConverter
    {
        /// <inheritdoc/>
        /// <inheritdoc/>
        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            object? curValue;

            curValue = value;
            for (int i = 0; i < Count; i++)
            {
                curValue = this[i].Convert(curValue, targetType, parameter, culture);
            }

            return curValue;
        }

        /// <inheritdoc/>
        public object? ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            object? curValue;

            curValue = value;
            for (int i = (Count - 1); i >= 0; i--)
            {
                curValue = this[i].ConvertBack(curValue, targetType, parameter, culture);
            }

            return curValue;
        }
    }
}
