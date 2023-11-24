using System;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Styling;

namespace Avalonia.Markup.Xaml.MarkupExtensions
{
    public class DynamicResourceExtension : IBinding
    {
        private object? _anchor;
        private BindingPriority _priority;
        private ThemeVariant? _themeVariant;

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

            _themeVariant = StaticResourceExtension.GetDictionaryVariant(serviceProvider);

            return this;
        }

        InstancedBinding? IBinding.Initiate(
            AvaloniaObject target,
            AvaloniaProperty? targetProperty,
            object? anchor,
            bool enableDataValidation)
        {
            if (ResourceKey is null)
                return null;
            var expression = new DynamicResourceExpression(ResourceKey, _anchor, _themeVariant);
            return new InstancedBinding(target, expression, BindingMode.OneWay, _priority);
        }
    }
}
