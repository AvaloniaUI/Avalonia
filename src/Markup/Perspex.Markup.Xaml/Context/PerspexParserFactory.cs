// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using OmniXaml;
using OmniXaml.ObjectAssembler;
using OmniXaml.Parsers.Parser;
using OmniXaml.Parsers.ProtoParser;

namespace Perspex.Markup.Xaml.Context
{
    public class PerspexParserFactory : IParserFactory
    {
        private readonly IRuntimeTypeSource runtimeTypeSource;

        public PerspexParserFactory()
            : this(new TypeFactory())
        {
        }

        public PerspexParserFactory(ITypeFactory typeFactory)
        {
            runtimeTypeSource = new PerspexRuntimeTypeSource(typeFactory);
        }      

        public IParser Create(Settings settings)
        {
            var xamlInstructionParser = new OrderAwareInstructionParser(new InstructionParser(runtimeTypeSource));

            IObjectAssembler objectAssembler = new PerspexObjectAssembler(runtimeTypeSource, settings);
            var phaseParserKit = new PhaseParserKit(
                new ProtoInstructionParser(runtimeTypeSource),
                xamlInstructionParser,
                objectAssembler);

            return new XmlParser(phaseParserKit);
        }
    }
}