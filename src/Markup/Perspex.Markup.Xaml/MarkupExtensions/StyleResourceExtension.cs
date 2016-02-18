// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using OmniXaml;
using Perspex.LogicalTree;
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
            var styleHost = extensionContext.TargetObject as IStyleHost;

            if (styleHost == null)
            {
                throw new ParseException(
                    $"StyleResource cannot be assigned to an object of type '{styleHost.GetType()}'.");
            }

            return styleHost.FindStyleResource(Name);
        }

        public string Name { get; set; }
    }
}