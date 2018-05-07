using System;
using System.ComponentModel;
using System.Globalization;

namespace Avalonia.Markup.Xaml.Converters
{
    public class CornerRadiusTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            return CornerRadius.Parse((string)value);
        }
    }
}