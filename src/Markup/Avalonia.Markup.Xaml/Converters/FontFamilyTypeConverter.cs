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

            string familyName;

            Uri baseUri = null;

            var fontWeight = FontWeight.Normal;

            var fontStyle = FontStyle.Normal;

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
                //case 3:
                //    {
                //        baseUri = new Uri(fontFamilyExpression[0], UriKind.RelativeOrAbsolute);
                //        familyName = fontFamilyExpression[1];
                //        fontWeight = (FontWeight)Enum.Parse(typeof(FontWeight), fontFamilyExpression[2]);
                //        break;
                //    }
                //case 4:
                //    {
                //        baseUri = new Uri(fontFamilyExpression[0], UriKind.RelativeOrAbsolute);
                //        familyName = fontFamilyExpression[1];
                //        fontWeight = (FontWeight)Enum.Parse(typeof(FontWeight), fontFamilyExpression[2]);
                //        fontStyle = (FontStyle)Enum.Parse(typeof(FontStyle), fontFamilyExpression[3]);
                //        break;
                //    }
                default:
                    {
                        throw new ArgumentException("Specified family is not supported.");
                    }
            }

            var fontFamily = new FontFamily(familyName, baseUri);

            var cachedFontFamily = AvaloniaLocator.Current.GetService<IFontFamilyCache>().GetOrAddFontFamily(fontFamily);

            cachedFontFamily.GetOrAddFamilyTypeface(baseUri, fontWeight, fontStyle);

            return fontFamily;
        }
    }
}