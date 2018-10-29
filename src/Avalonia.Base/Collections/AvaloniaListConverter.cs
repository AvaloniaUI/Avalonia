// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Utilities;

namespace Avalonia.Collections
{
    /// <summary>
    /// Creates an <see cref="AvaloniaList{T}"/> from a string representation.
    /// </summary>
    public class AvaloniaListConverter<T> : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var result = new AvaloniaList<T>();

            // TODO: Use StringTokenizer here.
            var values = ((string)value).Split(',');

            foreach (var s in values)
            {
                if (TypeUtilities.TryConvert(typeof(T), s, culture, out var v))
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
}
