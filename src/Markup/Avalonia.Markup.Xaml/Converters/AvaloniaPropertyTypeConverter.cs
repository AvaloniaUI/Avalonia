// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Styling;
using Portable.Xaml;
using Portable.Xaml.ComponentModel;
using Portable.Xaml.Markup;

namespace Avalonia.Markup.Xaml.Converters
{
    public class AvaloniaPropertyTypeConverter : TypeConverter
    {
        private static readonly Regex regex = new Regex(@"^\(?(\w*)\.(\w*)\)?|(.*)$");

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var (owner, propertyName) = ParseProperty((string)value);
            var ownerType = TryResolveOwnerByName(context, owner) ??
                context.GetFirstAmbientValue<ControlTemplate>()?.TargetType ??
                context.GetFirstAmbientValue<Style>()?.Selector?.TargetType;

            if (ownerType == null)
            {
                throw new XamlLoadException(
                    $"Could not determine the owner type for property '{propertyName}'. " +
                    "Please fully qualify the property name or specify a target type on " +
                    "the containing template.");
            }

            var property = AvaloniaPropertyRegistry.Instance.FindRegistered(ownerType, propertyName);

            if (property == null)
            {
                throw new XamlLoadException($"Could not find AvaloniaProperty '{ownerType.Name}.{propertyName}'.");
            }

            return property;
        }

        private Type TryResolveOwnerByName(ITypeDescriptorContext context, string owner)
        {
            if (owner != null)
            {
                var resolver = context.GetService<IXamlTypeResolver>();
                var result = resolver.Resolve(owner);

                if (result == null)
                {
                    throw new XamlLoadException($"Could not find type '{owner}'.");
                }

                return result;
            }

            return null;
        }

        private (string owner, string property) ParseProperty(string s)
        {
            var result = regex.Match(s);

            if (result.Groups[1].Success)
            {
                return (result.Groups[1].Value, result.Groups[2].Value);
            }
            else
            {
                return (null, result.Groups[3].Value);
            }
        }
    }
}