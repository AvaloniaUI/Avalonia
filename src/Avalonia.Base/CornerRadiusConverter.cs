using System;
using System.ComponentModel;
using System.Globalization;

namespace Avalonia;

/// <summary>
/// Creates a <see cref="CornerRadius"/> from a string representation.
/// </summary>
public class CornerRadiusConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object? value)
    {
        return value is string s ? CornerRadius.Parse(s) : null;
    }
}
