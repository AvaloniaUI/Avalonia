// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Data;
using Avalonia.Markup.Xaml.Data;
using System;

namespace Avalonia.Markup.Xaml.MarkupExtensions
{
#if !OMNIXAML

    using Portable.Xaml.Markup;
    using PortableXaml;

    [MarkupExtensionReturnType(typeof(IBinding))]
    public class StyleResourceExtension : MarkupExtension
    {
        public StyleResourceExtension(string name)
        {
            Name = name;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return XamlBinding.FromMarkupExtensionContext(
                            new StyleResourceBinding(Name),
                            serviceProvider);
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