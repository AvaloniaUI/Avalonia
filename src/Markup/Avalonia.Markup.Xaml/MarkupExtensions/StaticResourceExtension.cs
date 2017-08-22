// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.ComponentModel;
using Avalonia.Controls;
using Portable.Xaml;
using Portable.Xaml.ComponentModel;
using Portable.Xaml.Markup;

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
            var schemaContext = context.GetService<IXamlSchemaContextProvider>()?.SchemaContext;
            var ambientProvider = context.GetService<IAmbientProvider>();
            var resourceProviderType = schemaContext.GetXamlType(typeof(IResourceProvider));
            var resourceProviders = ambientProvider.GetAllAmbientValues(resourceProviderType);

            foreach (IResourceProvider resourceProvider in resourceProviders)
            {
                if (resourceProvider.TryGetResource(ResourceKey, out var value))
                {
                    return value;
                }
            }

            return AvaloniaProperty.UnsetValue;
        }
    }
}
