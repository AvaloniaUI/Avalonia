using System.IO;
using System.Xml.Serialization;
using Microsoft.CodeAnalysis;

namespace Avalonia.SourceGenerator.CompositionGenerator
{
    [Generator(LanguageNames.CSharp)]
    public class CompositionRoslynGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var schema =
                context.AdditionalTextsProvider.Where(static file => file.Path.EndsWith("composition-schema.xml", System.StringComparison.OrdinalIgnoreCase));
            var configs = schema.Select((t, _) => t.GetText())
                .Where(source => source is not null)
                .Select((source, _) => (GConfig)new XmlSerializer(typeof(GConfig)).Deserialize(new StringReader(source!.ToString())));
            context.RegisterSourceOutput(configs, (spc, config) =>
            {
                var generator = new Generator(new RoslynCompositionGeneratorSink(spc), config);
                generator.Generate();
            });
        }
    }
}
