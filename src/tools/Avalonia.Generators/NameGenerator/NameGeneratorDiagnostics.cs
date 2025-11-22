using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Avalonia.Generators.NameGenerator;

internal static class NameGeneratorDiagnostics
{
    private const string Category = "Avalonia.NameGenerator";
    private const string BugReportLink = "https://github.com/AvaloniaUI/Avalonia/issues/new/choose";

    // Name generation errors should typicially be warnings, because that allows the compile to proceed and
    // reach the point at which code errors are reported. These can give the user actionable information
    // about what they need to fix, which the name generator doesn't have.

    public static readonly DiagnosticDescriptor InvalidType = new(
        "AXN0001", $"Invalid type",
        "Avalonia could not generate code-behind properties or the InitializeContext method because the x:Class type '{0}' was not found in the project",
        Category,
        defaultSeverity: DiagnosticSeverity.Warning, isEnabledByDefault: true);

    [SuppressMessage("MicrosoftCodeAnalysisDesign", "RS1032:Define diagnostic message correctly", Justification = "Printing internal exception")]
    public static readonly DiagnosticDescriptor InternalError = new(
        "AXN0002", "Internal error",
        messageFormat: $"Avalonia encountered an internal error while generating code-behind properties and/or the InitializeContext method. " +
                       $"Please file a bug report at {BugReportLink}. The exception is {{0}}",
        Category,
        DiagnosticSeverity.Error, true,
        helpLinkUri: BugReportLink);

    public static readonly DiagnosticDescriptor ParseFailed = new(
        "AXN0003", $"XAML error",
        "Avalonia could not generate code-behind properties for named elements due to a XAML error: {0}",
        Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NamedElementFailed = new(
        "AXN0004", $"XAML error",
        "Avalonia could not generate code-behind property for '{0}' due to a XAML error: {1}",
        Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);
}
