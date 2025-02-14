using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
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

        // Keep instance method ProvideValue as simple as possible, increasing chance to inline it.
        // With modern runtimes, inlining this method also helps to eliminate extension allocation completely. 
        public object? ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider, ResourceKey);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static object? ProvideValue(IServiceProvider serviceProvider, object? resourceKey)
        {
            if (resourceKey is null)
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
                ?? GetDictionaryVariant(stack);

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
                // avoid allocations iterating the parents when possible
                if (stack is IAvaloniaXamlIlEagerParentStackProvider eagerStack)
                {
                    var enumerator = new EagerParentStackEnumerator(eagerStack);
                    while (enumerator.TryGetNextOfType<IResourceNode>() is { } node)
                    {
                        if (node.TryGetResource(resourceKey, themeVariant, out var value))
                        {
                            return ColorToBrushConverter.Convert(value, targetType);
                        }
                    }
                }
                else
                {
                    foreach (var parent in stack.Parents)
                    {
                        if (parent is IResourceNode node && node.TryGetResource(resourceKey, themeVariant, out var value))
                        {
                            return ColorToBrushConverter.Convert(value, targetType);
                        }
                    }
                }
            }

            if (targetObject is Control target &&
                targetProperty is Avalonia.Data.Core.IPropertyInfo property)
            {
                // This is stored locally to avoid allocating closure in the outer scope.
                var localTargetType = targetType;
                var localKeyInstance = resourceKey;
                
                DelayedBinding.Add(target, property, x => 
                    ColorToBrushConverter.Convert(x.FindResource(localKeyInstance), localTargetType));
                return AvaloniaProperty.UnsetValue;
            }

            throw new KeyNotFoundException($"Static resource '{resourceKey}' not found.");
        }

        internal static ThemeVariant? GetDictionaryVariant(IAvaloniaXamlIlParentStackProvider? stack)
        {
            switch (stack)
            {
                case null:
                    return null;

                case IAvaloniaXamlIlEagerParentStackProvider eager:
                    var enumerator = new EagerParentStackEnumerator(eager);

                    while (enumerator.TryGetNextOfType<IThemeVariantProvider>() is { } themeVariantProvider)
                    {
                        if (themeVariantProvider.Key is { } setKey)
                        {
                            return setKey;
                        }
                    }

                    return null;

                case { } provider:
                    foreach (var parent in provider.Parents)
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
}

