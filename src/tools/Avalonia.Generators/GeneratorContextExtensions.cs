using System;
using Microsoft.CodeAnalysis;

namespace Avalonia.Generators;

internal static class GeneratorContextExtensions
{
    private const string UnhandledErrorDescriptorId = "AXN0002";
    private const string InvalidTypeDescriptorId = "AXN0001";

    public static string GetMsBuildProperty(
        this GeneratorExecutionContext context,
        string name,
        string defaultValue = "")
    {
        context.AnalyzerConfigOptions.GlobalOptions.TryGetValue($"build_property.{name}", out var value);
        return value ?? defaultValue;
    }

    public static void ReportNameGeneratorUnhandledError(this GeneratorExecutionContext context, Exception error) =>
        context.Report(UnhandledErrorDescriptorId,
            "Unhandled exception occured while generating typed Name references. " +
            "Please file an issue: https://github.com/avaloniaui/Avalonia",
            error.Message,
            error.ToString());

    public static void ReportNameGeneratorInvalidType(this GeneratorExecutionContext context, string typeName) =>
        context.Report(InvalidTypeDescriptorId,
            $"Avalonia x:Name generator was unable to generate names for type '{typeName}'. " +
            $"The type '{typeName}' does not exist in the assembly.");

    private static void Report(this GeneratorExecutionContext context, string id, string title, string message = null, string description = null) =>
        context.ReportDiagnostic(
            Diagnostic.Create(
                new DiagnosticDescriptor(
                    id: id,
                    title: title,
                    messageFormat: message ?? title,
                    category: "Usage",
                    defaultSeverity: DiagnosticSeverity.Error,
                    isEnabledByDefault: true,
                    description),
                Location.None));
}
