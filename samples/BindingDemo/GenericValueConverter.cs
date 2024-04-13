using System;
using System.Globalization;
using Avalonia.Data.Converters;

#nullable enable

namespace BindingDemo;

public class GenericValueConverter<T> : IValueConverter
{
    public GenericValueConverter()
    {
        
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is T)
        {
            return $"{typeof(T).Name}: {value}";
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
