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
                context.AdditionalTextsProvider.Where(static file => file.Path.EndsWith("composition-schema.xml"));
            var configs = schema.Select((t, _) =>
                (GConfig)new XmlSerializer(typeof(GConfig)).Deserialize(new StringReader(t.GetText().ToString())));
            context.RegisterSourceOutput(configs, (spc, config) =>
            {
                var generator = new Generator(new RoslynCompositionGeneratorSink(spc), config);
                generator.Generate();
            });
        }
    }
}