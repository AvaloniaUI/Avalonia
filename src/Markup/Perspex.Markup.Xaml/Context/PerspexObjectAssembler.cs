





namespace Perspex.Markup.Xaml.Context
{
    using System;
    using OmniXaml;
    using OmniXaml.ObjectAssembler;
    using Templates;

    public class PerspexObjectAssembler : IObjectAssembler
    {
        private readonly TemplateHostingObjectAssembler objectAssembler;

        public PerspexObjectAssembler(IWiringContext wiringContext, ObjectAssemblerSettings objectAssemblerSettings = null)
        {
            var mapping = new DeferredLoaderMapping();
            mapping.Map<XamlDataTemplate>(template => template.Content, new TemplateLoader());

            var assembler = new ObjectAssembler(wiringContext, new TopDownMemberValueContext(), objectAssemblerSettings);
            this.objectAssembler = new TemplateHostingObjectAssembler(assembler, mapping);
        }


        public object Result => this.objectAssembler.Result;

        public EventHandler<XamlSetValueEventArgs> XamlSetValueHandler { get; set; }

        public IWiringContext WiringContext => this.objectAssembler.WiringContext;

        public void Process(XamlInstruction node)
        {
            this.objectAssembler.Process(node);
        }

        public void OverrideInstance(object instance)
        {
            this.objectAssembler.OverrideInstance(instance);
        }
    }
}