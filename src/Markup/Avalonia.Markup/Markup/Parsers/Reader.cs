// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;
using System.Text;

namespace Avalonia.Markup.Parsers
{
    internal class Reader
    {
        private readonly string _s;
        private int _i;

        public Reader(string s)
        {
            _s = s;
        }

        public bool End => _i == _s.Length;
        public char Peek => _s[_i];
        public int Position => _i;
        public char Take() => _s[_i++];

        public void SkipWhitespace()
        {
            while (!End && char.IsWhiteSpace(Peek))
            {
                Take();
            }
        }

        public bool TakeIf(char c)
        {
            if (Peek == c)
            {
                Take();
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool TakeIf(Func<char, bool> condition)
        {
            if (condition(Peek))
            {
                Take();
                return true;
            }
            return false;
        }

        public ReadOnlySpan<char> TakeUntil(char c)
        {
            int startIndex = Position;
            while (!End && Peek != c)
            {
                Take();
            }
            return _s.AsSpan(startIndex, Position - startIndex);
        }

        public ReadOnlySpan<char> ParseIdentifier()
        {
            if (IsValidIdentifierStart(Peek))
            {
                int startIndex = Position;

                while (!End && IsValidIdentifierChar(Peek))
                {
                    Take();
                }

                return _s.AsSpan(startIndex, Position - startIndex);
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
