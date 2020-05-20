using System;
using Avalonia.Controls;
using Avalonia.Data;

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
                return InstancedBinding.OneWay(control.GetResourceObservable(ResourceKey));
            }
            else if (_resourceProvider is object)
            {
                return InstancedBinding.OneWay(_resourceProvider.GetResourceObservable(ResourceKey));
            }

            return null;
        }
    }
}
