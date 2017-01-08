// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Globalization;

namespace Avalonia.Controls.Utils
{
    internal static class StringUtils
    {
        private enum CharClass
        {
            CharClassUnknown,
            CharClassWhitespace,
            CharClassAlphaNumeric,
        }

        public static bool IsEol(char c)
        {
            return c == '\r' || c == '\n';
        }

        public static bool IsStartOfWord(string text, int index)
        {
            if (index >= text.Length)
            {
                return false;
            }

            // A 'word' starts with an AlphaNumeric or some punctuation symbols immediately
            // preceeded by lwsp.
            if (index > 0 && !char.IsWhiteSpace(text[index - 1]))
            {
                return false;
            }

            switch (CharUnicodeInfo.GetUnicodeCategory(text[index]))
            {
                case UnicodeCategory.LowercaseLetter:
                case UnicodeCategory.TitlecaseLetter:
                case UnicodeCategory.UppercaseLetter:
                case UnicodeCategory.DecimalDigitNumber:
                case UnicodeCategory.LetterNumber:
                case UnicodeCategory.OtherNumber:
                case UnicodeCategory.DashPunctuation:
                case UnicodeCategory.InitialQuotePunctuation:
                case UnicodeCategory.OpenPunctuation:
                case UnicodeCategory.CurrencySymbol:
                case UnicodeCategory.MathSymbol:
                    return true;

                // TODO: How do you do this in .NET?
                // case UnicodeCategory.OtherPunctuation:
                //    // words cannot start with '.', but they can start with '&' or '*' (for example)
                //    return g_unichar_break_type(buffer->text[index]) == G_UNICODE_BREAK_ALPHABETIC;
                default:
                    return false;
            }
        }

        public static int PreviousWord(string text, int cursor)
        {
            int begin;
            int i;
            int cr;
            int lf;

            lf = LineBegin(text, cursor) - 1;

            if (lf > 0 && text[lf] == '\n' && text[lf - 1] == '\r')
            {
                cr = lf - 1;
            }
            else
            {
                cr = lf;
            }

            // if the cursor is at the beginning of the line, return the end of the prev line
            if (cursor - 1 == lf)
            {
                return (cr > 0) ? cr : 0;
            }

            CharClass cc = GetCharClass(text[cursor - 1]);
            begin = lf + 1;
            i = cursor;

            // skip over the word, punctuation, or run of whitespace
            while (i > begin && GetCharClass(text[i - 1]) == cc)
            {
                i--;
            }

            // if the cursor was at whitespace, skip back a word too
            if (cc == CharClass.CharClassWhitespace && i > begin)
            {
                cc = GetCharClass(text[i - 1]);
                while (i > begin && GetCharClass(text[i - 1]) == cc)
                {
                    i--;
                }
            }

            return i;
        }

        public static int NextWord(string text, int cursor)
        {
            int i, lf, cr;

            cr = LineEnd(text, cursor);

            if (cr < text.Length && text[cr] == '\r' && text[cr + 1] == '\n')
            {
                lf = cr + 1;
            }
            else
            {
                lf = cr;
            }

            // if the cursor is at the end of the line, return the starting offset of the next line
            if (cursor == cr || cursor == lf)
            {
                if (lf < text.Length)
                {
                    return lf + 1;
                }

                return cursor;
            }

            CharClass cc = GetCharClass(text[cursor]);
            i = cursor;

            // skip over the word, punctuation, or run of whitespace
            while (i < cr && GetCharClass(text[i]) == cc)
            {
                i++;
            }

            // skip any whitespace after the word/punct
            while (i < cr && char.IsWhiteSpace(text[i]))
            {
                i++;
            }

            return i;
        }

        private static CharClass GetCharClass(char c)
        {
            if (char.IsWhiteSpace(c))
            {
                return CharClass.CharClassWhitespace;
            }
            else if (char.IsLetterOrDigit(c))
            {
                return CharClass.CharClassAlphaNumeric;
            }
            else
            {
                return CharClass.CharClassUnknown;
            }
        }

        private static int LineBegin(string text, int pos)
        {
            while (pos > 0 && !IsEol(text[pos - 1]))
            {
                pos--;
            }

            return pos;
        }

        private static int LineEnd(string text, int cursor, bool include = false)
        {
            while (cursor < text.Length && !IsEol(text[cursor]))
            {
                cursor++;
            }

            if (include && cursor < text.Length)
            {
                if (text[cursor] == '\r' && text[cursor + 1] == '\n')
                {
                    cursor += 2;
                }
                else
                {
                    cursor++;
                }
            }

            return cursor;
        }
    }
}
