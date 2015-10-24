// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using OmniXaml;
using Perspex.Markup.Xaml.Binding;

namespace Perspex.Markup.Xaml.MarkupExtensions
{
    public class TemplateBindingExtension : MarkupExtension
    {
        public TemplateBindingExtension()
        {
        }

        public TemplateBindingExtension(string path)
        {
            Path = path;
        }

        public override object ProvideValue(MarkupExtensionContext extensionContext)
        {
            return new XamlBindingDefinition
            {
                Mode = Mode,
                Priority = BindingPriority.TemplatedParent,
                RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent),
                SourcePropertyPath = Path,
            };
        }

        public string Path { get; set; }
        public BindingMode Mode { get; set; }
    }
}