using System;
using System.Collections.Generic;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Markup.Data;
using Avalonia.Markup.Xaml.Converters;
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
            var provideTarget = serviceProvider.GetService<IProvideValueTarget>();

            var targetType = provideTarget.TargetProperty switch
            {
                AvaloniaProperty ap => ap.PropertyType,
                PropertyInfo pi => pi.PropertyType,
                _ => null,
            };

            // Look upwards though the ambient context for IResourceHosts and IResourceProviders
            // which might be able to give us the resource.
            foreach (var e in stack.Parents)
            {
                object value;

                if (e is IResourceHost host && host.TryGetResource(ResourceKey, out value))
                {
                    return ColorToBrushConverter.Convert(value, targetType);
                }
                else if (e is IResourceProvider provider && provider.TryGetResource(ResourceKey, out value))
                {
                    return ColorToBrushConverter.Convert(value, targetType);
                }
            }

            if (provideTarget.TargetObject is IControl target &&
                provideTarget.TargetProperty is PropertyInfo property)
            {
                DelayedBinding.Add(target, property, x => GetValue(x, targetType));
                return AvaloniaProperty.UnsetValue;
            }

            throw new KeyNotFoundException($"Static resource '{ResourceKey}' not found.");
        }

        private object GetValue(IStyledElement control, Type targetType)
        {
            return ColorToBrushConverter.Convert(control.FindResource(ResourceKey), targetType);
        }
    }
}
