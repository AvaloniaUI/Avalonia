using System;
using Avalonia.Input.TextInput;
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

        // Logical line (scans to hard breaks), not the visual wrapped line.
        public static (int Start, int End) LineBounds(int offset, ReadOnlySpan<char> text)
        {
            var length = text.Length;
            if (length == 0)
            {
                return (0, 0);
            }

            var position = Math.Clamp(offset, 0, length - 1);

            var start = position;
            while (start > 0 && text[start - 1] != '\n' && text[start - 1] != '\r')
            {
                start--;
            }

            var end = position;
            while (end < length && text[end] != '\n' && text[end] != '\r')
            {
                end++;
            }

            return (start, end);
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
    }
}
