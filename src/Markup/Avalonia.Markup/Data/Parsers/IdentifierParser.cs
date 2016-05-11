// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Globalization;
using System.Text;

namespace Avalonia.Markup.Data.Parsers
{
    internal static class IdentifierParser
    {
        public static string Parse(Reader r)
        {
            if (IsValidIdentifierStart(r.Peek))
            {
                var result = new StringBuilder();

                while (!r.End && IsValidIdentifierChar(r.Peek))
                {
                    result.Append(r.Take());
                }

                return result.ToString();
            }
            else
            {
                return null;
            }
        }

        private static bool IsValidIdentifierStart(char c)
        {
            return char.IsLetter(c) || c == '_';
        }

        private static bool IsValidIdentifierChar(char c)
        {
            if (IsValidIdentifierStart(c))
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
