using System;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml.Converters;
using Avalonia.Media;

#nullable enable

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
                _priority = BindingPriority.TemplatedParent;

            var provideTarget = serviceProvider.GetService<IProvideValueTarget>();

            if (!(provideTarget.TargetObject is IStyledElement))
            {
                _anchor = serviceProvider.GetFirstParent<IStyledElement>() ??
                    serviceProvider.GetFirstParent<IResourceProvider>() ??
                    (object?)serviceProvider.GetFirstParent<IResourceHost>();
            }

            return this;
        }

        InstancedBinding? IBinding.Initiate(
            IAvaloniaObject target,
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

        private Func<object?, object?>? GetConverter(AvaloniaProperty? targetProperty)
        {
            if (targetProperty?.PropertyType == typeof(IBrush))
            {
                return x => ColorToBrushConverter.Convert(x, typeof(IBrush));
            }

            return null;
        }
    }
}
