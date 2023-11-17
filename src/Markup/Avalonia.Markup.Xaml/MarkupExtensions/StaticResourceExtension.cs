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

        public object? ResourceKey { get; set; }

        public object? ProvideValue(IServiceProvider serviceProvider)
        {
            if (ResourceKey is not { } resourceKey)
            {
                throw new ArgumentException("StaticResourceExtension.ResourceKey must be set.");
            }

            var stack = serviceProvider.GetService<IAvaloniaXamlIlParentStackProvider>();
            var provideTarget = serviceProvider.GetService<IProvideValueTarget>();
            var targetObject = provideTarget?.TargetObject;
            var targetProperty = provideTarget?.TargetProperty switch
            {
                AvaloniaProperty ap => ap,
                PropertyInfo pi => new Avalonia.Data.Core.ReflectionClrPropertyInfo(pi),
                _ => provideTarget?.TargetProperty,
            };

            var themeVariant = (targetObject as IThemeVariantHost)?.ActualThemeVariant
                ?? GetDictionaryVariant(serviceProvider);

            var targetType = targetProperty switch
            {
                AvaloniaProperty ap => ap.PropertyType,
                Avalonia.Data.Core.IPropertyInfo cpi => cpi.PropertyType,
                _ => null
            };

            if (targetObject is Setter { Property: { } setterProperty })
            {
                targetType = setterProperty.PropertyType;
            }

            // Look upwards though the ambient context for IResourceNodes
            // which might be able to give us the resource.
            if (stack is not null)
            {
                foreach (var parent in stack.Parents)
                {
                    if (parent is IResourceNode node && node.TryGetResource(resourceKey, themeVariant, out var value))
                    {
                        return ColorToBrushConverter.Convert(value, targetType);
                    }
                }
            }

            if (targetObject is Control target &&
                targetProperty is Avalonia.Data.Core.IPropertyInfo property)
            {
                // This is stored locally to avoid allocating closure in the outer scope.
                var localTargetType = targetType;
                var localInstance = this;
                
                DelayedBinding.Add(target, property, x => localInstance.GetValue(x, localTargetType));
                return AvaloniaProperty.UnsetValue;
            }

            throw new KeyNotFoundException($"Static resource '{resourceKey}' not found.");
        }

        private object? GetValue(StyledElement control, Type? targetType)
        {
            return ColorToBrushConverter.Convert(control.FindResource(ResourceKey!), targetType);
        }

        internal static ThemeVariant? GetDictionaryVariant(IServiceProvider serviceProvider)
        {
            var parents = serviceProvider.GetService<IAvaloniaXamlIlParentStackProvider>()?.Parents;
            if (parents is null)
            {
                return null;
            }

            foreach (var parent in parents)
            {
                if (parent is IThemeVariantProvider { Key: { } setKey })
                {
                    return setKey;
                }
            }

            return null;
        }
    }
}

