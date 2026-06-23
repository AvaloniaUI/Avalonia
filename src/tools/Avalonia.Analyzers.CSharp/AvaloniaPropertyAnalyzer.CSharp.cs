using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Avalonia.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public partial class AvaloniaPropertyAnalyzer
{
    private static partial TypeReference TypeReferenceFromInvocationTypeParameter(IInvocationOperation invocation, ITypeParameterSymbol typeParameter)
    {
        var argument = invocation.TargetMethod.TypeArguments[typeParameter.Ordinal];
        var typeArgumentSyntax = invocation.Syntax;

        // type arguments do not appear in the invocation, so search the code for them
        try
        {
            typeArgumentSyntax = invocation.Syntax.DescendantNodes()
                .First(n => n.IsKind(SyntaxKind.TypeArgumentList))
                .DescendantNodes().ElementAt(typeParameter.Ordinal);
        }
        catch
        {
            // ignore, this is just a nicety
        }

        return new TypeReference(argument, typeArgumentSyntax.GetLocation());
    }

    private static partial bool IsSimpleAssignmentNode(SyntaxNode node)
        => node.IsKind(SyntaxKind.SimpleAssignmentExpression);

    private static partial bool IsInvocationNode(SyntaxNode node)
        => node.IsKind(SyntaxKind.InvocationExpression);
}
