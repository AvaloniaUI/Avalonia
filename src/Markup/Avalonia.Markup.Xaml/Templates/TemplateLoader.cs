// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Xaml;

namespace Avalonia.Markup.Xaml.Templates
{
    public class TemplateLoader : XamlDeferringLoader
    {
        public override object Load(XamlReader xamlReader, IServiceProvider serviceProvider)
        {
            var factory = serviceProvider.GetService<IXamlObjectWriterFactory>();
            return new TemplateContent(xamlReader, factory);
        }

        public override XamlReader Save(object value, IServiceProvider serviceProvider)
        {
            return ((TemplateContent)value).List.GetReader();
        }
    }
}
