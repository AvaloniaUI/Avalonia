// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Styling;
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
                if (resourceProvider is IControl control && control.StylingParent != null)
                {
                    // If we've got to a control that has a StylingParent then it's probably
                    // a top level control and its StylingParent is pointing to the global
                    // styles. If this is case just do a FindResource on it.
                    return control.FindResource(ResourceKey);
                }
                else if (resourceProvider.TryGetResource(ResourceKey, out var value))
                {
                    return value;
                }
            }

            return AvaloniaProperty.UnsetValue;
        }
    }
}
