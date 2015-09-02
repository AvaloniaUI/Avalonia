// -----------------------------------------------------------------------
// <copyright file="ThicknessConverter.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Markup.Xaml.Converters
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using OmniXaml.TypeConversion;

    public class ThicknessConverter : ITypeConverter
    {
        public object ConvertFrom(IXamlTypeConverterContext context, CultureInfo culture, object value)
        {
            var s = value as string;
            if (s != null)
            {
                return ConvertFromString(s);
            }

            return null;
        }

        private static Thickness ConvertFromString(string s)
        {
            var parts = s.Split(',', ' ');

            switch (parts.Length)
            {
                case 1:
                    var uniform = double.Parse(parts[0]);
                    return new Thickness(uniform);
                case 2:
                    var horizontal = double.Parse(parts[0]);
                    var vertical = double.Parse(parts[1]);
                    return new Thickness(horizontal, vertical);
                case 4:
                    var left = double.Parse(parts[0]);
                    var top = double.Parse(parts[1]);
                    var right = double.Parse(parts[2]);
                    var bottom = double.Parse(parts[3]);
                    return new Thickness(left, top, right, bottom);
            }

            throw new InvalidOperationException("Invalid Thickness.");
        }

        public object ConvertTo(IXamlTypeConverterContext context, CultureInfo culture, object value, Type destinationType)
        {
            throw new System.NotImplementedException();
        }

        public bool CanConvertTo(IXamlTypeConverterContext context, Type destinationType)
        {
            throw new NotImplementedException();
        }

        public bool CanConvertFrom(IXamlTypeConverterContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }

            return false;
        }
    }
}