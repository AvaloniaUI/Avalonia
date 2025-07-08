using System;
using Avalonia.Generators.Common;
using Avalonia.Generators.Common.Domain;
using Avalonia.Generators.Compiler;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Avalonia.Generators.NameGenerator;

[Generator(LanguageNames.CSharp)]
public class AvaloniaNameIncrementalGenerator : IIncrementalGenerator
{
    private const string SourceItemGroupMetadata = "build_metadata.AdditionalFiles.SourceItemGroup";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var options = context.AnalyzerConfigOptionsProvider
            .Select(static (options, _) => new GeneratorOptions(options.GlobalOptions))
            .Combine(context.AnalyzerConfigOptionsProvider);

        var xamlFiles = context.AdditionalTextsProvider.Combine(options)
            .Select(static (pair, cancellationToken) =>
            {
                var text = pair.Left;
                var options = pair.Right.Left;
                var optionsProvider = pair.Right.Right;
                var filePath = text.Path;

                if (!(filePath.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) ||
                    filePath.EndsWith(".paml", StringComparison.OrdinalIgnoreCase) ||
                    filePath.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase)))
                {
                    return default;
                }

                if (!options.AvaloniaNameGeneratorFilterByPath.Matches(filePath))
                {
                    return default;
                }

                if (!optionsProvider.GetOptions(pair.Left).TryGetValue(SourceItemGroupMetadata, out var itemGroup)
                    || itemGroup != "AvaloniaXaml")
                {
                    return default;
                }

                if (text.GetText(cancellationToken) is not { } textContent)
                {
                    return default;
                }

                return (textContent, options);
            })
            .Where(tuple => tuple.textContent is not null);

        var generatorInput = xamlFiles.Combine(context.CompilationProvider);

        context.RegisterSourceOutput(generatorInput, static (context, pair) =>
        {
            var options = pair.Left.options;
            var textSource = pair.Left.textContent;
            var compilation = pair.Right;

            if (!options.AvaloniaNameGeneratorIsEnabled)
            {
                return;
            }

            var types = new RoslynTypeSystem((CSharpCompilation)compilation);
            ICodeGenerator codeGenerator = options.AvaloniaNameGeneratorBehavior switch
            {
                Behavior.OnlyProperties => new OnlyPropertiesCodeGenerator(),
                Behavior.InitializeComponent => new InitializeComponentCodeGenerator(types, options.AvaloniaNameGeneratorAttachDevTools),
                _ => throw new ArgumentOutOfRangeException()
            };

            var compiler = MiniCompiler.CreateDefault(types, MiniCompiler.AvaloniaXmlnsDefinitionAttribute);
            var generator = new AvaloniaNameGenerator(
                options.AvaloniaNameGeneratorViewFileNamingStrategy,
                options.AvaloniaNameGeneratorFilterByNamespace,
                new XamlXViewResolver(types, compiler, true,
                    type => context.ReportNameGeneratorInvalidType(type),
                    error => context.ReportNameGeneratorUnhandledError(error)),
                new XamlXNameResolver(options.AvaloniaNameGeneratorClassFieldModifier),
                codeGenerator);

            var partialClass = generator.GenerateNameReferences(textSource);
            if (partialClass is not null)
            {
                context.AddSource(partialClass.FileName, partialClass.Content);
            }
        });
    }
}
