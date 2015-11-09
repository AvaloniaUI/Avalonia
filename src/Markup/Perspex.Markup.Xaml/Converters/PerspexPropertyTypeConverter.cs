// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;
using System.Linq;
using OmniXaml;
using OmniXaml.TypeConversion;
using Perspex.Styling;

namespace Perspex.Markup.Xaml.Converters
{
    public class PerspexPropertyTypeConverter : ITypeConverter
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

            string typeName;
            string propertyName;
            Type type;

            ParseProperty(s, out typeName, out propertyName);

            if (typeName == null)
            {
                var styleType = context.TypeRepository.GetXamlType(typeof(Style));
                var style = (Style)context.TopDownValueContext.GetLastInstance(styleType);
                type = style.Selector.TargetType;

                if (type == null)
                {
                    throw new XamlParseException(
                        "Could not determine the target type. Please fully qualify the property name.");
                }
            }
            else
            {
                type = context.TypeRepository.GetByQualifiedName(typeName)?.UnderlyingType;

                if (type == null)
                {
                    throw new XamlParseException($"Could not find type '{typeName}'.");
                }
            }

            // First look for non-attached property on the type and then look for an attached property.
            var property = PerspexPropertyRegistry.Instance.FindRegistered(type, s);
            
            if (property == null)
            {
                property = PerspexPropertyRegistry.Instance.GetAttached(type)
                    .FirstOrDefault(x => x.Name == propertyName);
            }

            if (property == null)
            {
                throw new XamlParseException(
                    $"Could not find PerspexProperty '{type.Name}.{propertyName}'.");
            }

            return property;
        }

        public object ConvertTo(IXamlTypeConverterContext context, CultureInfo culture, object value, Type destinationType)
        {
            throw new NotImplementedException();
        }

        private void ParseProperty(string s, out string typeName, out string propertyName)
        {
            var split = s.Split('.');

            if (split.Length == 1)
            {
                typeName = null;
                propertyName = split[0];
            }
            else if (split.Length == 2)
            {
                typeName = split[0];
                propertyName = split[1];
            }
            else
            {
                throw new XamlParseException($"Invalid property name: '{s}'.");
            }
        }
    }
}