// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using OmniXaml;
using Perspex.Data;
using Perspex.Markup.Data;
using Perspex.Markup.Xaml.Data;

namespace Perspex.Markup.Xaml.MarkupExtensions
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
                ValidationMethods = ValidationMethods
            };
        }

        public IValueConverter Converter { get; set; }
        public object ConverterParameter { get; set; }
        public string ElementName { get; set; }
        public object FallbackValue { get; set; }
        public BindingMode Mode { get; set; }
        public string Path { get; set; }
        public BindingPriority Priority { get; set; } = BindingPriority.LocalValue;
        public object Source { get; set; }
        public ValidationMethods ValidationMethods { get; set; } = ValidationMethods.None;
    }
}