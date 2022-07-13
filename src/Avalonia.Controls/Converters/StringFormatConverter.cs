using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data;
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
        try
        {
            return string.Format((string)values[0]!, values.Skip(1).ToArray());
        }
        catch (Exception e)
        {
            return new BindingNotification(e, BindingErrorType.Error);
        }
    }
}
