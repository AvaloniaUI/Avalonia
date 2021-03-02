using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Avalonia.Controls.Converters
{
    public class BottomCornerRadiusFilterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var exp = (ExpandDirection)value;
            if (exp == ExpandDirection.Left)
                return new CornerRadius(3, 0, 0, 3);
            if (exp == ExpandDirection.Up)
                return new CornerRadius(3, 3, 0, 0);
            else if (exp == ExpandDirection.Right)
                return new CornerRadius(0, 3, 3, 0);
            else
                return new CornerRadius(0, 0, 3, 3);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }
}