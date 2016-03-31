// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Controls;
using Perspex.Markup.Xaml.Context;
using Perspex.Metadata;
using Portable.Xaml;
using Portable.Xaml.Markup;

namespace Perspex.Markup.Xaml.Templates
{
    public class FocusAdornerTemplate : ITemplate<IControl>
    {
        [Content]
        [XamlDeferLoad(typeof(DeferredLoader), typeof(IControl))]
        public TemplateContent Content { get; set; }

        public IControl Build()
        {
            return Content.Load();
        }
    }
}