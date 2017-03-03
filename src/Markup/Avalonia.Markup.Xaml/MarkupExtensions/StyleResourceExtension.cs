// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;

using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml.Data;
using Avalonia.Styling;


namespace Avalonia.Markup.Xaml.MarkupExtensions
{
#if !OMNIXAML

    using Portable.Xaml.Markup;

    [MarkupExtensionReturnType(typeof(StyleResourceBinding))]
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

        [ConstructorArgument("name")]
        public string Name { get; set; }
    }

#else

    using OmniXaml;

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
#endif
}