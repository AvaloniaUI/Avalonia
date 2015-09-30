using System.Globalization;
using System.Linq;
using Sprache;

namespace Perspex.Markup.Parsers.CSharp.Grammar
{
    internal class IdentifierGrammar
    {
        private static readonly Parser<char> CombiningCharacter = Parse.Char(
            c =>
            {
                var cat = CharUnicodeInfo.GetUnicodeCategory(c);
                return cat == UnicodeCategory.NonSpacingMark ||
                       cat == UnicodeCategory.SpacingCombiningMark;
            },
            "Connecting Character");

        private static readonly Parser<char> ConnectingCharacter = Parse.Char(
            c => CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.ConnectorPunctuation,
            "Connecting Character");

        private static readonly Parser<char> FormattingCharacter = Parse.Char(
            c => CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.Format,
            "Connecting Character");

        private static readonly Parser<char> IdentifierStart = Parse.Letter.Or(Parse.Char('_'));

        private static readonly Parser<char> IdentifierChar = Parse
            .LetterOrDigit
            .Or(ConnectingCharacter)
            .Or(CombiningCharacter)
            .Or(FormattingCharacter);

        public static Parser<IdentifierSyntax> Identifier()
        {
            return from start in IdentifierStart.Once().Text()
                   from @char in IdentifierChar.Many().Text()
                   select new IdentifierSyntax(start + @char);
        }
    }
}
