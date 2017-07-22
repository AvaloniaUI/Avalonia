// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;

namespace Avalonia.Markup.Xaml.Converters
{
#if !OMNIXAML

    using Portable.Xaml.ComponentModel;
	using System.ComponentModel;

    public class TimeSpanTypeConverter : System.ComponentModel.TimeSpanConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var valueStr = (string)value;
            if (!valueStr.Contains(":"))
            {
                // shorthand seconds format (ie. "0.25")
                var secs = double.Parse(valueStr, CultureInfo.InvariantCulture);
                return TimeSpan.FromSeconds(secs);
            }

            return base.ConvertFrom(context, culture, value);
        }
    }

#else

    using OmniXaml.TypeConversion;
    using Avalonia.Media;

    public class TimeSpanTypeConverter : ITypeConverter
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
            var valueStr = (string)value;
            if (!valueStr.Contains(":"))
            {
                // shorthand seconds format (ie. "0.25")
                var secs = double.Parse(valueStr, CultureInfo.InvariantCulture);
                return TimeSpan.FromSeconds(secs);
            }

            return TimeSpan.Parse(valueStr);
        }

        public object ConvertTo(IValueContext context, CultureInfo culture, object value, Type destinationType)
        {
            throw new NotImplementedException();
        }
    }
#endif
}