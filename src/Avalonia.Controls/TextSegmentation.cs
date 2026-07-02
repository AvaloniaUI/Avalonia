using System;
using Avalonia.Input.TextInput;
using Avalonia.Media.TextFormatting;
using Avalonia.Media.TextFormatting.Unicode;

namespace Avalonia.Controls
{
    /// <summary>
    /// Pure UTF-16 text segmentation shared by the TextBox IME navigation and the automation text
    /// navigation: grapheme-cluster (UAX-29) character stepping, UAX-29 word boundaries, heuristic
    /// sentence and logical-line bounds, move-by-unit, and contiguous-change diffing. Everything
    /// operates on a <see cref="ReadOnlySpan{T}"/> and code-unit offsets so both navigators use one
    /// implementation; a <see cref="string"/> caller converts implicitly.
    /// </summary>
    internal static class TextSegmentation
    {
        public static int NextGrapheme(int offset, ReadOnlySpan<char> text)
        {
            if (offset >= text.Length)
            {
                return text.Length;
            }

            var enumerator = new GraphemeEnumerator(text.Slice(offset));
            return enumerator.MoveNext(out var grapheme) ? offset + grapheme.Length : text.Length;
        }

        public static int PreviousGrapheme(int offset, ReadOnlySpan<char> text)
        {
            if (offset <= 0)
            {
                return 0;
            }

            var enumerator = new GraphemeEnumerator(text.Slice(0, offset));
            var start = 0;
            while (enumerator.MoveNext(out var grapheme))
            {
                start = grapheme.Offset;
            }

            return start;
        }

        public static int SnapToValid(int offset, ReadOnlySpan<char> text, bool forward)
        {
            if (offset > 0 && offset < text.Length &&
                char.IsLowSurrogate(text[offset]) && char.IsHighSurrogate(text[offset - 1]))
            {
                return forward ? offset + 1 : offset - 1;
            }

            return offset;
        }

        /// <summary>
        /// The bounds of the <paramref name="unit"/> containing <paramref name="offset"/>, with the
        /// boundary tie broken by <paramref name="gravity"/>: a backward-gravity position exactly at a
        /// unit start resolves to the preceding unit. Handles Character, Word, Sentence, Line and
        /// Paragraph (Line and Paragraph both map to the logical line here).
        /// </summary>
        public static (int Start, int End) UnitBounds(int offset, TextUnit unit, LogicalDirection gravity, ReadOnlySpan<char> text)
        {
            var bounds = UnitBoundsCore(offset, unit, text);

            if (gravity == LogicalDirection.Backward && offset > 0 && bounds.Start == offset)
            {
                bounds = unit == TextUnit.Character
                    ? (PreviousGrapheme(offset, text), offset)
                    : UnitBoundsCore(offset - 1, unit, text);
            }

            return bounds;
        }

        private static (int Start, int End) UnitBoundsCore(int offset, TextUnit unit, ReadOnlySpan<char> text)
            => unit switch
            {
                TextUnit.Character => CharacterBounds(offset, text),
                TextUnit.Word => WordBounds(offset, text),
                TextUnit.Sentence => SentenceBounds(offset, text),
                _ => LineBounds(offset, text),
            };

        public static (int Start, int End) CharacterBounds(int offset, ReadOnlySpan<char> text)
        {
            if (text.Length == 0)
            {
                return (0, 0);
            }

            // At the document end the only enclosing character is the last grapheme; clamping to
            // length - 1 could land inside a surrogate pair or cluster.
            if (offset >= text.Length)
            {
                return (PreviousGrapheme(text.Length, text), text.Length);
            }

            var start = Math.Max(0, offset);
            return (start, NextGrapheme(start, text));
        }

        // UAX-29 word segmentation; enumerates from the document start, so O(offset).
        public static (int Start, int End) WordBounds(int offset, ReadOnlySpan<char> text)
        {
            if (text.Length == 0)
            {
                return (0, 0);
            }

            var clamped = Math.Clamp(offset, 0, text.Length);
            var enumerator = new WordBreakEnumerator(text);
            while (enumerator.MoveNext(out var segment))
            {
                var end = segment.Offset + segment.Length;
                if (clamped < end)
                {
                    return (segment.Offset, end);
                }
            }

            return (text.Length, text.Length);
        }

