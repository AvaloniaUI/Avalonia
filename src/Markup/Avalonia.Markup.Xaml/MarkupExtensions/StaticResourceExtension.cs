// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Data;
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
            var schemaContext = context.GetService<IXamlSchemaContextProvider>().SchemaContext;
            var ambientProvider = context.GetService<IAmbientProvider>();
            var resourceProviderType = schemaContext.GetXamlType(typeof(IResourceNode));
            var ambientValues = ambientProvider.GetAllAmbientValues(resourceProviderType);

            // Look upwards though the ambient context for IResourceProviders which might be able
            // to give us the resource.
            //
            // TODO: If we're in a template then only the ambient values since the root of the
            // template wil be included here. We need some way to get hold of the parent ambient
            // context and search that. See the test:
            //
            //   StaticResource_Can_Be_Assigned_To_Property_In_ControlTemplate_In_Styles_File
            //
            foreach (var ambientValue in ambientValues)
            {
                // We override XamlType.CanAssignTo in BindingXamlType so the results we get back
                // from GetAllAmbientValues aren't necessarily of the correct type.
                if (ambientValue is IResourceNode resourceProvider)
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

        private object GetValue(IControl control)
        {
            return control.FindResource(ResourceKey);
        }
    }
}
