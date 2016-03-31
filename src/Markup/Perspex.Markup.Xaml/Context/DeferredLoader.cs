// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Markup.Xaml.Templates;
using Portable.Xaml;

namespace Perspex.Markup.Xaml.Context
{
    public class DeferredLoader : XamlDeferringLoader
    {
        public override object Load(XamlReader xamlReader, IServiceProvider serviceProvider)
        {
            return new TemplateContent(xamlReader);
        }

        public override XamlReader Save(object value, IServiceProvider serviceProvider)
        {
            throw new NotImplementedException();
        }
    }
}
