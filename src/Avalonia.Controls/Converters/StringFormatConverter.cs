using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Avalonia.Controls.Converters;

/// <summary>
/// Calls <see cref="string.Format(string, object[])"/> on the passed in values, where the first element in the list
/// is the string, and everything after it is passed into the object array in order.
/// </summary>
public class StringFormatConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values != null &&
            values.Count == 5 &&
            values[0] is string format &&
            values[1] is double &&
            values[2] is double &&
            values[3] is double &&
            values[4] is double)

        {

            try
            {
                return string.Format(format, values[1], values[2], values[3], values[4]);
            }
            catch
            {
                return AvaloniaProperty.UnsetValue;
            }
        }
        return AvaloniaProperty.UnsetValue;
    }
}
