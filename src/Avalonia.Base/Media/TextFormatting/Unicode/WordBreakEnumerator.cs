using System;

namespace Avalonia.Media.TextFormatting.Unicode
{
    /// <summary>
    /// Enumerates Unicode word-boundary segments.
    /// </summary>
    public ref struct WordBreakEnumerator
    {
        private readonly ReadOnlySpan<char> _text;
        private int _offset;
        private int _codepointOffset;

        public WordBreakEnumerator(ReadOnlySpan<char> text)
        {
            _text = text;
            _offset = 0;
            _codepointOffset = 0;
        }

        /// <summary>
        /// Moves to the next <see cref="WordSegment"/>.
        /// </summary>
        /// <param name="segment">The current word-boundary segment.</param>
        /// <returns><see langword="true"/> if a segment was found; otherwise, <see langword="false"/>.</returns>
        public bool MoveNext(out WordSegment segment)
        {
            if (_offset >= _text.Length)
            {
                segment = default;

                return false;
            }

            var segmentStart = _offset;
            var segmentCodepointStart = _codepointOffset;
            var current = ReadForward(_offset);
            var currentEnd = current.End;
            var boundaryCodepoint = _codepointOffset + 1;

            while (currentEnd < _text.Length)
            {
                var next = ReadForward(currentEnd);

                if (IsBoundary(current, next))
                {
                    break;
                }

                current = next;
                currentEnd = current.End;
                boundaryCodepoint++;
            }

            segment = new WordSegment(
                segmentStart,
                currentEnd - segmentStart,
                segmentCodepointStart,
                boundaryCodepoint - segmentCodepointStart);

            _offset = currentEnd;
            _codepointOffset = boundaryCodepoint;

            return true;
        }

        private readonly bool IsBoundary(in WordBreakUnit current, in WordBreakUnit next)
        {
            // WB3, WB3a, WB3b, and WB3c are evaluated before WB4, so these
            // rules use the adjacent code points exactly as they appear in text.
            if (current.WordBreakClass == WordBreakClass.CarriageReturn &&
                next.WordBreakClass == WordBreakClass.LineFeed)
            {
                return false;
            }

            if (IsNewline(current) || IsNewline(next))
            {
                return true;
            }

            if (current.WordBreakClass == WordBreakClass.ZWJ &&
                next.Codepoint.GraphemeBreakClass == GraphemeBreakClass.ExtendedPictographic)
            {
                return false;
            }

            if (current.WordBreakClass == WordBreakClass.WSegSpace &&
                next.WordBreakClass == WordBreakClass.WSegSpace)
            {
                return false;
            }

            if (IsIgnored(next.WordBreakClass))
            {
                return false;
            }

            var left = GetEffectivePrevious(current);
            var right = next.WordBreakClass;

            if (IsAHLetter(left.WordBreakClass) && IsAHLetter(right))
            {
                return false;
            }

            if (IsAHLetter(left.WordBreakClass) &&
                IsMidLetterMidNumLetQ(right) &&
                TryGetNextSignificant(next.End, out var after) &&
                IsAHLetter(after.WordBreakClass))
            {
                return false;
            }

            if (IsAHLetter(right) &&
                IsMidLetterMidNumLetQ(left.WordBreakClass) &&
                TryGetPreviousSignificant(left.Start, out var before) &&
                IsAHLetter(before.WordBreakClass))
            {
                return false;
            }

            if (left.WordBreakClass == WordBreakClass.HebrewLetter &&
                right == WordBreakClass.SingleQuote)
            {
                return false;
            }

            if (left.WordBreakClass == WordBreakClass.HebrewLetter &&
                right == WordBreakClass.DoubleQuote &&
                TryGetNextSignificant(next.End, out after) &&
                after.WordBreakClass == WordBreakClass.HebrewLetter)
            {
                return false;
            }

            if (right == WordBreakClass.HebrewLetter &&
                left.WordBreakClass == WordBreakClass.DoubleQuote &&
                TryGetPreviousSignificant(left.Start, out before) &&
                before.WordBreakClass == WordBreakClass.HebrewLetter)
            {
                return false;
            }

            if (left.WordBreakClass == WordBreakClass.Numeric &&
                right == WordBreakClass.Numeric)
            {
                return false;
            }

            if (IsAHLetter(left.WordBreakClass) && right == WordBreakClass.Numeric)
            {
                return false;
            }

            if (left.WordBreakClass == WordBreakClass.Numeric && IsAHLetter(right))
            {
                return false;
            }

            if (right == WordBreakClass.Numeric &&
                IsMidNumMidNumLetQ(left.WordBreakClass) &&
                TryGetPreviousSignificant(left.Start, out before) &&
                before.WordBreakClass == WordBreakClass.Numeric)
            {
                return false;
            }

            if (left.WordBreakClass == WordBreakClass.Numeric &&
                IsMidNumMidNumLetQ(right) &&
                TryGetNextSignificant(next.End, out after) &&
                after.WordBreakClass == WordBreakClass.Numeric)
            {
                return false;
            }

            if (left.WordBreakClass == WordBreakClass.Katakana &&
                right == WordBreakClass.Katakana)
            {
                return false;
            }

            if (IsAHLetterNumericKatakanaExtendNumLet(left.WordBreakClass) &&
                right == WordBreakClass.ExtendNumLet)
            {
                return false;
            }

            if (left.WordBreakClass == WordBreakClass.ExtendNumLet &&
                IsAHLetterNumericKatakana(right))
            {
                return false;
            }

            if (left.WordBreakClass == WordBreakClass.RegionalIndicator &&
                right == WordBreakClass.RegionalIndicator &&
                (CountRegionalIndicatorsBefore(next.Start) & 1) == 1)
            {
                return false;
            }

            return true;
        }

        private readonly WordBreakUnit GetEffectivePrevious(in WordBreakUnit current)
        {
            if (!IsIgnored(current.WordBreakClass))
            {
                return current;
            }

            var scanEnd = current.Start;

            while (TryReadBackward(scanEnd, out var previous))
            {
                if (!IsIgnored(previous.WordBreakClass))
                {
                    // WB4 does not ignore format or extend code points across
                    // start-of-text or hard line breaks.
                    return IsNewline(previous) ? current : previous;
                }

                scanEnd = previous.Start;
            }

            return current;
        }

        private readonly int CountRegionalIndicatorsBefore(int end)
        {
            var count = 0;
            var scanEnd = end;

            while (TryGetPreviousSignificant(scanEnd, out var previous))
            {
                if (previous.WordBreakClass != WordBreakClass.RegionalIndicator)
                {
                    break;
                }

                count++;
                scanEnd = previous.Start;
            }

            return count;
        }

        private readonly bool TryGetPreviousSignificant(int end, out WordBreakUnit codepoint)
        {
            var scanEnd = end;

            while (TryReadBackward(scanEnd, out codepoint))
            {
                if (!IsIgnored(codepoint.WordBreakClass))
                {
                    return true;
                }

                scanEnd = codepoint.Start;
            }

            codepoint = default;

            return false;
        }

        private readonly bool TryGetNextSignificant(int start, out WordBreakUnit codepoint)
        {
            var scanStart = start;

            while (TryReadForward(scanStart, out codepoint))
            {
                if (!IsIgnored(codepoint.WordBreakClass))
                {
                    return true;
                }

                scanStart = codepoint.End;
            }

            codepoint = default;

            return false;
        }

        private readonly WordBreakUnit ReadForward(int start)
        {
            var codepoint = Codepoint.ReadAt(_text, start, out var count);

            return new WordBreakUnit(codepoint, start, start + count);
        }

        private readonly bool TryReadForward(int start, out WordBreakUnit codepoint)
        {
            if (start >= _text.Length)
            {
                codepoint = default;

                return false;
            }

            codepoint = ReadForward(start);

            return true;
        }

        private readonly bool TryReadBackward(int end, out WordBreakUnit codepoint)
        {
            if (end <= 0)
            {
                codepoint = default;

                return false;
            }

            var start = end - 1;

            if (start > 0 &&
                char.IsLowSurrogate(_text[start]) &&
                char.IsHighSurrogate(_text[start - 1]))
            {
                start--;
            }

            codepoint = ReadForward(start);

            return true;
        }

        private static bool IsAHLetter(WordBreakClass wordBreakClass)
        {
            return wordBreakClass is WordBreakClass.ALetter or WordBreakClass.HebrewLetter;
        }

        private static bool IsAHLetterNumericKatakana(WordBreakClass wordBreakClass)
        {
            return IsAHLetter(wordBreakClass) ||
                wordBreakClass is WordBreakClass.Numeric or WordBreakClass.Katakana;
        }

        private static bool IsAHLetterNumericKatakanaExtendNumLet(WordBreakClass wordBreakClass)
        {
            return IsAHLetterNumericKatakana(wordBreakClass) ||
                wordBreakClass == WordBreakClass.ExtendNumLet;
        }

        private static bool IsIgnored(WordBreakClass wordBreakClass)
        {
            return wordBreakClass is WordBreakClass.Extend or WordBreakClass.Format or WordBreakClass.ZWJ;
        }

        private static bool IsMidLetterMidNumLetQ(WordBreakClass wordBreakClass)
        {
            return wordBreakClass is WordBreakClass.MidLetter or WordBreakClass.MidNumLet or WordBreakClass.SingleQuote;
        }

        private static bool IsMidNumMidNumLetQ(WordBreakClass wordBreakClass)
        {
            return wordBreakClass is WordBreakClass.MidNum or WordBreakClass.MidNumLet or WordBreakClass.SingleQuote;
        }

        private static bool IsNewline(in WordBreakUnit codepoint)
        {
            return codepoint.WordBreakClass is WordBreakClass.CarriageReturn or WordBreakClass.LineFeed or WordBreakClass.Newline;
        }

        private readonly struct WordBreakUnit
        {
            public WordBreakUnit(Codepoint codepoint, int start, int end)
            {
                Codepoint = codepoint;
                WordBreakClass = codepoint.WordBreakClass;
                Start = start;
                End = end;
            }

            public Codepoint Codepoint { get; }

            public WordBreakClass WordBreakClass { get; }

            public int Start { get; }

            public int End { get; }
        }
    }
}
