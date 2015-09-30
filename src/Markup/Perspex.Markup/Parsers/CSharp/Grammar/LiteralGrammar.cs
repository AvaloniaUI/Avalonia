using System.Globalization;
using System.Linq;
using Sprache;

namespace Perspex.Markup.Parsers.CSharp.Grammar
{
    internal class LiteralGrammar
    {
        public static Parser<LiteralExpressionSyntax> Literal()
        {
            return Integer();
        }

        public static Parser<LiteralExpressionSyntax> Integer()
        {
            return from number in Parse.Number
                   select new LiteralExpressionSyntax(int.Parse(number), number);
        }
    }
}
