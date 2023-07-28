using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ControlCatalog.Models;

internal class GDPdLengthConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d)
        {
            return new Avalonia.Controls.DataGridLength(d,Avalonia.Controls.DataGridLengthUnitType.Pixel,d,d);
        }
        else if (value is decimal d2)
        {
            var dv =System.Convert.ToDouble(d2);
            return new Avalonia.Controls.DataGridLength(dv, Avalonia.Controls.DataGridLengthUnitType.Pixel, dv, dv);
        }
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Avalonia.Controls.DataGridLength width)
        {
            return System.Convert.ToDecimal(width.DisplayValue);
        }
        return value;
    }
}
