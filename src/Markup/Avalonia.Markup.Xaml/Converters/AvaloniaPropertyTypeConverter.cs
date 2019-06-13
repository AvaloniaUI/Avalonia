// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Logging;
using Avalonia.Markup.Parsers;
using Avalonia.Markup.Xaml.Parsers;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Styling;
using Avalonia.Utilities;

namespace Avalonia.Markup.Xaml.Converters
{
    public class AvaloniaPropertyTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var registry = AvaloniaPropertyRegistry.Instance;
            var parser = new PropertyParser();
            var (ns, owner, propertyName) = parser.Parse(new CharacterReader(((string)value).AsSpan()));
            var ownerType = TryResolveOwnerByName(context, ns, owner);
            var targetType = context.GetFirstParent<ControlTemplate>()?.TargetType ??
                context.GetFirstParent<Style>()?.Selector?.TargetType ??
                typeof(Control);
            var effectiveOwner = ownerType ?? targetType;
            var property = registry.FindRegistered(effectiveOwner, propertyName);

            if (property == null)
            {
                throw new XamlLoadException($"Could not find property '{effectiveOwner.Name}.{propertyName}'.");
            }

            if (effectiveOwner != targetType &&
                !property.IsAttached &&
                !registry.IsRegistered(targetType, property))
            {
                Logger.Warning(
                    LogArea.Property,
                    this,
                    "Property '{Owner}.{Name}' is not registered on '{Type}'.",
                    effectiveOwner,
                    propertyName,
                    targetType);
            }

            return property;
        }

        private Type TryResolveOwnerByName(ITypeDescriptorContext context, string ns, string owner)
        {
            if (owner != null)
            {
                var result = context.ResolveType(ns, owner);

                if (result == null)
                {
                    var name = string.IsNullOrEmpty(ns) ? owner : $"{ns}:{owner}";
                    throw new XamlLoadException($"Could not find type '{name}'.");
                }

                return result;
            }

            return null;
        }
    }
}
