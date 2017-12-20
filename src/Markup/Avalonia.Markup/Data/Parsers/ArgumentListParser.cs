// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Markup.Data.Parsers
{
    internal static class ArgumentListParser
    {
        public static IList<string> Parse(Reader r, char open, char close)
        {
            if (r.Peek == open)
            {
                var result = new List<string>();

                r.Take();

                while (!r.End)
                {
                    var builder = new StringBuilder();
                    while (!r.End && r.Peek != ',' && r.Peek != close && !char.IsWhiteSpace(r.Peek))
                    {
                        builder.Append(r.Take());
                    }
                    if (builder.Length == 0)
                    {
                        throw new ExpressionParseException(r.Position, "Expected indexer argument.");
                    }
                    result.Add(builder.ToString());

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

                throw new ExpressionParseException(r.Position, "Expected ']'.");
            }

            return null;
        }
    }
}
