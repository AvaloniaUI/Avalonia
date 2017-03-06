// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls;
using System.Collections.Generic;

namespace Avalonia.Markup.Xaml.Templates
{
#if !OMNIXAML
    using Portable.Xaml;

    public class TemplateContent
    {
        public TemplateContent(IEnumerable<NamespaceDeclaration> namespaces, XamlReader reader)
        {
            List = new XamlNodeList(reader.SchemaContext);

            //we need to rpeserve all namespace and prefixes to writer
            //otherwise they are lost. a bug in Portable.xaml or by design ??
            foreach (var ns in namespaces)
            {
                List.Writer.WriteNamespace(ns);
            }

            XamlServices.Transform(reader, List.Writer);
        }

        public XamlNodeList List { get; }

        public IControl Load()
        {
            return (IControl)AvaloniaXamlLoader.LoadFromReader(List.GetReader());
        }

        public static IControl Load(object templateContent)
        {
            return ((TemplateContent)templateContent).Load();
        }
    }
#else

    using Avalonia.Markup.Xaml.Context;
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

        public static IControl Load(object templateContent)
        {
            return ((TemplateContent)templateContent).Load();
        }
    }

#endif
}