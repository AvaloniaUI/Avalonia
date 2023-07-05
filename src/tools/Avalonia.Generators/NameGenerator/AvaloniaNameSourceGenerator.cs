using System;
using Avalonia.Generators.Common;
using Avalonia.Generators.Common.Domain;
using Avalonia.Generators.Compiler;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Avalonia.Generators.NameGenerator;

[Generator]
public class AvaloniaNameSourceGenerator : ISourceGenerator
{
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

            var partials = generator.GenerateNameReferences(context.AdditionalFiles, context.CancellationToken);
            foreach (var (fileName, content) in partials)
            {
                if(context.CancellationToken.IsCancellationRequested)
                {
                    break;
                }

                context.AddSource(fileName, content);
            }
        }
        catch (Exception exception)
        {
            context.ReportNameGeneratorUnhandledError(exception);
        }
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
            Behavior.InitializeComponent => new InitializeComponentCodeGenerator(types),
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
