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
        private IStyledElement? _anchor;
        private IResourceProvider? _resourceProvider;

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
            var provideTarget = serviceProvider.GetService<IProvideValueTarget>();

            if (!(provideTarget.TargetObject is IStyledElement))
            {
                _anchor = serviceProvider.GetFirstParent<IStyledElement>();

                if (_anchor is null)
                {
                    _resourceProvider = serviceProvider.GetFirstParent<IResourceProvider>();
                }
            }

            return this;
        }

        InstancedBinding? IBinding.Initiate(
            IAvaloniaObject target,
            AvaloniaProperty targetProperty,
            object anchor,
            bool enableDataValidation)
        {
            if (ResourceKey is null)
            {
                return null;
            }

            var control = target as IStyledElement ?? _anchor as IStyledElement;

            if (control != null)
            {
                var source = control.GetResourceObservable(ResourceKey, GetConverter(targetProperty));
                return InstancedBinding.OneWay(source);
            }
            else if (_resourceProvider is object)
            {
                var source = _resourceProvider.GetResourceObservable(ResourceKey, GetConverter(targetProperty));
                return InstancedBinding.OneWay(source);
            }

            return null;
        }

        private Func<object?, object?>? GetConverter(AvaloniaProperty targetProperty)
        {
            if (targetProperty?.PropertyType == typeof(IBrush))
            {
                return x => ColorToBrushConverter.Convert(x, typeof(IBrush));
            }

            return null;
        }
    }
}
