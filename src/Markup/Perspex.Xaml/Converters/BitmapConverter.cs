namespace Perspex.Xaml.Converters
{
    using System;
    using System.Globalization;
    using Media.Imaging;
    using OmniXaml.TypeConversion;

    public class BitmapConverter : ITypeConverter
    {
        public bool CanConvertFrom(IXamlTypeConverterContext context, Type sourceType)
        {
            return true;
        }

        public bool CanConvertTo(IXamlTypeConverterContext context, Type destinationType)
        {
            return true;
        }

        public object ConvertFrom(IXamlTypeConverterContext context, CultureInfo culture, object value)
        {
            var path = (string)value;
            return new Bitmap(path);
        }

        public object ConvertTo(IXamlTypeConverterContext context, CultureInfo culture, object value, Type destinationType)
        {
            return new Bitmap(10, 10);
        }
    }
}