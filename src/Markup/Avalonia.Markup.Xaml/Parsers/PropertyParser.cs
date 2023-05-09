using Avalonia.Data.Core;
using Avalonia.Utilities;

namespace Avalonia.Markup.Xaml.Parsers
{
    internal class PropertyParser
    {
        public static (string? ns, string? owner, string name) Parse(CharacterReader r)
        {
            if (r.End)
            {
                throw new ExpressionParseException(0, "Expected property name.");
            }

            var openParens = r.TakeIf('(');
            bool closeParens = false;
            string? ns = null;
            string? owner = null;
            string? name = null;

            do
            {
                var token = r.ParseIdentifier();

                if (token.IsEmpty)
                {
                    if (r.End)
                    {
                        break;
                    }
                    else
                    {
                        if (openParens && !r.End && (closeParens = r.TakeIf(')')))
                        {
                            break;
                        }
                        else if (openParens)
                        {
                            throw new ExpressionParseException(r.Position, $"Expected ')'.");
                        }

                        throw new ExpressionParseException(r.Position, $"Unexpected '{r.Peek}'.");
                    }
                }
                else if (!r.End && r.TakeIf(':'))
                {
                    ns = ns == null ?
                        token.ToString() :
                        throw new ExpressionParseException(r.Position, "Unexpected ':'.");
                }
                else if (!r.End && r.TakeIf('.'))
                {
                    owner = owner == null ?
                        token.ToString() :
                        throw new ExpressionParseException(r.Position, "Unexpected '.'.");
                }
                else
                {
                    name = token.ToString();
                }
            } while (!r.End);

            if (name == null)
            {
                throw new ExpressionParseException(0, "Expected property name.");
            }
            else if (openParens && owner == null)
            {
                throw new ExpressionParseException(1, "Expected property owner.");
            }
            else if (openParens && !closeParens)
            {
                throw new ExpressionParseException(r.Position, "Expected ')'.");
            }
            else if (!r.End)
            {
                throw new ExpressionParseException(r.Position, "Expected end of expression.");
            }

            return (ns, owner, name);
        }
    }
}
