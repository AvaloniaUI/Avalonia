// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Markup.Xaml.Data;
using Portable.Xaml.Markup;

namespace Perspex.Markup.Xaml.MarkupExtensions
{
    public class StyleResourceExtension : MarkupExtension
    {
        public StyleResourceExtension(string name)
        {
            Name = name;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new StyleResourceBinding(this.Name);
        }

        public string Name { get; set; }
    }
}