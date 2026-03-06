using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.VisualBasic;

namespace Avalonia.Analyzers;

[DiagnosticAnalyzer(LanguageNames.VisualBasic)]
public partial class AvaloniaPropertyAnalyzer
{
    private static partial TypeReference TypeReferenceFromInvocationTypeParameter(IInvocationOperation invocation, ITypeParameterSymbol typeParameter)
    {
        var argument = invocation.TargetMethod.TypeArguments[typeParameter.Ordinal];
        var typeArgumentSyntax = invocation.Syntax;

        return new TypeReference(argument, typeArgumentSyntax.GetLocation());
    }

    private static partial bool IsSimpleAssignmentNode(SyntaxNode node)
        => node.IsKind(SyntaxKind.SimpleAssignmentStatement);

    private static partial bool IsInvocationNode(SyntaxNode node)
        => node.IsKind(SyntaxKind.InvocationExpression);
}
