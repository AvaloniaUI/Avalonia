// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Controls;
using Avalonia.Data;
using Portable.Xaml.Markup;

namespace Avalonia.Markup.Xaml.MarkupExtensions
{
    public class DynamicResourceExtension : MarkupExtension, IBinding
    {
        public DynamicResourceExtension()
        {
        }

        public DynamicResourceExtension(string resourceKey)
        {
            ResourceKey = resourceKey;
        }

        public string ResourceKey { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider) => this;

        InstancedBinding IBinding.Initiate(
            IAvaloniaObject target,
            AvaloniaProperty targetProperty,
            object anchor,
            bool enableDataValidation)
        {
            if (target is IControl control)
            {
                var resource = control.FindResource(ResourceKey);
                return new InstancedBinding(resource);
            }

            return null;
        }
    }
}
