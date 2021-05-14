using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Avalonia.Diagnostics.Converters
{
    internal class BoolToOpacityConverter : IValueConverter
    {
        public double Opacity { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? 1d : Opacity;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
