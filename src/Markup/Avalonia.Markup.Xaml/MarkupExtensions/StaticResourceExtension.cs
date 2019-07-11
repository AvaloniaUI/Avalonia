// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Markup.Data;

namespace Avalonia.Markup.Xaml.MarkupExtensions
{
    public class StaticResourceExtension
    {
        public StaticResourceExtension()
        {
        }

        public StaticResourceExtension(string resourceKey)
        {
            ResourceKey = resourceKey;
        }

        public string ResourceKey { get; set; }

        public object ProvideValue(IServiceProvider serviceProvider)
        {
            // Look upwards though the ambient context for IResourceProviders which might be able
            // to give us the resource.
            foreach (var resourceProvider in serviceProvider.GetParents<IResourceNode>())
            {
                if (resourceProvider.TryGetResource(ResourceKey, out var value))
                {
                    return value;
                }

            }

            // The resource still hasn't been found, so add a delayed one-time binding.
            var provideTarget = serviceProvider.GetService<IProvideValueTarget>();

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
