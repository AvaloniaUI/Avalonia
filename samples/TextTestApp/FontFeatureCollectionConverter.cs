using System;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Media;

namespace TextTestApp
{
    public class FontFeatureCollectionConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            return FontFeatureCollection.Parse((string)value);
        }
    }
}
