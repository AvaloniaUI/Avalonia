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
            if (value is int scalarDepth)
            {
                return new Thickness(
                    Left ? Indent * scalarDepth : 0,
                    Top ? Indent * scalarDepth : 0,
                    Right ? Indent * scalarDepth : 0,
                    Bottom ? Indent * scalarDepth : 0);
            }
            else if (value is Thickness thinknessDepth)
            {
                return new Thickness(
                    Left ? Indent * thinknessDepth.Left : 0,
                    Top ? Indent * thinknessDepth.Top : 0,
                    Right ? Indent * thinknessDepth.Right : 0,
                    Bottom ? Indent * thinknessDepth.Bottom : 0);
            }
            return new Thickness(0);
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }
}
