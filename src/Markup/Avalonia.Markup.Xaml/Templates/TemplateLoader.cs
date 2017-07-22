// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Markup.Xaml.Templates
{
#if !OMNIXAML

    using Portable.Xaml;
    using Portable.Xaml.ComponentModel;
    using System;

    public class TemplateLoader : XamlDeferringLoader
    {
        public override object Load(XamlReader xamlReader, IServiceProvider serviceProvider)
        {
            var tdc = (ITypeDescriptorContext)serviceProvider;
            var ns = tdc.GetService<IXamlNamespaceResolver>();
            return new TemplateContent(ns.GetNamespacePrefixes(), xamlReader);
        }

        public override XamlReader Save(object value, IServiceProvider serviceProvider)
        {
            return ((TemplateContent)value).List.GetReader();
        }
    }

#else

    using System.Collections.Generic;
    using OmniXaml;

    public class TemplateLoader : IDeferredLoader
    {
        public object Load(IEnumerable<Instruction> nodes, IRuntimeTypeSource runtimeTypeSource)
        {
            return new TemplateContent(nodes, runtimeTypeSource);
        }
    }

#endif
}