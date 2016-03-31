// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;
using System.Linq;
using Perspex.Styling;
using Portable.Xaml;
using Portable.Xaml.ComponentModel;

namespace Perspex.Markup.Xaml.Converters
{
    public class PerspexPropertyTypeConverter : TypeConverter
    {
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
            var s = (string)value;

            string typeName;
            string propertyName;
            Type type;

            ParseProperty(s, out typeName, out propertyName);

            if (typeName == null)
            {
                ////var styleType = context.TypeRepository.GetByType(typeof(Style));
                ////var style = (Style)context.TopDownValueContext.GetLastInstance(styleType);
                ////type = style.Selector?.TargetType;

                ////if (type == null)
                {
                    throw new XamlException(
                        "Could not determine the target type. Please fully qualify the property name.");
                }
            }
            else
            {
                ////type = context.TypeRepository.GetByQualifiedName(typeName)?.UnderlyingType;

                ////if (type == null)
                {
                    throw new XamlException($"Could not find type '{typeName}'.");
                }
            }

            // First look for non-attached property on the type and then look for an attached property.
            var property = PerspexPropertyRegistry.Instance.FindRegistered(type, s) ??
                           PerspexPropertyRegistry.Instance.GetAttached(type)
                           .FirstOrDefault(x => x.Name == propertyName);

            if (property == null)
            {
                throw new XamlException(
                    $"Could not find PerspexProperty '{type.Name}.{propertyName}'.");
            }

            return property;
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
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
                throw new XamlException($"Invalid property name: '{s}'.");
            }
        }
    }
}