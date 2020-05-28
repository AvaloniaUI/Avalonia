using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Avalonia.Media.Immutable;

namespace Avalonia.Media
{
    internal class ColorToSolidColorBrushConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            return Color.Parse((string)value);
        }
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return typeof(IBrush).IsAssignableFrom(destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (typeof(IBrush).IsAssignableFrom(destinationType))
            {
                return new ImmutableSolidColorBrush((Color)value);
            }

            return null;
        }
    }
}