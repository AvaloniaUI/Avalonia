// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Styling;
using Portable.Xaml;
using Portable.Xaml.ComponentModel;
using System.ComponentModel;
using Portable.Xaml.Markup;
using System;
using System.Globalization;

namespace Avalonia.Markup.Xaml.Converters
{
    public class SetterValueTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            object setter = context.GetService<IProvideValueTarget>().TargetObject;
            var schemaContext = context.GetService<IXamlSchemaContextProvider>().SchemaContext;

            return ConvertSetterValue(context, schemaContext, culture, (setter as Setter), value);
        }

        [Obsolete("TODO: try assosiate Setter.Value property with SetterValueTypeConverter, so far coouldn't make it :(")]
        internal static object ConvertSetterValue(ITypeDescriptorContext dcontext, XamlSchemaContext context, CultureInfo info, Setter setter, object value)
        {
            Type targetType = setter?.Property?.PropertyType;

            if (targetType == null)
            {
                return value;
            }

            var ttConv = context.GetXamlType(targetType)?.TypeConverter?.ConverterInstance;

            if (ttConv != null)
            {
                value = ttConv.ConvertFromString(dcontext, info, value as string);
            }

            return value;
        }
    }
}