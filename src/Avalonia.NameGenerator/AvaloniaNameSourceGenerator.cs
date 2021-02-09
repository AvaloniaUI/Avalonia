using System;
using System.Runtime.CompilerServices;
using Avalonia.NameGenerator.Compiler;
using Avalonia.NameGenerator.Domain;
using Avalonia.NameGenerator.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

[assembly: InternalsVisibleTo("Avalonia.NameGenerator.Tests")]

namespace Avalonia.NameGenerator
{
    [Generator]
    public class AvaloniaNameSourceGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context) { }

        public void Execute(GeneratorExecutionContext context)
        {
            var compilation = (CSharpCompilation)context.Compilation;
            var types = new RoslynTypeSystem(compilation);
            var compiler = MiniCompiler.CreateDefault(types, MiniCompiler.AvaloniaXmlnsDefinitionAttribute);

            INameGenerator avaloniaNameGenerator =
                new AvaloniaNameGenerator(
                    new XamlXClassResolver(types, compiler, true, type => ReportInvalidType(context, type)),
                    new XamlXNameResolver(),
                    new FindControlCodeGenerator());

            try
            {
                var partials = avaloniaNameGenerator.GenerateNameReferences(context.AdditionalFiles);
                foreach (var partial in partials) context.AddSource(partial.FileName, partial.Content);
            }
            catch (Exception exception)
            {
                ReportUnhandledError(context, exception);
            }
        }

        private static void ReportUnhandledError(GeneratorExecutionContext context, Exception error)
        {
            const string message = "Unhandled exception occured while generating typed Name references. " +
                                   "Please file an issue: https://github.com/avaloniaui/avalonia.namegenerator";
            context.ReportDiagnostic(
                Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "AXN0002",
                        message,
                        error.ToString(),
                        "Usage",
                        DiagnosticSeverity.Warning,
                        true),
                    Location.None));
        }

        private static void ReportInvalidType(GeneratorExecutionContext context, string typeName)
        {
            var message = $"Avalonia x:Name generator was unable to generate names for type '{typeName}'. " +
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
}
