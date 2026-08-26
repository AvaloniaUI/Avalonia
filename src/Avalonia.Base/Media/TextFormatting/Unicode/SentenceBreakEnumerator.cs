using System;

namespace Avalonia.Media.TextFormatting.Unicode
{
    /// <summary>
    /// Enumerates Unicode sentence-boundary segments per UAX #29 rules SB1–SB11, SB998.
    /// </summary>
    /// <remarks>
    /// The enumerator is a <see langword="ref struct"/> operating on a <see cref="ReadOnlySpan{T}"/>
    /// so every operation is allocation-free. It reads codepoints with
    /// <see cref="Codepoint.ReadAt"/> and classifies them via the <see cref="SentenceBreakClass"/>
    /// property backed by the <c>SentenceBreak</c> trie.
    /// </remarks>
    public ref struct SentenceBreakEnumerator
    {
        private readonly ReadOnlySpan<char> _text;
        private int _offset;

        /// <summary>
        /// Initializes a new instance of the <see cref="SentenceBreakEnumerator"/> struct.
        /// </summary>
        /// <param name="text">The text to enumerate sentence segments over.</param>
        public SentenceBreakEnumerator(ReadOnlySpan<char> text)
        {
            _text = text;
            _offset = 0;
        }

        /// <summary>
        /// Moves to the next <see cref="SentenceSegment"/>.
        /// </summary>
        /// <param name="segment">The current sentence-boundary segment.</param>
        /// <returns><see langword="true"/> if a segment was found; otherwise, <see langword="false"/>.</returns>
        public bool MoveNext(out SentenceSegment segment)
        {
            if (_offset >= _text.Length)
            {
                segment = default;
                return false;
            }

            var segmentStart = _offset;
            var current = ReadForward(_offset);
            var currentEnd = current.End;

            while (currentEnd < _text.Length)
            {
                var next = ReadForward(currentEnd);

                if (IsBoundary(current, next))
                {
                    break;
                }

                current = next;
                currentEnd = current.End;
            }

            segment = new SentenceSegment(segmentStart, _text.Slice(segmentStart, currentEnd - segmentStart));
            _offset = currentEnd;

            return true;
        }

        // UAX-29 sentence boundary rules SB1–SB11 / SB998.
        // Rules are tested in order; the first matching rule wins.
        // SB1 (sot ÷) and SB2 (÷ eot) are implicit: the loop above starts at the
        // start of text and stops at text.Length.
        private readonly bool IsBoundary(in SentenceBreakUnit current, in SentenceBreakUnit next)
        {
            // SB3: CR × LF — no break between CR and LF.
            if (current.SentenceBreakClass == SentenceBreakClass.CarriageReturn &&
                next.SentenceBreakClass == SentenceBreakClass.LineFeed)
            {
                return false;
            }

            // SB4: Break after paragraph separators (Sep, CR, LF).
            if (IsSep(current.SentenceBreakClass))
            {
                return true;
            }

            // SB5: X (Extend | Format) × X  — Extend/Format do not break from the preceding char.
            // The right-side ignorable check ensures the main loop doesn't advance past them.
            // The left-side GetEffectivePrevious helper resolves the effective previous class.
            if (IsIgnored(next.SentenceBreakClass))
            {
                return false;
            }

            // Resolve the effective left class, skipping any trailing Extend/Format per SB5.
            var left = GetEffectivePrevious(current);
            var right = next.SentenceBreakClass;

            // SB6: ATerm × Numeric
            if (left.SentenceBreakClass == SentenceBreakClass.ATerm &&
                right == SentenceBreakClass.Numeric)
            {
                return false;
            }

            // SB7: (Upper | Lower) ATerm × Upper
            if (left.SentenceBreakClass == SentenceBreakClass.ATerm &&
                right == SentenceBreakClass.Upper &&
                TryGetPreviousSignificant(left.Start, out var beforeATerm) &&
                IsUpperOrLower(beforeATerm.SentenceBreakClass))
            {
                return false;
            }

            // SB8: ATerm Close* Sp* × (¬{OLetter|Upper|Sep|CR|LF|STerm|ATerm})* Lower
            // If the left context is ATerm Close* Sp*, and scanning right we find a Lower
            // (without hitting a blocker first), do not break.
            if (HasATermContext(current) && ScanForwardSb8(right, next.End))
            {
                return false;
            }

            // SB8a: (STerm | ATerm) Close* Sp* × (SContinue | STerm | ATerm)
            if (HasTerminatorContext(current, includeSpaces: true) &&
                (right == SentenceBreakClass.SContinue ||
                 right == SentenceBreakClass.STerm ||
                 right == SentenceBreakClass.ATerm))
            {
                return false;
            }

            // SB9: (STerm | ATerm) Close* × (Close | Sp | Sep | CR | LF)
            if (HasTerminatorContext(current, includeSpaces: false) &&
                (right == SentenceBreakClass.Close ||
                 right == SentenceBreakClass.Sp ||
                 IsSep(right)))
            {
                return false;
            }

            // SB10: (STerm | ATerm) Close* Sp* × (Sp | Sep | CR | LF)
            if (HasTerminatorContext(current, includeSpaces: true) &&
                (right == SentenceBreakClass.Sp || IsSep(right)))
            {
                return false;
            }

            // SB11: (STerm | ATerm) Close* Sp* (Sep | CR | LF)? ÷
            if (HasCompleteTerminatorContext(current))
            {
                return true;
            }

            // SB998: Otherwise, no break.
            return false;
        }

        // SB8 forward scan from the first right-side character.
        // Returns true iff we can reach a Lower without hitting a "blocker" class.
        // Blocker: OLetter | Upper | Sep | CR | LF | STerm | ATerm
        // (Note: Lower itself is the TARGET, not a blocker.)
        private readonly bool ScanForwardSb8(SentenceBreakClass firstRight, int restStart)
        {
            if (firstRight == SentenceBreakClass.Lower)
            {
                return true;
            }

            if (IsSb8Blocker(firstRight))
            {
                return false;
            }

            // firstRight is neither Lower nor a blocker: scan forward for Lower/blocker.
            var scanStart = restStart;

            while (TryReadForward(scanStart, out var ahead))
            {
                var cls = ahead.SentenceBreakClass;

                // Apply SB5: skip Extend/Format transparently.
                if (IsIgnored(cls))
                {
                    scanStart = ahead.End;
                    continue;
                }

                if (cls == SentenceBreakClass.Lower)
                {
                    return true;
                }

                if (IsSb8Blocker(cls))
                {
                    return false;
                }

                scanStart = ahead.End;
            }

            return false;
        }

        // Returns true if the left context (at and before `current`) ends with
        // (STerm | ATerm) Close* [Sp*]
        // when includeSpaces=true the Sp* is included; false means only Close*.
        private readonly bool HasTerminatorContext(in SentenceBreakUnit current, bool includeSpaces)
        {
            var ec = GetEffectivePrevious(current);
            var cls = ec.SentenceBreakClass;
            var scanEnd = ec.Start;

            // Skip Sp* (only when includeSpaces=true)
            if (includeSpaces)
            {
                while (cls == SentenceBreakClass.Sp)
                {
                    if (!TryGetPreviousSignificant(scanEnd, out var prev))
                    {
                        return false;
                    }

                    cls = prev.SentenceBreakClass;
                    scanEnd = prev.Start;
                }
            }

            // Skip Close*
            while (cls == SentenceBreakClass.Close)
            {
                if (!TryGetPreviousSignificant(scanEnd, out var prev))
                {
                    return false;
                }

                cls = prev.SentenceBreakClass;
                scanEnd = prev.Start;
            }

            return cls == SentenceBreakClass.ATerm || cls == SentenceBreakClass.STerm;
        }

        // Returns true if the left context matches:
        //   (STerm | ATerm) Close* Sp* (Sep | CR | LF)?
        // i.e., the complete context for SB11 which allows an optional trailing paragraph separator.
        private readonly bool HasCompleteTerminatorContext(in SentenceBreakUnit current)
        {
            var ec = GetEffectivePrevious(current);
            var cls = ec.SentenceBreakClass;
            var scanEnd = ec.Start;

            // Skip optional trailing Sep/CR/LF (at most one paragraph separator).
            if (IsSep(cls))
            {
                if (!TryGetPreviousSignificant(scanEnd, out var prev))
                {
                    return false;
                }

                cls = prev.SentenceBreakClass;
                scanEnd = prev.Start;
            }

            // Skip Sp*
            while (cls == SentenceBreakClass.Sp)
            {
                if (!TryGetPreviousSignificant(scanEnd, out var prev))
                {
                    return false;
                }

                cls = prev.SentenceBreakClass;
                scanEnd = prev.Start;
            }

            // Skip Close*
            while (cls == SentenceBreakClass.Close)
            {
                if (!TryGetPreviousSignificant(scanEnd, out var prev))
                {
                    return false;
                }

                cls = prev.SentenceBreakClass;
                scanEnd = prev.Start;
            }

            return cls == SentenceBreakClass.ATerm || cls == SentenceBreakClass.STerm;
        }

        // Returns true if the left context ends with ATerm Close* Sp*
        // (ATerm-specific version; STerm does not apply for SB8).
        private readonly bool HasATermContext(in SentenceBreakUnit current)
        {
            var ec = GetEffectivePrevious(current);
            var cls = ec.SentenceBreakClass;
            var scanEnd = ec.Start;

            // Skip Sp*
            while (cls == SentenceBreakClass.Sp)
            {
                if (!TryGetPreviousSignificant(scanEnd, out var prev))
                {
                    return false;
                }

                cls = prev.SentenceBreakClass;
                scanEnd = prev.Start;
            }

            // Skip Close*
            while (cls == SentenceBreakClass.Close)
            {
                if (!TryGetPreviousSignificant(scanEnd, out var prev))
                {
                    return false;
                }

                cls = prev.SentenceBreakClass;
                scanEnd = prev.Start;
            }

            return cls == SentenceBreakClass.ATerm;
        }

        // Follows the SB5 "ignore" rule: if `current` is ignored (Extend/Format),
        // scan backward to find the effective preceding non-ignored unit. If no
        // non-ignored unit is found, returns `current` unchanged (edge case:
        // leading ignored chars have no base to attach to).
        private readonly SentenceBreakUnit GetEffectivePrevious(in SentenceBreakUnit current)
        {
            if (!IsIgnored(current.SentenceBreakClass))
            {
                return current;
            }

            var scanEnd = current.Start;

            while (TryReadBackward(scanEnd, out var previous))
            {
                if (!IsIgnored(previous.SentenceBreakClass))
                {
                    // SB4: do not carry the ignored attachment across a paragraph separator.
                    return IsSep(previous.SentenceBreakClass) ? current : previous;
                }

                scanEnd = previous.Start;
            }

            return current;
        }

        private readonly bool TryGetPreviousSignificant(int end, out SentenceBreakUnit codepoint)
        {
            var scanEnd = end;

            while (TryReadBackward(scanEnd, out codepoint))
            {
                if (!IsIgnored(codepoint.SentenceBreakClass))
                {
                    return true;
                }

                scanEnd = codepoint.Start;
            }

            codepoint = default;

            return false;
        }

        private readonly SentenceBreakUnit ReadForward(int start)
        {
            var codepoint = Codepoint.ReadAt(_text, start, out var count);
            return new SentenceBreakUnit(codepoint, start, start + count);
        }

        private readonly bool TryReadForward(int start, out SentenceBreakUnit codepoint)
        {
            if (start >= _text.Length)
            {
                codepoint = default;
                return false;
            }

            codepoint = ReadForward(start);
            return true;
        }

        private readonly bool TryReadBackward(int end, out SentenceBreakUnit codepoint)
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

        // SB4: Sep | CR | LF are paragraph separators.
        private static bool IsSep(SentenceBreakClass cls)
        {
            return cls is SentenceBreakClass.Sep
                or SentenceBreakClass.CarriageReturn
                or SentenceBreakClass.LineFeed;
        }

        // SB5: Extend and Format are transparent (ignored) for sentence-boundary rules.
        private static bool IsIgnored(SentenceBreakClass cls)
        {
            return cls is SentenceBreakClass.Extend or SentenceBreakClass.Format;
        }

        private static bool IsUpperOrLower(SentenceBreakClass cls)
        {
            return cls is SentenceBreakClass.Upper or SentenceBreakClass.Lower;
        }

        // SB8 forward-scan blocker set: classes that terminate the lookahead without
        // matching Lower. Lower itself is the TARGET and is NOT included here.
        private static bool IsSb8Blocker(SentenceBreakClass cls)
        {
            return cls is SentenceBreakClass.OLetter
                or SentenceBreakClass.Upper
                or SentenceBreakClass.Sep
                or SentenceBreakClass.CarriageReturn
                or SentenceBreakClass.LineFeed
                or SentenceBreakClass.STerm
                or SentenceBreakClass.ATerm;
        }

        private readonly struct SentenceBreakUnit
        {
            public SentenceBreakUnit(Codepoint codepoint, int start, int end)
            {
                Codepoint = codepoint;
                SentenceBreakClass = codepoint.SentenceBreakClass;
                Start = start;
                End = end;
            }

            public Codepoint Codepoint { get; }

            public SentenceBreakClass SentenceBreakClass { get; }

            public int Start { get; }

            public int End { get; }
        }
    }
}
