// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;

namespace Avalonia.Utilities
{
#if !BUILDTASK
    public
#endif
    static class IdentifierParser
    {
        public static ReadOnlySpan<char> ParseIdentifier(this ref CharacterReader r)
        {
            if (IsValidIdentifierStart(r.Peek))
            {
                return r.TakeWhile(IsValidIdentifierChar);
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
