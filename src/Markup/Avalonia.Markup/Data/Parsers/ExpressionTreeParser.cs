using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Avalonia.Markup.Data.Parsers
{
    class ExpressionTreeParser
    {
        private readonly bool enableDataValidation;

        public ExpressionTreeParser(bool enableDataValidation)
        {
            this.enableDataValidation = enableDataValidation;
        }

        public ExpressionNode Parse(Expression expr)
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
