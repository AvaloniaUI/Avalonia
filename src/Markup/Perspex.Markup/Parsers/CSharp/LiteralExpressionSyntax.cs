// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Perspex.Markup.Parsers.CSharp
{
    internal class LiteralExpressionSyntax : ExpressionSyntax
    {
        public LiteralExpressionSyntax(object value, string valueText)
        {
            Value = value;
            ValueText = valueText;
        }

        public object Value { get; }
        public string ValueText { get; }
    }
}
