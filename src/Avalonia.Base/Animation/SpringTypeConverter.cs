using System;
using System.ComponentModel;
using System.Globalization;

namespace Avalonia.Animation;

/// <summary>
/// Converts string values to <see cref="Spring"/> values.
/// </summary>
internal class SpringTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string);
    }

    public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        return Spring.Parse((string)value, CultureInfo.InvariantCulture);
    }
}
