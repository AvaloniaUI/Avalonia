using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Avalonia.Markup.Xaml.UnitTests.MarkupExtensions
{
    public class TestValueConverter : IValueConverter
    {
        public string Append { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString() + Append;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
