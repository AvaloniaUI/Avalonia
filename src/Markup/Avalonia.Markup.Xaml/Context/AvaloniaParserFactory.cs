// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using OmniXaml;
using OmniXaml.ObjectAssembler;
using OmniXaml.Parsers.Parser;
using OmniXaml.Parsers.ProtoParser;

namespace Avalonia.Markup.Xaml.Context
{
    public class AvaloniaParserFactory : IParserFactory
    {
        private readonly IRuntimeTypeSource runtimeTypeSource;

        public AvaloniaParserFactory()
            : this(new TypeFactory())
        {
        }

        public AvaloniaParserFactory(ITypeFactory typeFactory)
        {
            runtimeTypeSource = new AvaloniaRuntimeTypeSource(typeFactory);
        }      

        public IParser Create(Settings settings)
        {
            var xamlInstructionParser = new OrderAwareInstructionParser(new InstructionParser(runtimeTypeSource));

            IObjectAssembler objectAssembler = new AvaloniaObjectAssembler(
                runtimeTypeSource,
                new TopDownValueContext(),
                settings);
            var phaseParserKit = new PhaseParserKit(
                new ProtoInstructionParser(runtimeTypeSource),
                xamlInstructionParser,
                objectAssembler);

            return new XmlParser(phaseParserKit);
        }
    }
}