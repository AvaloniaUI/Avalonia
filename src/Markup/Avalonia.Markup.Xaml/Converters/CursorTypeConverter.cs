// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;
using Avalonia.Input;

namespace Avalonia.Markup.Xaml.Converters
{
#if !OMNIXAML

    using Portable.Xaml.ComponentModel;
	using System.ComponentModel;

    public class CursorTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var cursor = (StandardCursorType)Enum.Parse(typeof(StandardCursorType), ((string)value).Trim(), true);
            return new Cursor(cursor);
        }
    }

#else

    using OmniXaml.TypeConversion;

    public class CursorTypeConverter : ITypeConverter
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
            var cursor = (StandardCursorType)Enum.Parse(typeof (StandardCursorType), ((string) value).Trim(), true);
            return new Cursor(cursor);
        }

        public object ConvertTo(IValueContext context, CultureInfo culture, object value, Type destinationType)
        {
            throw new NotImplementedException();
        }
    }

#endif
}