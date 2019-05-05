using System;
using System.ComponentModel;
using System.Globalization;

namespace Avalonia.Markup.Xaml.Converters
{
    public class AvaloniaUriTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var s = value as string;
            if (s == null)
                return null;
            //On Unix Uri tries to interpret paths starting with "/" as file Uris
            var kind = s.StartsWith("/") ? UriKind.Relative : UriKind.Absolute;
            if (!Uri.TryCreate(s, kind, out var res))
                throw new ArgumentException("Unable to parse URI: " + s);
            return res;
        }
    }
}
