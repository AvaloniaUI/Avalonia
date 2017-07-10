using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Data;

namespace Avalonia.Markup.Data
{
    internal class MarkupBindingChainException : BindingChainException
    {
        private IList<string> _nodes = new List<string>();

        public MarkupBindingChainException(string message)
            : base(message)
        {
        }

        public MarkupBindingChainException(string message, string node)
            : base(message)
        {
            AddNode(node);
        }

        public MarkupBindingChainException(string message, string expression, string expressionNullPoint)
            : base(message, expression, expressionNullPoint)
        {
            _nodes = null;
        }

        public bool HasNodes => _nodes?.Count > 0;
        public void AddNode(string node) => _nodes.Add(node);

        public void Commit(string expression)
        {
            Expression = expression;
            ExpressionErrorPoint = _nodes != null ?
                string.Join(".", _nodes.Reverse())
                    .Replace(".!", "!")
                    .Replace(".[", "[")
                    .Replace(".^", "^") :
                string.Empty;
            _nodes = null;
        }
    }
}
