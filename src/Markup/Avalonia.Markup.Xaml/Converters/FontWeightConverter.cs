// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Markup.Xaml.Converters
{
#if OMNIXAML
    using Avalonia.Media;
    using OmniXaml.TypeConversion;
    using System;
    using System.Globalization;

    public class FontWeightConverter : ITypeConverter
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
            FontWeight result;
            
            if (Enum.TryParse(value as string, out result))
            {
                return result;
            }
            else
            {
                throw new ArgumentException("unable to convert parameter to FontWeight");
            }
        }

        public object ConvertTo(IValueContext context, CultureInfo culture, object value, Type destinationType)
        {
            throw new NotImplementedException();
        }
    }
#endif
}
