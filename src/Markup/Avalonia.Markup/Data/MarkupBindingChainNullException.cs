using System.Collections.Generic;
using System.Linq;
using Avalonia.Data;

namespace Avalonia.Markup.Data
{
    internal class MarkupBindingChainNullException : BindingChainNullException
    {
        private IList<string> _nodes = new List<string>();

        public MarkupBindingChainNullException()
        {
        }

        public MarkupBindingChainNullException(string expression, string expressionNullPoint)
            : base(expression, expressionNullPoint)
        {
            _nodes = null;
        }

        public bool HasNodes => _nodes.Count > 0;
        public void AddNode(string node) => _nodes.Add(node);

        public void Commit(string expression)
        {
            Expression = expression;
            ExpressionNullPoint = string.Join(".", _nodes.Reverse())
                .Replace(".!", "!")
                .Replace(".[", "[");
            _nodes = null;
        }
    }
}
