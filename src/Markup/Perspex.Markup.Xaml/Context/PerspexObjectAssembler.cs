// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using OmniXaml;
using OmniXaml.ObjectAssembler;
using Perspex.Controls;
using Perspex.Markup.Xaml.Templates;

namespace Perspex.Markup.Xaml.Context
{
    using OmniXaml.ObjectAssembler.Commands;

    public class PerspexObjectAssembler : IObjectAssembler
    {
        private readonly TemplateHostingObjectAssembler objectAssembler;
        private readonly ObjectAssembler assembler;

        public PerspexObjectAssembler(IRuntimeTypeSource runtimeTypeSource, Settings objectAssemblerSettings = null)
        {
            var mapping = new DeferredLoaderMapping();
            mapping.Map<ControlTemplate>(x => x.Content, new TemplateLoader());
            mapping.Map<DataTemplate>(x => x.Content, new TemplateLoader());
            mapping.Map<FocusAdornerTemplate>(x => x.Content, new TemplateLoader());
            mapping.Map<TreeDataTemplate>(x => x.Content, new TemplateLoader());
            mapping.Map<ItemsPanelTemplate>(x => x.Content, new TemplateLoader());

            assembler = new ObjectAssembler(runtimeTypeSource, new TopDownValueContext(), objectAssemblerSettings);
            objectAssembler = new TemplateHostingObjectAssembler(assembler, mapping);
        }


        public object Result => objectAssembler.Result;

        public EventHandler<XamlSetValueEventArgs> XamlSetValueHandler { get; set; }

        public IRuntimeTypeSource TypeSource => assembler.TypeSource;

        public ITopDownValueContext TopDownValueContext => assembler.TopDownValueContext;

        public IInstanceLifeCycleListener LifecycleListener
        {
            get { throw new NotImplementedException(); }
        }

        public void Process(Instruction node)
        {
            objectAssembler.Process(node);
        }

        public void OverrideInstance(object instance)
        {
            objectAssembler.OverrideInstance(instance);
        }
    }
}