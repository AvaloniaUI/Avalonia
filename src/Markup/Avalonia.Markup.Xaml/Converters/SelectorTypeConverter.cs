// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Markup.Parsers;

#if SYSTEM_XAML
using System.Windows.Markup;
#else
using Portable.Xaml.Markup;
#endif

namespace Avalonia.Markup.Xaml.Converters
{
    public class SelectorTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var resolver = context.GetService<IXamlTypeResolver>();

            Type Resolve(string ns, string name)
            {
                return string.IsNullOrWhiteSpace(ns) ?
                    resolver.Resolve(name) :
                    resolver.Resolve(ns + ':' + name);
            }

            var parser = new SelectorParser(Resolve);
            return parser.Parse((string)value);
        }
    }
}
