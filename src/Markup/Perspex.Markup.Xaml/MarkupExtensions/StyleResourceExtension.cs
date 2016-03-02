// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;
using OmniXaml;
using Perspex.LogicalTree;
using Perspex.Markup.Xaml.Data;
using Perspex.Styling;

namespace Perspex.Markup.Xaml.MarkupExtensions
{
    public class StyleResourceExtension : MarkupExtension
    {
        public StyleResourceExtension(string name)
        {
            Name = name;
        }

        public override object ProvideValue(MarkupExtensionContext extensionContext)
        {
            return new StyleResourceBinding(this.Name);
        }

        public string Name { get; set; }
    }
}