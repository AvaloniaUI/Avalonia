using System;
using System.Globalization;

namespace Avalonia.Data.Converters
{
    public class StringFormatValueConverter : IValueConverter
    {
        public StringFormatValueConverter(string format, IValueConverter inner)
        {
            Contract.Requires<ArgumentNullException>(format != null);

            Format = format;
            Inner = inner;
        }

        public IValueConverter Inner { get; }
        public string Format { get; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            value = Inner?.Convert(value, targetType, parameter, culture) ?? value;
            return string.Format(Format, value, culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("Two way bindings are not supported with a string format");
        }
    }
}
