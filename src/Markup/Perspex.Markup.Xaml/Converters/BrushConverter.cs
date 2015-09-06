// -----------------------------------------------------------------------
// <copyright file="BrushConverter.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Markup.Xaml.Converters
{
    using System;
    using System.Globalization;
    using System.Reflection;
    using System.Text;
    using Media;
    using Media.Imaging;
    using OmniXaml.TypeConversion;
    using Platform;

    public class BrushConverter : ITypeConverter
    {
        public bool CanConvertFrom(IXamlTypeConverterContext context, Type sourceType)
        {
            return true;
        }

        public bool CanConvertTo(IXamlTypeConverterContext context, Type destinationType)
        {
            return true;
        }

        public object ConvertFrom(IXamlTypeConverterContext context, CultureInfo culture, object value)
        {
            return Brush.Parse((string)value);
        }

        public object ConvertTo(IXamlTypeConverterContext context, CultureInfo culture, object value, Type destinationType)
        {
            throw new NotImplementedException();
        }
    }
}