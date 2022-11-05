using System;
using System.ComponentModel;
using System.Globalization;

namespace Avalonia.Controls;

/// <summary>
/// Creates a <see cref="GridLength"/> from a string representation.
/// </summary>
public class GridLengthConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object? value)
    {
        return value is string s ? GridLength.Parse(s) : null;
    }
}
