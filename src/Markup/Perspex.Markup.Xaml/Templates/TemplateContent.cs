// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Controls;
using Portable.Xaml;

namespace Perspex.Markup.Xaml.Templates
{
    public class TemplateContent
    {
        public TemplateContent(XamlReader reader)
        {
            List = new XamlNodeList(reader.SchemaContext);
            XamlServices.Transform(reader, List.Writer);
        }

        public XamlNodeList List { get; set; }

        public IControl Load()
        {
            return (IControl)XamlServices.Load(List.GetReader());
        }
    }
}