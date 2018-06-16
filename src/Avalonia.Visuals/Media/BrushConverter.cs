// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Globalization;

namespace Avalonia.Media
{
    /// <summary>
    /// Creates an <see cref="IBrush"/> from a string representation.
    /// </summary>
    public class BrushConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            return Brush.Parse((string)value);
        }
    }
}