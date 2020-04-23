// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Avalonia.Markup.Xaml.Converters
{
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
            string[] pointStrs = strValue.Split(new[] { ' ', '\t', '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new List<Point>(pointStrs.Length / 2);
            for (int i = 0; i < pointStrs.Length; i += 2)
            {
                result.Add(Point.Parse($"{pointStrs[i]} {pointStrs[i + 1]}"));
            }

            return result;
        }
    }
}
