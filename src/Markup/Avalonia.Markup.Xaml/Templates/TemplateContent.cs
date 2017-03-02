// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Context;

namespace Avalonia.Markup.Xaml.Templates
{
#if !OMNIXAML
    using Portable.Xaml;

    public class TemplateContent
    {
        public TemplateContent()
        {
        }

        public TemplateContent(XamlReader reader)
        {
            List = new XamlNodeList(reader.SchemaContext);
            XamlServices.Transform(reader, List.Writer);
        }

        public XamlNodeList List { get; set; }

        public IControl Load()
        {
            //return (IControl)XamlServices.Load(List.GetReader());
            return (IControl)AvaloniaXamlLoader.LoadFromReader(List.GetReader());
        }

        public static IControl Load(object templateContent)
        {
            return ((TemplateContent)templateContent).Load();
        }
    }
#else

    using OmniXaml;
    using OmniXaml.ObjectAssembler;

    public class TemplateContent
    {
        private readonly IEnumerable<Instruction> nodes;
        private readonly IRuntimeTypeSource runtimeTypeSource;

        public TemplateContent(IEnumerable<Instruction> nodes, IRuntimeTypeSource runtimeTypeSource)
        {
            this.nodes = nodes;
            this.runtimeTypeSource = runtimeTypeSource;
        }

        public Control Load()
        {
            var assembler = new AvaloniaObjectAssembler(
                runtimeTypeSource,
                new TopDownValueContext());

            foreach (var xamlNode in nodes)
            {
                assembler.Process(xamlNode);
            }

            return (Control)assembler.Result;
        }
    }

#endif
}