        public static int WordBoundary(int offset, bool forward, ReadOnlySpan<char> text)
        {
            if (forward)
            {
                if (offset >= text.Length)
                {
                    return text.Length;
                }

                var enumerator = new WordBreakEnumerator(text);
                while (enumerator.MoveNext(out var segment))
                {
                    var end = segment.Offset + segment.Length;
                    if (end > offset)
                    {
                        return end;
                    }
                }

                return text.Length;
            }

            if (offset <= 0)
            {
                return 0;
            }

            var previous = 0;
            var backward = new WordBreakEnumerator(text);
            while (backward.MoveNext(out var segment))
            {
                var end = segment.Offset + segment.Length;
                if (end >= offset)
                {
                    break;
                }

                previous = end;
            }

            return previous;
        }

        // Logical line: bounded by UAX-14 mandatory breaks via LineBreakEnumerator (so CRLF is one
        // break), with the terminator trimmed from the content but trailing spaces kept. This is not
        // the visual wrapped line, which needs layout. O(offset), matching the UAX-29 word path.
        public static (int Start, int End) LineBounds(int offset, ReadOnlySpan<char> text)
        {
            var length = text.Length;
            if (length == 0)
            {
                return (0, 0);
            }

            offset = Math.Clamp(offset, 0, length - 1);

            var start = 0;
            var enumerator = new LineBreakEnumerator(text);

            while (enumerator.MoveNext(out var lineBreak))
            {
                if (!lineBreak.Required)
                {
                    continue;
                }

                // PositionWrap is the start of the next line, past the break sequence.
                if (lineBreak.PositionWrap <= offset)
                {
                    start = lineBreak.PositionWrap;
                    continue;
                }

                // First hard break after the offset: end the content before the terminator
                // characters, but keep any trailing spaces that precede them.
                var end = lineBreak.PositionWrap;
                while (end > start && IsMandatoryBreak(text[end - 1]))
                {
                    end--;
                }

                return (start, end);
            }

            return (start, length);
        }

        // Heuristic sentence bounds; there is no UAX-29 sentence segmenter in the tree.
        public static (int Start, int End) SentenceBounds(int offset, ReadOnlySpan<char> text)
        {
            var length = text.Length;
            if (length == 0)
            {
                return (0, 0);
            }

            var position = Math.Clamp(offset, 0, length - 1);

            var start = position;
            while (start > 0 && !IsSentenceBoundary(text[start - 1]))
            {
                start--;
            }

            var end = position;
            while (end < length && !IsSentenceBoundary(text[end]))
            {
                end++;
            }

            if (end < length)
            {
                end++;
            }

            return (start, end);
        }

        public static int MoveByUnit(int offset, TextUnit unit, bool forward, ReadOnlySpan<char> text)
        {
            switch (unit)
            {
                case TextUnit.Document:
                case TextUnit.Page:
                case TextUnit.Format:
                    return forward ? text.Length : 0;

                case TextUnit.Character:
                    return forward ? NextGrapheme(offset, text) : PreviousGrapheme(offset, text);

                case TextUnit.Word:
                    return WordBoundary(offset, forward, text);

                default:
                    var (start, end) = unit == TextUnit.Sentence
                        ? SentenceBounds(offset, text)
                        : LineBounds(offset, text);

                    if (forward)
                    {
                        return end > offset ? end : NextGrapheme(offset, text);
                    }

                    return start < offset ? start : PreviousGrapheme(offset, text);
            }
        }

        // The single contiguous change between two strings (common prefix/suffix).
        public static (int Offset, int OldLength, int NewLength) ComputeChange(ReadOnlySpan<char> oldText, ReadOnlySpan<char> newText)
        {
            var min = Math.Min(oldText.Length, newText.Length);

            var prefix = 0;
            while (prefix < min && oldText[prefix] == newText[prefix])
            {
                prefix++;
            }

            var suffix = 0;
            while (suffix < min - prefix &&
                   oldText[oldText.Length - 1 - suffix] == newText[newText.Length - 1 - suffix])
            {
                suffix++;
            }

            return (prefix, oldText.Length - prefix - suffix, newText.Length - prefix - suffix);
        }

        private static bool IsSentenceBoundary(char c) => c is '.' or '!' or '?' or '\n' or '\r';

        // A UAX-14 mandatory (hard) line break character: LF, CR, NEL, and the BK class - which covers
        // VT, FF, U+2028 LINE SEPARATOR and U+2029 PARAGRAPH SEPARATOR. Used only to trim the terminator
        // from a line's content; LineBreakEnumerator decides where lines break. All are BMP, so one
        // UTF-16 unit suffices.
        private static bool IsMandatoryBreak(char c) => new Codepoint(c).LineBreakClass switch
        {
            LineBreakClass.MandatoryBreak => true,
            LineBreakClass.LineFeed => true,
            LineBreakClass.CarriageReturn => true,
            LineBreakClass.NextLine => true,
            _ => false,
        };
    }
}
