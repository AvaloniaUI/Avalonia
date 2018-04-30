using System;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Media;

namespace Avalonia.Markup.Xaml.Converters
{
    public class FontFamilyTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var s = (string)value;

            if (string.IsNullOrEmpty(s)) throw new ArgumentException("Specified family is not supported.");

            var fontFamilyExpression = s.Split('#');

            switch (fontFamilyExpression.Length)
            {
                case 1:
                    {
                        return new FontFamily(fontFamilyExpression[0]);
                    }
                case 2:
                    {
                        return new FontFamily(fontFamilyExpression[1], new Uri(fontFamilyExpression[0], UriKind.RelativeOrAbsolute));
                    }
                default:
                    {
                        throw new ArgumentException("Specified family is not supported.");
                    }
            }
        }
    }
}