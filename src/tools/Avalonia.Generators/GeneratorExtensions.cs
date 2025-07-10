using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Avalonia.Generators;

internal static class GeneratorExtensions
{
    private const string UnhandledErrorDescriptorId = "AXN0002";
    private const string InvalidTypeDescriptorId = "AXN0001";

    public static string GetMsBuildProperty(
        this AnalyzerConfigOptions options,
        string name,
        string defaultValue = "")
    {
        options.TryGetValue($"build_property.{name}", out var value);
        return value ?? defaultValue;
    }

    public static DiagnosticDescriptor NameGeneratorUnhandledError(Exception error) => new(
        UnhandledErrorDescriptorId,
        title: "Unhandled exception occurred while generating typed Name references. " +
               "Please file an issue: https://github.com/avaloniaui/Avalonia",
        messageFormat: error.Message,
        description: error.ToString(),
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor NameGeneratorInvalidType(string typeName) => new(
        InvalidTypeDescriptorId,
        title: $"Avalonia x:Name generator was unable to generate names for type '{typeName}'. " +
               $"The type '{typeName}' does not exist in the assembly.",
        messageFormat: $"Avalonia x:Name generator was unable to generate names for type '{typeName}'. " +
                       $"The type '{typeName}' does not exist in the assembly.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static void Report(this SourceProductionContext context, DiagnosticDescriptor diagnostics) =>
        context.ReportDiagnostic(Diagnostic.Create(diagnostics, Location.None));
}
