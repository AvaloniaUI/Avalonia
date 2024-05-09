using System;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Metadata;

namespace Avalonia.Data;

[PrivateApi]
public class CultureInfoIetfLanguageTagConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) => sourceType == typeof(string);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string cultureName)
        {
            return CultureInfo.GetCultureInfoByIetfLanguageTag(cultureName);
        }

        throw GetConvertFromException(value);
    }
}
