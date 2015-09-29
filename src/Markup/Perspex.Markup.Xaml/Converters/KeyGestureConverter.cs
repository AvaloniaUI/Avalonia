using System;
using System.Globalization;
using OmniXaml.TypeConversion;
using Perspex.Input;

namespace Perspex.Markup.Xaml.Converters
{
    class KeyGestureConverter : ITypeConverter
    {
        public bool CanConvertFrom(IXamlTypeConverterContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public bool CanConvertTo(IXamlTypeConverterContext context, Type destinationType)
        {
            return false;
        }

        public object ConvertFrom(IXamlTypeConverterContext context, CultureInfo culture, object value)
        {
            return KeyGesture.Parse((string)value);
        }

        public object ConvertTo(IXamlTypeConverterContext context, CultureInfo culture, object value, Type destinationType)
        {
            throw new NotImplementedException();
        }
    }
}
