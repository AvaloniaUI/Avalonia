// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Markup.Parsers.CSharp.Grammar;
using Sprache;

namespace Perspex.Markup.Parsers.CSharp
{
    public class CSharpSyntaxTree
    {
        internal static ExpressionStatementSyntax ParseExpression(string expression)
        {
            return ExpressionGrammar.ExpressionStatement().Parse(expression);
        }
    }
}
