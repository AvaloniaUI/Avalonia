// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using OmniXaml;
using OmniXaml.ObjectAssembler;
using OmniXaml.Parsers.ProtoParser;
using OmniXaml.Parsers.XamlInstructions;

namespace Perspex.Markup.Xaml.Context
{
    public class PerspexParserFactory : IXamlParserFactory
    {
        private readonly IWiringContext _wiringContext;

        public PerspexParserFactory()
            : this(new TypeFactory())
        {
        }

        public PerspexParserFactory(ITypeFactory typeFactory)
        {
            _wiringContext = new PerspexWiringContext(typeFactory);
        }

        public IXamlParser CreateForReadingFree()
        {
            var objectAssemblerForUndefinedRoot = GetObjectAssemblerForUndefinedRoot();

            return CreateParser(objectAssemblerForUndefinedRoot);
        }

        private IXamlParser CreateParser(IObjectAssembler objectAssemblerForUndefinedRoot)
        {
            var xamlInstructionParser = new OrderAwareXamlInstructionParser(new XamlInstructionParser(_wiringContext));

            var phaseParserKit = new PhaseParserKit(
                new XamlProtoInstructionParser(_wiringContext),
                xamlInstructionParser,
                objectAssemblerForUndefinedRoot);

            return new XamlXmlParser(phaseParserKit);
        }

        private IObjectAssembler GetObjectAssemblerForUndefinedRoot()
        {
            return new ObjectAssembler(_wiringContext, new TopDownValueContext());
        }

        public IXamlParser CreateForReadingSpecificInstance(object rootInstance)
        {
            var objectAssemblerForUndefinedRoot = GetObjectAssemblerForSpecificRoot(rootInstance);

            return CreateParser(objectAssemblerForUndefinedRoot);
        }

        private IObjectAssembler GetObjectAssemblerForSpecificRoot(object rootInstance)
        {
            return new PerspexObjectAssembler(_wiringContext, new ObjectAssemblerSettings { RootInstance = rootInstance });
        }
    }
}