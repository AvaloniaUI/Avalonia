// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Markup.Data;
using Avalonia.Styling;

#if SYSTEM_XAML
using System.Xaml;
using System.Windows.Markup;
#else
using Portable.Xaml;
using Portable.Xaml.Markup;
#endif

namespace Avalonia.Markup.Xaml.MarkupExtensions
{
    public class StaticResourceExtension : MarkupExtension
    {
        public StaticResourceExtension()
        {
        }

        public StaticResourceExtension(string resourceKey)
        {
            ResourceKey = resourceKey;
        }

        public string ResourceKey { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var context = (ITypeDescriptorContext)serviceProvider;
            var schemaContext = context.GetService<IXamlSchemaContextProvider>().SchemaContext;
            var ambientProvider = context.GetService<IAmbientProvider>();
            var ambientValues = ambientProvider.GetAllAmbientValues(
                schemaContext.GetXamlType(typeof(Style)),
                schemaContext.GetXamlType(typeof(Styles)),
                schemaContext.GetXamlType(typeof(StyledElement)));

            // Look upwards though the ambient context for IResourceProviders which might be able
            // to give us the resource.
            foreach (IResourceNode node in ambientValues)
            {
                if (node.TryFindResource(ResourceKey, out var value))
                {
                    return value;
                }
            }

            // The resource still hasn't been found, so add a delayed one-time binding.
            var provideTarget = context.GetService<IProvideValueTarget>();

            if (provideTarget.TargetObject is IControl target &&
                provideTarget.TargetProperty is PropertyInfo property)
            {
                DelayedBinding.Add(target, property, GetValue);
                return AvaloniaProperty.UnsetValue;
            }

            throw new KeyNotFoundException($"Static resource '{ResourceKey}' not found.");
        }

        private object GetValue(IStyledElement control)
        {
            return control.FindResource(ResourceKey);
        }
    }
}
