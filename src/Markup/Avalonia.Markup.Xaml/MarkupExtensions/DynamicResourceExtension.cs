// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Data;
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
    public class DynamicResourceExtension : MarkupExtension, IBinding
    {
        private IResourceNode _anchor;

        public DynamicResourceExtension()
        {
        }

        public DynamicResourceExtension(string resourceKey)
        {
            ResourceKey = resourceKey;
        }

        public string ResourceKey { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var context = (ITypeDescriptorContext)serviceProvider;
            var provideTarget = context.GetService<IProvideValueTarget>();

            if (!(provideTarget.TargetObject is IResourceNode))
            {
                _anchor = GetAnchor(context);
            }

            return this;
        }

        InstancedBinding IBinding.Initiate(
            IAvaloniaObject target,
            AvaloniaProperty targetProperty,
            object anchor,
            bool enableDataValidation)
        {
            var control = target as IResourceNode ?? _anchor;

            if (control != null)
            {
                return InstancedBinding.OneWay(control.GetResourceObservable(ResourceKey));
            }

            return null;
        }

        private IResourceNode GetAnchor(ITypeDescriptorContext context)
        {
            var schemaContext = context.GetService<IXamlSchemaContextProvider>().SchemaContext;
            var ambientProvider = context.GetService<IAmbientProvider>();

            return (IResourceNode)ambientProvider.GetFirstAmbientValue(
                schemaContext.GetXamlType(typeof(StyledElement)),
                schemaContext.GetXamlType(typeof(Style)));
        }
    }
}
