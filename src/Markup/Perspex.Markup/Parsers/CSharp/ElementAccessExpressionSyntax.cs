// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Perspex.Markup.Parsers.CSharp
{
    internal class ElementAccessExpressionSyntax : ExpressionSyntax
    {
        public ElementAccessExpressionSyntax(
            ExpressionSyntax expression, 
            BracketedArgumentListSyntax argumentList)
        {
            Expression = expression;
            ArgumentList = argumentList;
        }

        public ExpressionSyntax Expression { get; }
        public BracketedArgumentListSyntax ArgumentList { get; }
    }
}
