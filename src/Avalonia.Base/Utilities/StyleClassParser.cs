using System;
using System.Globalization;

namespace Avalonia.Utilities
{
#if !BUILDTASK
    public
#endif
    static class StyleClassParser
    {
        public static ReadOnlySpan<char> ParseStyleClass(this ref CharacterReader r)
        {
            if (IsValidIdentifierStart(r.Peek))
            {
                return r.TakeWhile(c => IsValidIdentifierChar(c));
            }
            else
            {
                return ReadOnlySpan<char>.Empty;
            }
        }

        private static bool IsValidIdentifierStart(char c)
        {
            return char.IsLetter(c) || c == '_';
        }

        private static bool IsValidIdentifierChar(char c)
        {
            if (IsValidIdentifierStart(c) || c == '-')
            {
                return true;
            }
            else
            {
                var cat = CharUnicodeInfo.GetUnicodeCategory(c);
                return cat == UnicodeCategory.NonSpacingMark ||
                       cat == UnicodeCategory.SpacingCombiningMark ||
                       cat == UnicodeCategory.ConnectorPunctuation ||
                       cat == UnicodeCategory.Format ||
                       cat == UnicodeCategory.DecimalDigitNumber;
            }
        }
    }
}
