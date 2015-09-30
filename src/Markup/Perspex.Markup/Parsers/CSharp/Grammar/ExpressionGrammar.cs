// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Sprache;

namespace Perspex.Markup.Parsers.CSharp.Grammar
{
    internal class ExpressionGrammar
    {
        public static Parser<ExpressionStatementSyntax> ExpressionStatement()
        {
            return from expression in Expression().End()
                   select new ExpressionStatementSyntax(expression);
        }

        public static Parser<ExpressionSyntax> Expression()
        {
            return LiteralGrammar.Literal()
                .Or<ExpressionSyntax>(PrefixUnary())
                .Or<ExpressionSyntax>(MemberAccess())
                .Or<ExpressionSyntax>(ElementAccess())
                .Or<ExpressionSyntax>(IdentifierGrammar.Identifier());
        }

        public static Parser<MemberAccessExpressionSyntax> MemberAccess()
        {
            return from identifier in IdentifierGrammar.Identifier()
                   from dot in Parse.Char('.')
                   from expression in Expression()
                   select new MemberAccessExpressionSyntax(expression, identifier);
        }

        public static Parser<ElementAccessExpressionSyntax> ElementAccess()
        {
            return from expression in IdentifierGrammar.Identifier()
                   from arguments in BracketedArgumentList('[', ']')
                   select new ElementAccessExpressionSyntax(expression, arguments);
        }

        public static Parser<BracketedArgumentListSyntax> BracketedArgumentList(
            char openBracket, 
            char closeBracket)
        {
            return from open in Parse.Char(openBracket)
                   from arguments in Arguments()
                   from close in Parse.Char(closeBracket)
                   select new BracketedArgumentListSyntax(arguments);
        }

        public static Parser<IEnumerable<ExpressionSyntax>> Arguments()
        {
            return Expression().DelimitedBy(Parse.Char(',').Token());
        }

        public static Parser<PrefixUnaryExpressionSyntax> PrefixUnary()
        {
            return from bang in Parse.Char('!')
                   from operand in Expression()
                   select new PrefixUnaryExpressionSyntax(operand, SyntaxKind.LogicalNotExpression);
        }
    }
}
