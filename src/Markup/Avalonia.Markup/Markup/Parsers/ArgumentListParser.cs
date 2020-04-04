using System.Collections.Generic;
using Avalonia.Data.Core;
using Avalonia.Utilities;

namespace Avalonia.Markup.Parsers
{
    internal static class ArgumentListParser
    {
        public static IList<string> ParseArguments(this ref CharacterReader r, char open, char close, char delimiter = ',')
        {
            if (r.Peek == open)
            {
                var result = new List<string>();

                r.Take();

                while (!r.End)
                {
                    var argument = r.TakeWhile(c => c != delimiter && c != close && !char.IsWhiteSpace(c));
                    if (argument.IsEmpty)
                    {
                        throw new ExpressionParseException(r.Position, "Expected indexer argument.");
                    }

                    result.Add(argument.ToString());

                    r.SkipWhitespace();

                    if (r.End)
                    {
                        throw new ExpressionParseException(r.Position, $"Expected '{delimiter}'.");
                    }
                    else if (r.TakeIf(close))
                    {
                        return result;
                    }
                    else
                    {
                        if (r.Take() != delimiter)
                        {
                            throw new ExpressionParseException(r.Position, $"Expected '{delimiter}'.");
                        }

                        r.SkipWhitespace();
                    }
                }

                throw new ExpressionParseException(r.Position, $"Expected '{close}'.");
            }

            throw new ExpressionParseException(r.Position, $"Expected '{open}'.");
        }
    }
}
