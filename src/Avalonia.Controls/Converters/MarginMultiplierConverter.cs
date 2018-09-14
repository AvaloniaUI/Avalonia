using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Avalonia.Controls.Converters
{
    public class MarginMultiplierConverter : IValueConverter
    {
        public double Indent { get; set; }

        public bool Left { get; set; } = false;

        public bool Top { get; set; } = false;

        public bool Right { get; set; } = false;

        public bool Bottom { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is int depth))
                return new Thickness(0);

            return new Thickness(Left ? Indent * depth : 0, Top ? Indent * depth : 0, Right ? Indent * depth : 0, Bottom ? Indent * depth : 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }
}
