using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Markup.Data;
using Avalonia.Markup.Xaml.XamlIl.Runtime;

namespace Avalonia.Markup.Xaml.MarkupExtensions
{
    public class StaticResourceExtension
    {
        public StaticResourceExtension()
        {
        }

        public StaticResourceExtension(object resourceKey)
        {
            ResourceKey = resourceKey;
        }

        public object ResourceKey { get; set; }

        public object ProvideValue(IServiceProvider serviceProvider)
        {
            var stack = serviceProvider.GetService<IAvaloniaXamlIlParentStackProvider>();

            // Look upwards though the ambient context for IResourceHosts and IResourceProviders
            // which might be able to give us the resource.
            foreach (var e in stack.Parents)
            {
                object value;

                if (e is IResourceHost host && host.TryGetResource(ResourceKey, out value))
                {
                    return value;
                }
                else if (e is IResourceProvider provider && provider.TryGetResource(ResourceKey, out value))
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
