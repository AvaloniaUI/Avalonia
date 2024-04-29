using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia.Generators.Common;
using Avalonia.Generators.Common.Domain;
using Avalonia.Generators.Compiler;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Avalonia.Generators.NameGenerator;

[Generator]
public class AvaloniaNameSourceGenerator : ISourceGenerator
{
    private const string SourceItemGroupMetadata = "build_metadata.AdditionalFiles.SourceItemGroup";

    public void Initialize(GeneratorInitializationContext context) { }

    public void Execute(GeneratorExecutionContext context)
    {
        try
        {
            var generator = CreateNameGenerator(context);
            if (generator is null)
            {
                return;
            }

            var partials = generator.GenerateNameReferences(ResolveAdditionalFiles(context), context.CancellationToken);
            foreach (var (fileName, content) in partials)
            {
                if(context.CancellationToken.IsCancellationRequested)
                {
                    break;
                }

                context.AddSource(fileName, content);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            context.ReportNameGeneratorUnhandledError(exception);
        }
    }

    private static IEnumerable<AdditionalText> ResolveAdditionalFiles(GeneratorExecutionContext context)
    {
        return context
            .AdditionalFiles
            .Where(f => context.AnalyzerConfigOptions
                .GetOptions(f)
                .TryGetValue(SourceItemGroupMetadata, out var sourceItemGroup)
                && sourceItemGroup == "AvaloniaXaml");
    }

    private static INameGenerator CreateNameGenerator(GeneratorExecutionContext context)
    {
        var options = new GeneratorOptions(context);
        if (!options.AvaloniaNameGeneratorIsEnabled)
        {
            return null;
        }

        var types = new RoslynTypeSystem((CSharpCompilation)context.Compilation);
        ICodeGenerator generator = options.AvaloniaNameGeneratorBehavior switch {
            Behavior.OnlyProperties => new OnlyPropertiesCodeGenerator(),
            Behavior.InitializeComponent => new InitializeComponentCodeGenerator(types, options.AvaloniaNameGeneratorAttachDevTools),
            _ => throw new ArgumentOutOfRangeException()
        };

        var compiler = MiniCompiler.CreateDefault(types, MiniCompiler.AvaloniaXmlnsDefinitionAttribute);
        return new AvaloniaNameGenerator(
            options.AvaloniaNameGeneratorViewFileNamingStrategy,
            new GlobPatternGroup(options.AvaloniaNameGeneratorFilterByPath),
            new GlobPatternGroup(options.AvaloniaNameGeneratorFilterByNamespace),
            new XamlXViewResolver(types, compiler, true,
                type => context.ReportNameGeneratorInvalidType(type),
                error => context.ReportNameGeneratorUnhandledError(error)),
            new XamlXNameResolver(options.AvaloniaNameGeneratorClassFieldModifier),
            generator);
    }
}
