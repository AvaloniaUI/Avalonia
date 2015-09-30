using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Perspex.Markup.Binding
{
    public class ExpressionNodeBuilder
    {
        public static IList<ExpressionNode> Build(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                throw new ArgumentException("'expression' may not be empty.");
            }

            var syntaxTree = CSharpSyntaxTree.ParseText(expression, new CSharpParseOptions(kind: SourceCodeKind.Interactive));
            var syntaxRoot = syntaxTree.GetRoot();
            var syntax = syntaxRoot.ChildNodes().SingleOrDefault()?.ChildNodes()?.SingleOrDefault() as ExpressionStatementSyntax;

            if (syntax != null)
            {
                var result = new List<ExpressionNode>();

                foreach (SyntaxNode node in syntax.ChildNodes())
                {
                    var identifier = node as IdentifierNameSyntax;
                    var memberAccess = node as MemberAccessExpressionSyntax;

                    if (identifier != null)
                    {
                        result.Add(new PropertyAccessorNode(identifier.Identifier.ValueText));
                    }
                    else if (memberAccess != null)
                    {
                        Build(memberAccess, result);
                    }
                }

                for (int i = 0; i < result.Count - 1; ++i)
                {
                    result[i].Next = result[i + 1];
                }

                return result;
            }
            else
            {
                throw new Exception($"Invalid expression: {expression}");
            }
        }

        private static void Build(MemberAccessExpressionSyntax syntax, IList<ExpressionNode> result)
        {
            foreach (SyntaxNode node in syntax.ChildNodes())
            {
                var identifier = node as IdentifierNameSyntax;
                var memberAccess = node as MemberAccessExpressionSyntax;

                if (identifier != null)
                {
                    result.Add(new PropertyAccessorNode(identifier.Identifier.ValueText));
                }
                else if (memberAccess != null)
                {
                    Build(memberAccess, result);
                }
            }
        }
    }
}
