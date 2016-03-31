// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Controls;
using Perspex.Markup.Xaml.Context;
using Perspex.Metadata;
using Portable.Xaml.Markup;

namespace Perspex.Markup.Xaml.Templates
{
    public class ItemsPanelTemplate : ITemplate<IPanel>
    {
        [Content]
        [XamlDeferLoad(typeof(DeferredLoader), typeof(IPanel))]
        public TemplateContent Content { get; set; }

        public IPanel Build()
        {
            return (IPanel)Content.Load();
        }
    }
}
