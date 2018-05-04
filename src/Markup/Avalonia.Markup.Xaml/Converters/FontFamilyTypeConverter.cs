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

            if (!s.StartsWith("resm:")) return new FontFamily(s);

            var fontFamilyExpression = s.Split('#');

            string familyName;

            Uri baseUri = null;

            switch (fontFamilyExpression.Length)
            {
                case 1:
                    {
                        familyName = fontFamilyExpression[0];
                        break;
                    }
                case 2:
                    {
                        baseUri = new Uri(fontFamilyExpression[0], UriKind.RelativeOrAbsolute);
                        familyName = fontFamilyExpression[1];
                        break;
                    }
                default:
                    {
                        throw new ArgumentException("Specified family is not supported.");
                    }
            }

            return new FontFamily(familyName, baseUri);
        }
    }
}