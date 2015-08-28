namespace Perspex.Xaml.Desktop
{
    using Context;
    using HighLevel;
    using OmniXaml;
    using OmniXaml.ObjectAssembler;
    using OmniXaml.Parsers.ProtoParser;
    using OmniXaml.Parsers.XamlNodes;

    public class PerspexParserFactory : IXamlParserFactory
    {
        private readonly IWiringContext wiringContext;

        public PerspexParserFactory(ITypeFactory typeFactory)
        {
            wiringContext = PerspexWiringContextFactory.GetContext(typeFactory);
        }

        public IXamlParser CreateForReadingFree()
        {
            var objectAssemblerForUndefinedRoot = GetObjectAssemblerForUndefinedRoot();

            return CreateParser(objectAssemblerForUndefinedRoot);
        }

        private IXamlParser CreateParser(IObjectAssembler objectAssemblerForUndefinedRoot)
        {
            var xamlInstructionParser = new OrderAwareXamlInstructionParser(new XamlInstructionParser(wiringContext));

            var phaseParserKit = new PhaseParserKit(
                new XamlProtoInstructionParser(wiringContext),
                xamlInstructionParser,
                objectAssemblerForUndefinedRoot);

            return new XamlXmlParser(phaseParserKit);
        }

        private IObjectAssembler GetObjectAssemblerForUndefinedRoot()
        {
            return new ObjectAssembler(wiringContext, new TopDownMemberValueContext());
        }

        public IXamlParser CreateForReadingSpecificInstance(object rootInstance)
        {
            var objectAssemblerForUndefinedRoot = GetObjectAssemblerForSpecificRoot(rootInstance);

            return CreateParser(objectAssemblerForUndefinedRoot);
        }

        private IObjectAssembler GetObjectAssemblerForSpecificRoot(object rootInstance)
        {
            return new PerspexObjectAssembler(wiringContext, new ObjectAssemblerSettings { RootInstance = rootInstance });
        }
    }
}