// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;
using OmniXaml.TypeConversion;
using Perspex.Media;

namespace Perspex.Markup.Xaml.Converters
{
    public class TimeSpanTypeConverter : ITypeConverter
    {
        public bool CanConvertFrom(ITypeConverterContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public bool CanConvertTo(ITypeConverterContext context, Type destinationType)
        {
            return false;
        }

        public object ConvertFrom(ITypeConverterContext context, CultureInfo culture, object value)
        {
            var valueStr = (string)value;
            if (!valueStr.Contains(":"))
            {
                // shorthand seconds format (ie. "0.25")
                var secs = double.Parse(valueStr, CultureInfo.InvariantCulture);
                return TimeSpan.FromSeconds(secs);
            }

            return TimeSpan.Parse(valueStr);
        }

        public object ConvertTo(ITypeConverterContext context, CultureInfo culture, object value, Type destinationType)
        {
            throw new NotImplementedException();
        }
    }
}