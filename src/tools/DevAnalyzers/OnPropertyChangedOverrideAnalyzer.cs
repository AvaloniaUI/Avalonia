using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DevAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class OnPropertyChangedOverrideAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "AVADEV2001";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            "Missing invoke base.OnPropertyChanged",
            "Method '{0}' do not invoke base.{0}",
            "Potential issue",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "The OnPropertyChanged of the base class was not invoked in the override method declaration, which could lead to unwanted behavior.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
        }

        private static void AnalyzeMethod(SymbolAnalysisContext context)
        {
            if (context.Symbol is IMethodSymbol currentMethod
                && currentMethod.Name == "OnPropertyChanged"
                && currentMethod.OverriddenMethod is IMethodSymbol originalMethod)
            {
                var declaration = currentMethod.DeclaringSyntaxReferences.FirstOrDefault()
                    ?.GetSyntax(context.CancellationToken);
                if (declaration is not null && context.Compilation.GetSemanticModel(declaration!.SyntaxTree) is { } semanticModel)
                {
                    if (declaration.SyntaxTree.TryGetRoot(out var root))
                    {
                        var baseInvocations = root.DescendantNodes().OfType<BaseExpressionSyntax>();
                        if (baseInvocations.Any())
                        {
                            foreach (var baseInvocation in baseInvocations)
                            {
                                var parent = baseInvocation.Parent;
                                var targetSymbol = semanticModel.GetSymbolInfo(parent, context.CancellationToken);
                                if (SymbolEqualityComparer.Default.Equals(targetSymbol.Symbol, originalMethod))
                                {
                                    return;
                                }
                            }
                        }
                        context.ReportDiagnostic(Diagnostic.Create(Rule, currentMethod.Locations[0], currentMethod.Name));
                    }
                }
            }
        }

    }
}
