// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Perspex.Markup.Parsers.CSharp
{
    internal class PrefixUnaryExpressionSyntax : ExpressionSyntax
    {
        private SyntaxKind _kind;

        public PrefixUnaryExpressionSyntax(ExpressionSyntax operand, SyntaxKind kind)
        {
            Operand = operand;
            _kind = kind;
        }

        public ExpressionSyntax Operand { get; }

        public SyntaxKind Kind()
        {
            return _kind;
        }
    }
}
