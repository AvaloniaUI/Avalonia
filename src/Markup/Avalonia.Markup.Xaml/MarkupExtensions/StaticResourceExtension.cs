using System;
using System.Collections.Generic;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Markup.Data;
using Avalonia.Markup.Xaml.Converters;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Avalonia.Styling;

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

            if (provideTarget.TargetObject is Setter { Property: not null } setter)
            {
                targetType = setter.Property.PropertyType;
            }
            
            // Look upwards though the ambient context for IResourceNodes
            // which might be able to give us the resource.
            foreach (var parent in stack.Parents)
            {
                if (parent is IResourceNode node && node.TryGetResource(ResourceKey, out var value))
                {
                    return ColorToBrushConverter.Convert(value, targetType);
                }
            }

            if (provideTarget.TargetObject is Control target &&
                provideTarget.TargetProperty is PropertyInfo property)
            {
                // This is stored locally to avoid allocating closure in the outer scope.
                var localTargetType = targetType;
                var localInstance = this;
                
                DelayedBinding.Add(target, property, x => localInstance.GetValue(x, localTargetType));
                return AvaloniaProperty.UnsetValue;
            }

            throw new KeyNotFoundException($"Static resource '{ResourceKey}' not found.");
        }

        private object GetValue(StyledElement control, Type targetType)
        {
            return ColorToBrushConverter.Convert(control.FindResource(ResourceKey), targetType);
        }
    }
}

