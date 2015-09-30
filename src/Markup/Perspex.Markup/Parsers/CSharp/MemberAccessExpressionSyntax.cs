// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Perspex.Markup.Parsers.CSharp
{
    internal class MemberAccessExpressionSyntax : ExpressionSyntax
    {
        public MemberAccessExpressionSyntax(ExpressionSyntax expression, IdentifierSyntax member)
        {
            Expression = expression;
            Member = member;
        }

        public ExpressionSyntax Expression { get; }
        public IdentifierSyntax Member { get; }
    }
}
