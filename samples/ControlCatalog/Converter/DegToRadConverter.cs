using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Avalonia.Data.Converters;

namespace ControlCatalog.Converter
{
    public class DegToRadConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double rad)
            {
                return rad * 180.0d / Math.PI;
            }
            return 0.0d;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double deg)
            {
                return deg / 180.0d * Math.PI;
            }
            return 0.0d;
        }
    }
}
