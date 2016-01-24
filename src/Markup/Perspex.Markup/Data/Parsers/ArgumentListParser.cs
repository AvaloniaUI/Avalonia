// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Perspex.Markup.Data.Parsers
{
    internal static class ArgumentListParser
    {
        public static IList<object> Parse(Reader r, char open, char close)
        {
            if (r.Peek == open)
            {
                var result = new List<object>();

                r.Take();

                while (!r.End)
                {
                    var literal = LiteralParser.Parse(r);

                    if (literal != null)
                    {
                        result.Add(literal);
                    }
                    else
                    {
                        throw new ExpressionParseException(r.Position, "Expected integer.");
                    }

                    r.SkipWhitespace();

                    if (r.End)
                    {
                        throw new ExpressionParseException(r.Position, "Expected ','.");
                    }
                    else if (r.TakeIf(close))
                    {
                        return result;
                    }
                    else
                    {
                        if (r.Take() != ',')
                        {
                            throw new ExpressionParseException(r.Position, "Expected ','.");
                        }

                        r.SkipWhitespace();
                    }
                }

                if (!r.End)
                {
                    r.Take();
                    return result;
                }
                else
                {
                    throw new ExpressionParseException(r.Position, "Expected ']'.");
                }
            }

            return null;
        }
    }
}
