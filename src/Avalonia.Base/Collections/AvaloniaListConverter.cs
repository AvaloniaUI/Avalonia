using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia.Utilities;

namespace Avalonia.Collections
{
    /// <summary>
    /// Creates an <see cref="AvaloniaList{T}"/> from a string representation.
    /// </summary>
    [RequiresUnreferencedCode(TrimmingMessages.TypeConversionRequiresUnreferencedCodeMessage)]
    public class AvaloniaListConverter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T> : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object? value)
        {
            if (value is not string stringValue)
                return null;

            var result = new AvaloniaList<T>();

            // TODO: Use StringTokenizer here.
            var values = stringValue.Split(',');

            foreach (var s in values)
            {
                if (TypeUtilities.TryConvert(typeof(T), s, culture, out var v))
                {
                    result.Add((T)v!);
                }
                else
                {
                    throw new InvalidCastException($"Could not convert '{s}' to {typeof(T)}.");
                }
            }

            return result;
        }
    }
}
