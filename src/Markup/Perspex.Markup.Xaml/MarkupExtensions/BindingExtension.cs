// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using OmniXaml;
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
                ElementName = ElementName,
                Mode = Mode,
                Path = Path,
            };
        }

        public IValueConverter Converter { get; set; }
        public string ElementName { get; set; }
        public BindingMode Mode { get; set; }
        public string Path { get; set; }
    }
}