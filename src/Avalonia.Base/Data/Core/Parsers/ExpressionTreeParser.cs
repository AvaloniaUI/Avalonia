using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

namespace Avalonia.Data.Core.Parsers
{
    static class ExpressionTreeParser
    {
        [RequiresUnreferencedCode(TrimmingMessages.ExpressionNodeRequiresUnreferencedCodeMessage)]
        public static ExpressionNode Parse(Expression expr, bool enableDataValidation)
        {
            var visitor = new ExpressionVisitorNodeBuilder(enableDataValidation);

            visitor.Visit(expr);

            var nodes = visitor.Nodes;

            for (int n = 0; n < nodes.Count - 1; ++n)
            {
                nodes[n].Next = nodes[n + 1];
            }

            return nodes.FirstOrDefault() ?? new EmptyExpressionNode();
        }
    }
}
