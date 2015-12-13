// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using OmniXaml;
using OmniXaml.ObjectAssembler;
using Perspex.Controls;
using Perspex.Markup.Xaml.Templates;

namespace Perspex.Markup.Xaml.Context
{
    public class PerspexObjectAssembler : IObjectAssembler
    {
        private readonly TemplateHostingObjectAssembler _objectAssembler;

        public PerspexObjectAssembler(IWiringContext wiringContext, ObjectAssemblerSettings objectAssemblerSettings = null)
        {
            var mapping = new DeferredLoaderMapping();
            mapping.Map<ControlTemplate>(x => x.Content, new TemplateLoader());
            mapping.Map<DataTemplate>(x => x.Content, new TemplateLoader());
            mapping.Map<FocusAdornerTemplate>(x => x.Content, new TemplateLoader());
            mapping.Map<TreeDataTemplate>(x => x.Content, new TemplateLoader());
            mapping.Map<ItemsPanelTemplate>(x => x.Content, new TemplateLoader());

            var assembler = new ObjectAssembler(wiringContext, new TopDownValueContext(), objectAssemblerSettings);
            _objectAssembler = new TemplateHostingObjectAssembler(assembler, mapping);
        }


        public object Result => _objectAssembler.Result;

        public InstanceLifeCycleHandler InstanceLifeCycleHandler { get; set; } = new InstanceLifeCycleHandler();

        public EventHandler<XamlSetValueEventArgs> XamlSetValueHandler { get; set; }

        public IWiringContext WiringContext => _objectAssembler.WiringContext;

        public void Process(XamlInstruction node)
        {
            _objectAssembler.Process(node);
        }

        public void OverrideInstance(object instance)
        {
            _objectAssembler.OverrideInstance(instance);
        }
    }
}