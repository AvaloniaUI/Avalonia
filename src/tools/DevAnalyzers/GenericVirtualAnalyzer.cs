using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DevAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class GenericVirtualAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "AVADEV1001";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        "Do not use generic virtual methods",
        "Method '{0}' is a generic virtual method",
        "Performance",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Generic virtual methods affect JIT startup time adversely and should be avoided.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context)
    {
        var symbol = (IMethodSymbol)context.Symbol;

        if (symbol.IsGenericMethod &&
            (symbol.IsVirtual || symbol.ContainingType.TypeKind == TypeKind.Interface))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, symbol.Locations[0], symbol.Name));
        }
    }
}
