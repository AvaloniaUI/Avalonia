using System;
using System.Runtime.CompilerServices;
using Avalonia.NameGenerator.Compiler;
using Avalonia.NameGenerator.Domain;
using Avalonia.NameGenerator.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

[assembly: InternalsVisibleTo("Avalonia.NameGenerator.Tests")]

namespace Avalonia.NameGenerator;

[Generator]
public class AvaloniaNameSourceGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context) { }

    public void Execute(GeneratorExecutionContext context)
    {
        try
        {
            var generator = CreateNameGenerator(context);
            var partials = generator.GenerateNameReferences(context.AdditionalFiles);
            foreach (var (fileName, content) in partials) context.AddSource(fileName, content);
        }
        catch (Exception exception)
        {
            ReportUnhandledError(context, exception);
        }
    }

    private static INameGenerator CreateNameGenerator(GeneratorExecutionContext context)
    {
        var options = new GeneratorOptions(context);
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
            new XamlXViewResolver(types, compiler, true, type => ReportInvalidType(context, type)),
            new XamlXNameResolver(options.AvaloniaNameGeneratorDefaultFieldModifier),
            generator);
    }

    private static void ReportUnhandledError(GeneratorExecutionContext context, Exception error)
    {
        const string message =
            "Unhandled exception occured while generating typed Name references. " +
            "Please file an issue: https://github.com/avaloniaui/avalonia.namegenerator";
        context.ReportDiagnostic(
            Diagnostic.Create(
                new DiagnosticDescriptor(
                    "AXN0002",
                    message,
                    error.ToString(),
                    "Usage",
                    DiagnosticSeverity.Error,
                    true),
                Location.None));
    }

    private static void ReportInvalidType(GeneratorExecutionContext context, string typeName)
    {
        var message =
            $"Avalonia x:Name generator was unable to generate names for type '{typeName}'. " +
            $"The type '{typeName}' does not exist in the assembly.";
        context.ReportDiagnostic(
            Diagnostic.Create(
                new DiagnosticDescriptor(
                    "AXN0001",
                    message,
                    message,
                    "Usage",
                    DiagnosticSeverity.Error,
                    true),
                Location.None));
    }
}