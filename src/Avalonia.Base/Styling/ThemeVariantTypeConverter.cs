using System;
using System.ComponentModel;
using System.Globalization;

namespace Avalonia.Styling;

public class ThemeVariantTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string);
    }

    public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        return value switch
        {
            nameof(ThemeVariant.Light) => ThemeVariant.Light,
            nameof(ThemeVariant.Dark) => ThemeVariant.Dark,
            _ => new ThemeVariant(value)
        };
    }
}
