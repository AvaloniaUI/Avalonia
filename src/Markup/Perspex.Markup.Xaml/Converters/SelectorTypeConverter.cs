// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;
using Perspex.Markup.Xaml.Parsers;
using Portable.Xaml;
using Portable.Xaml.ComponentModel;
using Portable.Xaml.Markup;

namespace Perspex.Markup.Xaml.Converters
{
    public class SelectorTypeConverter : TypeConverter
    {
        public SelectorTypeConverter()
        {
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return false;
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var resolver = (IXamlTypeResolver)context.GetService(typeof(IXamlTypeResolver));

            if (resolver != null)
            {
                var parser = new SelectorParser((t, ns) => ResolveType(resolver, t, ns));
                return parser.Parse((string)value);
            }
            else
            {
                return null;
            }
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            throw new NotImplementedException();
        }

        private Type ResolveType(IXamlTypeResolver resolver, string type, string ns)
        {
            var qualified = string.IsNullOrWhiteSpace(ns) ? type : ns + ':' + type;
            var result = resolver.Resolve(qualified);
            
            if (result != null)
            {
                return result;
            }
            else
            {
                throw new XamlException($"Could not resolve type '{qualified}'.");
            }
        }
    }
}