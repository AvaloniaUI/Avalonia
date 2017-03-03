// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;
using Avalonia.Collections;
using Avalonia.Utilities;

namespace Avalonia.Markup.Xaml.Converters
{
#if !OMNIXAML

    using Portable.Xaml.ComponentModel;

    public class AvaloniaListTypeConverter<T> : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var result = new AvaloniaList<T>();
            var values = ((string)value).Split(',');

            foreach (var s in values)
            {
                object v;

                if (TypeUtilities.TryConvert(typeof(T), s, culture, out v))
                {
                    result.Add((T)v);
                }
                else
                {
                    throw new InvalidCastException($"Could not convert '{s}' to {typeof(T)}.");
                }
            }

            return result;
        }
    }

#else
    using OmniXaml.TypeConversion;

    public class AvaloniaListTypeConverter<T> : ITypeConverter
    {
        public bool CanConvertFrom(IValueContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public bool CanConvertTo(IValueContext context, Type destinationType)
        {
            return false;
        }

        public object ConvertFrom(IValueContext context, CultureInfo culture, object value)
        {
            var result = new AvaloniaList<T>();
            var values = ((string)value).Split(',');

            foreach (var s in values)
            {
                object v;

                if (TypeUtilities.TryConvert(typeof(T), s, culture, out v))
                {
                    result.Add((T)v);
                }
                else
                {
                    throw new InvalidCastException($"Could not convert '{s}' to {typeof(T)}.");
                }
            }

            return result;
        }

        public object ConvertTo(IValueContext context, CultureInfo culture, object value, Type destinationType)
        {
            throw new NotImplementedException();
        }
    }
#endif
}