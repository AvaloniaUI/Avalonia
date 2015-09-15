// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;
using System.Linq;
using OmniXaml.ObjectAssembler;
using OmniXaml.TypeConversion;
using Perspex.Markup.Xaml.Parsers;
using Perspex.Styling;

namespace Perspex.Markup.Xaml.Converters
{
    public class PerspexPropertyConverter : ITypeConverter
    {
        public bool CanConvertFrom(IXamlTypeConverterContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public bool CanConvertTo(IXamlTypeConverterContext context, Type destinationType)
        {
            return false;
        }

        public object ConvertFrom(IXamlTypeConverterContext context, CultureInfo culture, object value)
        {
            var s = (string)value;
            var lastDot = s.LastIndexOf('.');

            if (lastDot == -1)
            {
                throw new NotSupportedException("PerspexProperties must currently be fully qualified.");
            }

            var typeName = s.Substring(0, lastDot);
            var propertyName = s.Substring(lastDot + 1);
            var type = context.TypeRepository.GetByQualifiedName(typeName)?.UnderlyingType;
            var styleType = context.TypeRepository.GetXamlType(typeof(Style));

            // ATTN: SuperJMN
            //var style = ((XamlTypeConverterContext)context).TopDownValueContext.GetLastInstance(styleType);

            if (type == null)
            {
                throw new InvalidOperationException($"Could not find type '{typeName}'.");
            }

            // TODO: Handle attached properties.
            // TODO: Give decent error message for not found property.
            return PerspexObject.GetRegisteredProperties(type).Single(x => x.Name == propertyName);
        }

        public object ConvertTo(IXamlTypeConverterContext context, CultureInfo culture, object value, Type destinationType)
        {
            throw new NotImplementedException();
        }
    }
}