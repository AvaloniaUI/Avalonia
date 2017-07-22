// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Avalonia.Markup.Xaml.Converters
{
#if !OMNIXAML

    using Portable.Xaml.ComponentModel;
	using System.ComponentModel;

    public class PointsListTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string strValue = (string)value;
            string[] pointStrs = strValue.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new List<Point>(pointStrs.Length);
            foreach (var pointStr in pointStrs)
            {
                result.Add(Point.Parse(pointStr, culture));
            }

            return result;
        }
    }

#else

    using OmniXaml.TypeConversion;

    public class PointsListTypeConverter : ITypeConverter
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
            string strValue = (string)value;
            string[] pointStrs = strValue.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new List<Point>(pointStrs.Length);
            foreach (var pointStr in pointStrs)
            {
                result.Add(Point.Parse(pointStr, culture));
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
