// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;
using OmniXaml.TypeConversion;

namespace Avalonia.Markup.Xaml.Converters
{
#if OMNIXAML
    public class DateTimeTypeConverter : ITypeConverter
    {
        public object ConvertFrom(IValueContext context, CultureInfo culture, object value)
        {
            if (culture == null)
            {
                throw new ArgumentNullException("culture");
            }

            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            DateTimeFormatInfo dateTimeFormatInfo = (DateTimeFormatInfo)culture.GetFormat(typeof(DateTimeFormatInfo));
            DateTime d = DateTime.ParseExact(value.ToString(), dateTimeFormatInfo.ShortDatePattern, culture);
            return d;
        }

        public object ConvertTo(IValueContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException("destinationType");
            }

            if (culture == null)
            {
                throw new ArgumentNullException("culture");
            }

            DateTime? d = value as DateTime?;

            if (!d.HasValue || destinationType != typeof(string))
            {
                throw new NotSupportedException();
            }
            DateTimeFormatInfo dateTimeFormatInfo = (DateTimeFormatInfo)culture.GetFormat(typeof(DateTimeFormatInfo));
            return d.Value.ToString(dateTimeFormatInfo.ShortDatePattern, culture);
        }

        public bool CanConvertTo(IValueContext context, Type destinationType)
        {
            return destinationType == typeof(string);
        }

        public bool CanConvertFrom(IValueContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }
    }
#endif
}
