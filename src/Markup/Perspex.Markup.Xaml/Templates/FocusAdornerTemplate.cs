// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Controls;
using Perspex.Metadata;

namespace Perspex.Markup.Xaml.Templates
{
    public class FocusAdornerTemplate : ITemplate<IControl>
    {
        [Content]
        public TemplateContent Content { get; set; }

        public IControl Build()
        {
            return Content.Load();
        }
    }
}