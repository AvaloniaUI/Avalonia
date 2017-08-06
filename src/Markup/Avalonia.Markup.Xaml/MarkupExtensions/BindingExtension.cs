// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Data;
using Avalonia.Markup.Xaml.Data;
using System;

namespace Avalonia.Markup.Xaml.MarkupExtensions
{
    using Portable.Xaml.Markup;
    using PortableXaml;

    [MarkupExtensionReturnType(typeof(IBinding))]
    public class BindingExtension : MarkupExtension
    {
        public BindingExtension()
        {
        }

        public BindingExtension(string path)
        {
            Path = path;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var b = new Binding
            {
                Converter = Converter,
                ConverterParameter = ConverterParameter,
                ElementName = ElementName,
                FallbackValue = FallbackValue,
                Mode = Mode,
                Path = Path,
                Priority = Priority,
                RelativeSource = RelativeSource
            };

            return XamlBinding.FromMarkupExtensionContext(b, serviceProvider);
        }

        public IValueConverter Converter { get; set; }

        public object ConverterParameter { get; set; }

        public string ElementName { get; set; }

        public object FallbackValue { get; set; } = AvaloniaProperty.UnsetValue;

        public BindingMode Mode { get; set; }

        [ConstructorArgument("path")]
        public string Path { get; set; }

        public BindingPriority Priority { get; set; } = BindingPriority.LocalValue;

        public object Source { get; set; }

        public RelativeSource RelativeSource { get; set; }
    }
}