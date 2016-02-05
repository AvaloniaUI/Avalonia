namespace Perspex.Markup.Xaml.Converters
{
    using System;
    using System.Globalization;
    using Media;
    using OmniXaml.TypeConversion;

    public class GeometryTypeConverter : ITypeConverter
    {
        public bool CanConvertFrom(ITypeConverterContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public bool CanConvertTo(ITypeConverterContext context, Type destinationType)
        {
            return false;
        }

        public object ConvertFrom(ITypeConverterContext context, CultureInfo culture, object value)
        {
            return StreamGeometry.Parse((string)value);
        }

        public object ConvertTo(ITypeConverterContext context, CultureInfo culture, object value, Type destinationType)
        {
            throw new NotImplementedException();
        }
    }
}