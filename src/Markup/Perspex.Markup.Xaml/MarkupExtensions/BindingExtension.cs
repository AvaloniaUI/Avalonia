// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using OmniXaml;
using Perspex.Controls;
using Perspex.Markup.Xaml.Binding;

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
            return new XamlBindingDefinition
            {
                Mode = Mode,
                SourcePropertyPath = Path,
            };
        }

        public object Converter { get; set; }
        public BindingMode Mode { get; set; }
        public string Path { get; set; }
    }
}