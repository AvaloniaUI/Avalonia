// -----------------------------------------------------------------------
// <copyright file="BrushConverter.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Markup.Xaml.Converters
{
    using System;
    using System.Globalization;
    using System.Reflection;
    using System.Text;
    using Media;
    using Media.Imaging;
    using OmniXaml.TypeConversion;
    using Platform;

    public class BrushConverter : ITypeConverter
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
            var colorString = (string)value;

            var color = DecodeColor(colorString);

            if (color != null)
            {
                return new SolidColorBrush(color.Value);
            }
            else
            {
                var member = typeof(Brushes).GetTypeInfo().GetDeclaredProperty(colorString);

                if (member != null)
                {
                    return (Brush)member.GetValue(null);
                }
            }

            throw new InvalidOperationException("Invalid color string.");
        }

        private static Color? DecodeColor(string colorString)
        {
            if (colorString[0] == '#')
            {
                var restOfValue = colorString.Remove(0, 1);

                if (restOfValue.Length == 8)
                {
                    var a = Convert.ToByte(restOfValue.Substring(0, 2), 16);
                    var r = Convert.ToByte(restOfValue.Substring(2, 2), 16);
                    var g = Convert.ToByte(restOfValue.Substring(6, 2), 16);
                    var b = Convert.ToByte(restOfValue.Substring(8, 2), 16);
                    return Color.FromArgb(a, r, g, b);
                }

                if (restOfValue.Length == 6)
                {
                    var r = Convert.ToByte(restOfValue.Substring(0, 2), 16);
                    var g = Convert.ToByte(restOfValue.Substring(2, 2), 16);
                    var b = Convert.ToByte(restOfValue.Substring(4, 2), 16);
                    return Color.FromRgb(r, g, b);
                }

                throw new InvalidOperationException("The color code format cannot be parsed");
            }

            return null;
        }

        public object ConvertTo(IXamlTypeConverterContext context, CultureInfo culture, object value, Type destinationType)
        {
            throw new NotImplementedException();
        }
    }
}