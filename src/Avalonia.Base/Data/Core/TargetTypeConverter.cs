﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia.Data.Converters;
using System.Windows.Input;
using Avalonia.Utilities;
using static Avalonia.Utilities.TypeUtilities;
using System.ComponentModel;

namespace Avalonia.Data.Core;

internal abstract class TargetTypeConverter
{
    private static TargetTypeConverter? s_default;
    private static TargetTypeConverter? s_reflection;

    public static TargetTypeConverter GetDefaultConverter() => s_default ??= new DefaultConverter();

    [RequiresUnreferencedCode(TrimmingMessages.TypeConversionRequiresUnreferencedCodeMessage)]
    public static TargetTypeConverter GetReflectionConverter() => s_reflection ??= new ReflectionConverter();

    public abstract bool TryConvert(object? value, Type type, CultureInfo culture, out object? result);

    private class DefaultConverter : TargetTypeConverter
    {
        public override bool TryConvert(object? value, Type type, CultureInfo culture, out object? result)
        {
            if (value?.GetType() == type)
            {
                result = value;
                return true;
            }

            var t = Nullable.GetUnderlyingType(type) ?? type;

            if (value is null)
            {
                result = null;
                return !t.IsValueType || t != type;
            }

            if (value == AvaloniaProperty.UnsetValue)
            {
                // Here the behavior is different from the ReflectionConverter: there isn't any way
                // to create the default value for a type without using reflection, so we have to report
                // that we can't convert the value.
                result = null;
                return false;
            }

            if (t.IsAssignableFrom(value.GetType()))
            {
                result = value;
                return true;
            }

            if (t == typeof(string))
            {
                result = value.ToString();
                return true;
            }

            if (t.IsEnum && t.GetEnumUnderlyingType() == value.GetType())
            {
                result = Enum.ToObject(t, value);
                return true;
            }

#pragma warning disable IL2026
#pragma warning disable IL2067
#pragma warning disable IL2072
            // TODO: TypeConverters are not trimming friendly in some edge cases, we probably need
            // to make compiled bindings emit conversion code at compile-time.
            var toTypeConverter = TypeDescriptor.GetConverter(t);
            var from = value.GetType();

            if (toTypeConverter.CanConvertFrom(from))
            {
                try
                {
                    result = toTypeConverter.ConvertFrom(null, culture, value);
                    return true;
                }
                catch
                {
                    result = null;
                    return false;
                }
            }

            var fromTypeConverter = TypeDescriptor.GetConverter(from);

            if (fromTypeConverter.CanConvertTo(t))
            {
                try
                {
                    result = fromTypeConverter.ConvertTo(null, culture, value, t);
                    return true;
                }
                catch
                {
                    result = null;
                    return false;
                }
            }

            // TODO: This requires reflection: we probably need to make compiled bindings emit
            // conversion code at compile-time.
            if (FindTypeConversionOperatorMethod(
                value.GetType(),
                t,
                OperatorType.Implicit | OperatorType.Explicit) is { } cast)
            {
                try
                {
                    result = cast.Invoke(null, new[] { value });
                    return true;
                }
                catch
                {
                    result = null;
                    return false;
                }
            }
#pragma warning restore IL2067
#pragma warning restore IL2026

            if (value is IConvertible convertible)
            {
                try
                {
                    result = convertible.ToType(t, culture);
                    return true;
                }
                catch
                {
                    result = null;
                    return false;
                }
            }

            result = null;
            return false;
        }
    }

    [RequiresUnreferencedCode(TrimmingMessages.TypeConversionRequiresUnreferencedCodeMessage)]
    private class ReflectionConverter : TargetTypeConverter
    {
        public override bool TryConvert(object? value, Type type, CultureInfo culture, out object? result)
        {
            if (value?.GetType() == type)
            {
                result = value;
                return true;
            }
            else if (value == AvaloniaProperty.UnsetValue)
            {
                result = Activator.CreateInstance(type);
                return true;
            }
            else if (typeof(ICommand).IsAssignableFrom(type) && 
                value is Delegate d &&
                !d.Method.IsPrivate &&
                d.Method.GetParameters().Length <= 1)
            {
                result = new MethodToCommandConverter(d);
                return true;
            }
            else
            {
                return TypeUtilities.TryConvert(type, value, culture, out result);
            }
        }
    }
}
