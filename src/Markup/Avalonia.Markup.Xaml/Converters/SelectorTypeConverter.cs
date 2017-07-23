// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;
using Avalonia.Markup.Xaml.Parsers;

namespace Avalonia.Markup.Xaml.Converters
{
    using Portable.Xaml.ComponentModel;
	using System.ComponentModel;

    public class SelectorTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var parser = new SelectorParser((t, ns) => context.ResolveType(ns, t));

            return parser.Parse((string)value);
        }
    }
}