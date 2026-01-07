using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Avalonia.Analyzers;

/// <summary>
/// Analyzes object creation expressions to detect instances where a Bitmap is initialized
/// from the "avares" scheme directly, which is not allowed. Instead, the AssetLoader should be used
/// to open assets as a stream first.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class BitmapAnalyzer: DiagnosticAnalyzer
{
    private const string Title = "Cannot initialize Bitmap from \"avares\" scheme";
    private const string MessageFormat = "Cannot initialize Bitmap from \"avares\" scheme directly";
    private const string Description = "Cannot initialize Bitmap from \"avares\" scheme, use AssetLoader to open assets as stream first.";
    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticIds.Bitmap,
        Title, 
        MessageFormat, 
        Category,
        DiagnosticSeverity.Warning, 
        isEnabledByDefault: true, 
        description: Description);
    
    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ObjectCreationExpression);
    }
    
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(_rule); } }

    private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        var objectCreation = (ObjectCreationExpressionSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        // Check if the object creation is creating an instance of Avalonia.Media.Imaging.Bitmap
        var symbol = semanticModel.GetSymbolInfo(objectCreation).Symbol as IMethodSymbol;
        if (symbol == null || symbol.ContainingType.ToString() != "Avalonia.Media.Imaging.Bitmap")
        {
            return;
        }

        // Check if any argument starts with "avares://"
        foreach (var argument in objectCreation.ArgumentList.Arguments)
        {
            var constantValue = semanticModel.GetConstantValue(argument.Expression);
            if (constantValue.HasValue && constantValue.Value is string stringValue && stringValue.StartsWith("avares://"))
            {
                var diagnostic = Diagnostic.Create(_rule, objectCreation.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
    
}
