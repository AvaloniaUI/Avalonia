





namespace Perspex.Markup.Xaml.Context
{
    using OmniXaml;
    using OmniXaml.ObjectAssembler;
    using OmniXaml.Parsers.ProtoParser;
    using OmniXaml.Parsers.XamlInstructions;

    public class PerspexParserFactory : IXamlParserFactory
    {
        private readonly IWiringContext wiringContext;

        public PerspexParserFactory()
            : this(new TypeFactory())
        {
        }

        public PerspexParserFactory(ITypeFactory typeFactory)
        {
            this.wiringContext = new PerspexWiringContext(typeFactory);
        }

        public IXamlParser CreateForReadingFree()
        {
            var objectAssemblerForUndefinedRoot = this.GetObjectAssemblerForUndefinedRoot();

            return this.CreateParser(objectAssemblerForUndefinedRoot);
        }

        private IXamlParser CreateParser(IObjectAssembler objectAssemblerForUndefinedRoot)
        {
            var xamlInstructionParser = new OrderAwareXamlInstructionParser(new XamlInstructionParser(this.wiringContext));

            var phaseParserKit = new PhaseParserKit(
                new XamlProtoInstructionParser(this.wiringContext),
                xamlInstructionParser,
                objectAssemblerForUndefinedRoot);

            return new XamlXmlParser(phaseParserKit);
        }

        private IObjectAssembler GetObjectAssemblerForUndefinedRoot()
        {
            return new ObjectAssembler(this.wiringContext, new TopDownMemberValueContext());
        }

        public IXamlParser CreateForReadingSpecificInstance(object rootInstance)
        {
            var objectAssemblerForUndefinedRoot = this.GetObjectAssemblerForSpecificRoot(rootInstance);

            return this.CreateParser(objectAssemblerForUndefinedRoot);
        }

        private IObjectAssembler GetObjectAssemblerForSpecificRoot(object rootInstance)
        {
            return new PerspexObjectAssembler(this.wiringContext, new ObjectAssemblerSettings { RootInstance = rootInstance });
        }
    }
}