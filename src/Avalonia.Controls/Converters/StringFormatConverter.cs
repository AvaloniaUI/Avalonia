using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
        if (values[0] is string format)
        {
            try
            {
                return string.Format(format, values.Skip(1).ToArray());
            }
            catch
            {
                return AvaloniaProperty.UnsetValue;
            }
        }
        return AvaloniaProperty.UnsetValue;
    }
}
