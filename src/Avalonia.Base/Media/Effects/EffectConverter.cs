using System;
using System.ComponentModel;
using System.Globalization;

namespace Avalonia.Media;

public class EffectConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object? value)
    {
        return value is string s ? Effect.Parse(s) : null;
    }
}