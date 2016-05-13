// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Markup.Data.Parsers;

namespace Avalonia.Markup.Data
{
    internal static class ExpressionNodeBuilder
    {
        public static ExpressionNode Build(string expression, bool enableValidation = false)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                throw new ArgumentException("'expression' may not be empty.");
            }

            var reader = new Reader(expression);
            var parser = new ExpressionParser(enableValidation);
            var node = parser.Parse(reader);

            if (!reader.End)
            {
                throw new ExpressionParseException(reader.Position, "Expected end of expression.");
            }

            return node;
        }
    }
}
