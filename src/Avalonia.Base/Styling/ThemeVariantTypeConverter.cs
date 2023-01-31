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
            nameof(ThemeVariant.Default) => ThemeVariant.Default,
            nameof(ThemeVariant.Light) => ThemeVariant.Light,
            nameof(ThemeVariant.Dark) => ThemeVariant.Dark,
            _ => throw new NotSupportedException("ThemeVariant type converter supports only build in variants. For custom variants please use x:Static markup extension.")
        };
    }
}
