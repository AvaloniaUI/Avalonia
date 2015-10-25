// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Markup.Data.Parsers;

namespace Perspex.Markup.Data
{
    internal static class ExpressionNodeBuilder
    {
        public static ExpressionNode Build(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                throw new ArgumentException("'expression' may not be empty.");
            }

            var reader = new Reader(expression);
            var node = ExpressionParser.Parse(reader);

            if (!reader.End)
            {
                throw new ExpressionParseException(reader, "Expected end of expression.");
            }

            return node;
        }
    }
}
