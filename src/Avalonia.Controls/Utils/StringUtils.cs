using System;
using Avalonia.Media.TextFormatting.Unicode;

namespace Avalonia.Controls.Utils
{
    internal static class StringUtils
    {
        public static bool IsEol(char c)
        {
            return c == '\r' || c == '\n';
        }

        public static bool IsStartOfWord(string text, int index)
        {
            if (string.IsNullOrEmpty(text) || index < 0 || index >= text.Length)
            {
                return false;
            }

            if (char.IsWhiteSpace(text[index]))
            {
                return false;
            }

            var lineStart = LineBegin(text, index);
            var lineEnd = LineEnd(text, index);
            var lineSpan = text.AsSpan(lineStart, lineEnd - lineStart);
            var relIndex = index - lineStart;

            var enumerator = new WordBreakEnumerator(lineSpan);
            while (enumerator.MoveNext(out var segment))
            {
                if (segment.Offset == relIndex)
                {
                    return !IsWhitespace(lineSpan.Slice(segment.Offset, segment.Length));
                }

                if (segment.Offset > relIndex)
                {
                    break;
                }
            }

            return false;
        }

        public static bool IsEndOfWord(string text, int index)
        {
            if (string.IsNullOrEmpty(text) || index >= text.Length)
            {
                return true;
            }

            if (index <= 0)
            {
                return false;
            }

            if (char.IsWhiteSpace(text[index]))
            {
                return true;
            }

            var lineStart = LineBegin(text, index);
            var lineEnd = LineEnd(text, index);
            var lineSpan = text.AsSpan(lineStart, lineEnd - lineStart);
            var relIndex = index - lineStart;

            var enumerator = new WordBreakEnumerator(lineSpan);
            while (enumerator.MoveNext(out var segment))
            {
                if (segment.Offset + segment.Length == relIndex)
                {
                    return !IsWhitespace(lineSpan.Slice(segment.Offset, segment.Length));
                }

                if (segment.Offset >= relIndex)
                {
                    break;
                }
            }

            return false;
        }

        public static int PreviousWord(string text, int cursor)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            cursor = Math.Clamp(cursor, 0, text.Length);

            var lf = LineBegin(text, cursor) - 1;
            int cr;

            if (lf > 0 && text[lf] == '\n' && text[lf - 1] == '\r')
            {
                cr = lf - 1;
            }
            else
            {
                cr = lf;
            }

            // If cursor is at the beginning of the line, return the end of the previous line
            if (cursor - 1 == lf)
            {
                return cr > 0 ? cr : 0;
            }

            var lineStart = lf + 1;
            var lineEnd = LineEnd(text, cursor);
            var lineSpan = text.AsSpan(lineStart, lineEnd - lineStart);
            var relCursor = cursor - lineStart;

            if (relCursor <= 0)
            {
                return lineStart;
            }

            var enumerator = new WordBreakEnumerator(lineSpan);

            int lastWordStart = -1;
            int prevWordStart = -1;
            bool lastWasWs = false;

            while (enumerator.MoveNext(out var segment))
            {
                var sStart = segment.Offset;
                var sEnd = segment.Offset + segment.Length;

                if (sStart >= relCursor)
                {
                    break;
                }

                if (IsWhitespace(lineSpan.Slice(sStart, segment.Length)))
                {
                    lastWasWs = true;
                }
                else
                {
                    prevWordStart = lastWordStart;
                    lastWordStart = sStart;
                    lastWasWs = false;
                }
            }

            if (lastWasWs)
            {
                // The position immediately before relCursor is in whitespace.
                // Skip the whitespace run and move to the start of the preceding word.
                return lineStart + (lastWordStart >= 0 ? lastWordStart : 0);
            }

            if (lastWordStart >= 0)
            {
                return lineStart + lastWordStart;
            }

            return lineStart;
        }

        public static int NextWord(string text, int cursor)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            var cr = LineEnd(text, cursor);

            if (cursor >= text.Length)
            {
                return cursor;
            }

            int lf;
            if (cr < text.Length && text[cr] == '\r' && cr + 1 < text.Length && text[cr + 1] == '\n')
            {
                lf = cr + 1;
            }
            else
            {
                lf = cr;
            }

            // If cursor is at the end of the line, return the starting offset of the next line
            if (cursor == cr || cursor == lf)
            {
                if (lf < text.Length)
                {
                    return lf + 1;
                }

                return cursor;
            }

            var lineStart = LineBegin(text, cursor);
            var lineSpan = text.AsSpan(lineStart, cr - lineStart);
            var relCursor = cursor - lineStart;

            var enumerator = new WordBreakEnumerator(lineSpan);

            while (enumerator.MoveNext(out var segment))
            {
                var sStart = segment.Offset;
                var sEnd = segment.Offset + segment.Length;

                if (sEnd <= relCursor)
                {
                    continue;
                }

                if (IsWhitespace(lineSpan.Slice(sStart, segment.Length)))
                {
                    // relCursor is in or at the start of whitespace.
                    // Skip whitespace and skip the following word.
                    while (enumerator.MoveNext(out var nextSeg))
                    {
                        if (!IsWhitespace(lineSpan.Slice(nextSeg.Offset, nextSeg.Length)))
                        {
                            return lineStart + nextSeg.Offset + nextSeg.Length;
                        }
                    }

                    return cr;
                }

                // relCursor is in or at the start of a non-whitespace segment
                return lineStart + sEnd;
            }

            return cr;
        }

        private static bool IsWhitespace(ReadOnlySpan<char> span)
        {
            for (int i = 0; i < span.Length; i++)
            {
                if (!char.IsWhiteSpace(span[i]))
                {
                    return false;
                }
            }

            return true;
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
