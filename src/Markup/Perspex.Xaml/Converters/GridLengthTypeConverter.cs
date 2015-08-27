namespace Perspex.Xaml.Converters
{
    using System;
    using System.Globalization;
    using Controls;
    using OmniXaml.TypeConversion;

    public class GridLengthTypeConverter : ITypeConverter
    {
        public object ConvertFrom(IXamlTypeConverterContext context, CultureInfo culture, object value)
        {
            var str = value as string;

            if (str != null)
            {
                if (string.Equals(str, "Auto"))
                {
                    return new GridLength(0, GridUnitType.Auto);
                }
            }

            return new GridLength(1, GridUnitType.Star);
        }

        public object ConvertTo(IXamlTypeConverterContext context, CultureInfo culture, object value, Type destinationType)
        {
            if ((string) value == "Auto")
            {
                return new GridLength(0, GridUnitType.Auto);
            }

            return new GridLength(1, GridUnitType.Star);
        }

        public bool CanConvertTo(IXamlTypeConverterContext context, Type destinationType)
        {
            return true;
        }

        public bool CanConvertFrom(IXamlTypeConverterContext context, Type sourceType)
        {
            return true;
        }
    }
}