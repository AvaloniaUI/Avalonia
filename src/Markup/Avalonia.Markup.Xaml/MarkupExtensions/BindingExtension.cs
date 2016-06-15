// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using OmniXaml;
using Avalonia.Data;
using Avalonia.Markup.Xaml.Data;

namespace Avalonia.Markup.Xaml.MarkupExtensions
{
    public class BindingExtension : MarkupExtension
    {
        public BindingExtension()
        {
        }

        public BindingExtension(string path)
        {
            Path = path;
        }

        public override object ProvideValue(MarkupExtensionContext extensionContext)
        {
            return new Binding
            {
                Converter = Converter,
                ConverterParameter = ConverterParameter,
                ElementName = ElementName,
                FallbackValue = FallbackValue,
                Mode = Mode,
                Path = Path,
                Priority = Priority,
                EnableValidation = EnableValidation,
            };
        }

        public IValueConverter Converter { get; set; }
        public object ConverterParameter { get; set; }
        public string ElementName { get; set; }
        public object FallbackValue { get; set; } = AvaloniaProperty.UnsetValue;
        public BindingMode Mode { get; set; }
        public string Path { get; set; }
        public BindingPriority Priority { get; set; } = BindingPriority.LocalValue;
        public object Source { get; set; }
        public bool EnableValidation { get; set; }
    }
}