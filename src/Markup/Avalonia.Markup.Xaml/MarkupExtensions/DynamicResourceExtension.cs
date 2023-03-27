using System;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml.Converters;
using Avalonia.Media;

namespace Avalonia.Markup.Xaml.MarkupExtensions
{
    public class DynamicResourceExtension : IBinding
    {
        private object? _anchor;
        private BindingPriority _priority;

        public DynamicResourceExtension()
        {
        }

        public DynamicResourceExtension(object resourceKey)
        {
            ResourceKey = resourceKey;
        }

        public object? ResourceKey { get; set; }

        public IBinding ProvideValue(IServiceProvider serviceProvider)
        {
            if (serviceProvider.IsInControlTemplate())
                _priority = BindingPriority.Template;

            var provideTarget = serviceProvider.GetService<IProvideValueTarget>();

            if (provideTarget?.TargetObject is not StyledElement)
            {
                _anchor = serviceProvider.GetFirstParent<StyledElement>() ??
                    serviceProvider.GetFirstParent<IResourceProvider>() ??
                    (object?)serviceProvider.GetFirstParent<IResourceHost>();
            }

            return this;
        }

        InstancedBinding? IBinding.Initiate(
            AvaloniaObject target,
            AvaloniaProperty? targetProperty,
            object? anchor,
            bool enableDataValidation)
        {
            if (ResourceKey is null)
            {
                return null;
            }

            var control = target as IResourceHost ?? _anchor as IResourceHost;

            if (control != null)
            {
                var source = control.GetResourceObservable(ResourceKey, GetConverter(targetProperty));
                return InstancedBinding.OneWay(source, _priority);
            }
            else if (_anchor is IResourceProvider resourceProvider)
            {
                var source = resourceProvider.GetResourceObservable(ResourceKey, GetConverter(targetProperty));
                return InstancedBinding.OneWay(source, _priority);
            }

            return null;
        }

        private static Func<object?, object?>? GetConverter(AvaloniaProperty? targetProperty)
        {
            if (targetProperty?.PropertyType == typeof(IBrush))
            {
                return x => ColorToBrushConverter.Convert(x, typeof(IBrush));
            }

            return null;
        }
    }
}
