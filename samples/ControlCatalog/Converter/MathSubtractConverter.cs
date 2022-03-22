using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ControlCatalog.Converter;

public class MathSubtractConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (double)value - (double)parameter;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
