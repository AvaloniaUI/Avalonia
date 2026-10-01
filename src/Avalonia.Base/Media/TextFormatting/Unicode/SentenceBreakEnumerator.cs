using System;
using System.Runtime.CompilerServices;

namespace Avalonia.Media.TextFormatting.Unicode
{
    /// <summary>
    /// Enumerates Unicode sentence-boundary segments per UAX #29 rules SB1–SB11, SB998.
    /// </summary>
    /// <remarks>
    /// The enumerator is a <see langword="ref struct"/> operating on a <see cref="ReadOnlySpan{T}"/>
    /// so every operation is allocation-free. It reads codepoints with
    /// <see cref="Codepoint.ReadAt"/> and classifies them via the <see cref="SentenceBreakClass"/>
    /// property backed by the <c>SentenceBreak</c> trie. Each codepoint is visited a constant
    /// number of times: the left-hand context every rule needs is folded into a few fields as the
    /// walk advances, and the one rule that looks ahead reuses the scan it already made.
    /// </remarks>
    public ref struct SentenceBreakEnumerator
    {
        private readonly ReadOnlySpan<char> _text;
        private int _offset;

        // The left-hand context, updated one codepoint at a time as the walk advances.
        // _significant and _priorSignificant hold the last two classes that are not
        // Extend/Format, _ignorable the class of the last codepoint when that one is.
        private SentenceBreakClass _significant;
        private SentenceBreakClass _priorSignificant;
        private SentenceBreakClass _ignorable;
        private bool _hasSignificant;
        private bool _hasPriorSignificant;
        private bool _lastIsIgnorable;

        // How far the text to the left matches the (STerm | ATerm) Close* Sp* (Sep | CR | LF)?
        // grammar shared by SB8 through SB11, and which terminator opened it.
        private SentenceBreakClass _terminator;
        private TerminatorStage _terminatorStage;

        // The SB8 lookahead, memoized. A scan that ran from _lookaheadStart and stopped at
        // _lookaheadEnd gives the same answer for every start position in between, so the
        // Close* Sp* run following an ATerm is scanned once rather than once per position.
        private int _lookaheadStart;
        private int _lookaheadEnd;
        private bool _lookaheadResult;

        /// <summary>
        /// Initializes a new instance of the <see cref="SentenceBreakEnumerator"/> struct.
        /// </summary>
        /// <param name="text">The text to enumerate sentence segments over.</param>
        public SentenceBreakEnumerator(ReadOnlySpan<char> text)
        {
            _text = text;
            _offset = 0;
            _significant = SentenceBreakClass.Other;
            _priorSignificant = SentenceBreakClass.Other;
            _ignorable = SentenceBreakClass.Other;
            _hasSignificant = false;
            _hasPriorSignificant = false;
            _lastIsIgnorable = false;
            _terminator = SentenceBreakClass.Other;
            _terminatorStage = TerminatorStage.None;
            _lookaheadStart = -1;
            _lookaheadEnd = -1;
            _lookaheadResult = false;
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

            Consume(current.SentenceBreakClass);

            var currentEnd = current.End;

            while (currentEnd < _text.Length)
            {
                var next = ReadForward(currentEnd);

                if (IsBoundary(current, next))
                {
                    break;
                }

                current = next;

                Consume(current.SentenceBreakClass);

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
        private bool IsBoundary(in SentenceBreakUnit current, in SentenceBreakUnit next)
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

            // SB5: X (Extend | Format) × X — Extend/Format do not break from the preceding char.
            // The right-side check keeps the walk from advancing past them; the left side is
            // resolved by Left, which reports the base an ignorable attaches to.
            if (IsIgnorable(next.SentenceBreakClass))
            {
                return false;
            }

            var left = Left;
            var right = next.SentenceBreakClass;

            // SB6: ATerm × Numeric
            if (left == SentenceBreakClass.ATerm && right == SentenceBreakClass.Numeric)
            {
                return false;
            }

            // SB7: (Upper | Lower) ATerm × Upper
            if (left == SentenceBreakClass.ATerm &&
                right == SentenceBreakClass.Upper &&
                _hasPriorSignificant &&
                IsUpperOrLower(_priorSignificant))
            {
                return false;
            }

            // SB8: ATerm Close* Sp* × (¬{OLetter|Upper|Sep|CR|LF|STerm|ATerm})* Lower
            // If the left context is ATerm Close* Sp*, and scanning right we find a Lower
            // (without hitting a blocker first), do not break.
            if (_terminator == SentenceBreakClass.ATerm &&
                MatchesTerminatorContext(TerminatorStage.Space) &&
                HasLowerAhead(next.Start))
            {
                return false;
            }

            // SB8a: (STerm | ATerm) Close* Sp* × (SContinue | STerm | ATerm)
            if (MatchesTerminatorContext(TerminatorStage.Space) &&
                (right == SentenceBreakClass.SContinue ||
                 right == SentenceBreakClass.STerm ||
                 right == SentenceBreakClass.ATerm))
            {
                return false;
            }

            // SB9: (STerm | ATerm) Close* × (Close | Sp | Sep | CR | LF)
            if (MatchesTerminatorContext(TerminatorStage.Close) &&
                (right == SentenceBreakClass.Close ||
                 right == SentenceBreakClass.Sp ||
                 IsSep(right)))
            {
                return false;
            }

            // SB10: (STerm | ATerm) Close* Sp* × (Sp | Sep | CR | LF)
            if (MatchesTerminatorContext(TerminatorStage.Space) &&
                (right == SentenceBreakClass.Sp || IsSep(right)))
            {
                return false;
            }

            // SB11: (STerm | ATerm) Close* Sp* (Sep | CR | LF)? ÷
            if (MatchesTerminatorContext(TerminatorStage.Separator))
            {
                return true;
            }

            // SB998: Otherwise, no break.
            return false;
        }

        // Folds one codepoint into the left-hand context.
        private void Consume(SentenceBreakClass cls)
        {
            if (IsIgnorable(cls))
            {
                _ignorable = cls;
                _lastIsIgnorable = true;

                // SB5 attaches an ignorable to the preceding base, leaving the context as it
                // was. After a paragraph separator there is no base to attach to, so the
                // ignorable stands on its own and matches no terminator grammar.
                if (!_hasSignificant || IsSep(_significant))
                {
                    _terminator = SentenceBreakClass.Other;
                    _terminatorStage = TerminatorStage.None;
                }

                return;
            }

            _priorSignificant = _significant;
            _hasPriorSignificant = _hasSignificant;
            _significant = cls;
            _hasSignificant = true;
            _lastIsIgnorable = false;

            // Walk the (STerm | ATerm) Close* Sp* (Sep | CR | LF)? grammar. Its stages are
            // ordered, so a class that would move back through them ends the match instead.
            if (cls is SentenceBreakClass.ATerm or SentenceBreakClass.STerm)
            {
                _terminator = cls;
                _terminatorStage = TerminatorStage.Terminator;
            }
            else if (_terminatorStage == TerminatorStage.None)
            {
                // No terminator to the left, so there is nothing to advance.
            }
            else if (cls == SentenceBreakClass.Close && _terminatorStage <= TerminatorStage.Close)
            {
                _terminatorStage = TerminatorStage.Close;
            }
            else if (cls == SentenceBreakClass.Sp && _terminatorStage <= TerminatorStage.Space)
            {
                _terminatorStage = TerminatorStage.Space;
            }
            else if (IsSep(cls) && _terminatorStage <= TerminatorStage.Space)
            {
                _terminatorStage = TerminatorStage.Separator;
            }
            else
            {
                _terminator = SentenceBreakClass.Other;
                _terminatorStage = TerminatorStage.None;
            }
        }

        // The effective class to the left of the next codepoint, per SB5: the base an
        // Extend/Format attaches to, or the ignorable itself when it has no base.
        private readonly SentenceBreakClass Left
            => _lastIsIgnorable && (!_hasSignificant || IsSep(_significant)) ? _ignorable : _significant;

        // True when the text to the left matches (STerm | ATerm) Close* Sp* (Sep | CR | LF)?
        // truncated at the given stage: Close for SB9, Space for SB8/SB8a/SB10, Separator for SB11.
        private readonly bool MatchesTerminatorContext(TerminatorStage upTo)
            => _terminatorStage != TerminatorStage.None && _terminatorStage <= upTo;

        // SB8 lookahead: true iff a Lower is reachable from start without passing a blocker
        // (OLetter | Upper | Sep | CR | LF | STerm | ATerm). Extend/Format are transparent per SB5.
        private bool HasLowerAhead(int start)
        {
            if (start >= _lookaheadStart && start <= _lookaheadEnd)
            {
                return _lookaheadResult;
            }

            var position = start;
            var result = false;

            while (position < _text.Length)
            {
                var cls = Codepoint.ReadAt(_text, position, out var count).SentenceBreakClass;

                if (cls == SentenceBreakClass.Lower)
                {
                    result = true;
                    break;
                }

                if (IsSb8Blocker(cls))
                {
                    break;
                }

                position += count;
            }

            _lookaheadStart = start;
            _lookaheadEnd = position;
            _lookaheadResult = result;

            return result;
        }

        private readonly SentenceBreakUnit ReadForward(int start)
        {
            var codepoint = Codepoint.ReadAt(_text, start, out var count);
            return new SentenceBreakUnit(codepoint, start, start + count);
        }

        // SB4: Sep | CR | LF are paragraph separators.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsSep(SentenceBreakClass cls)
        {
            const ulong mask =
                (1UL << (int)SentenceBreakClass.Sep) |
                (1UL << (int)SentenceBreakClass.CarriageReturn) |
                (1UL << (int)SentenceBreakClass.LineFeed);

            return ((1UL << (int)cls) & mask) != 0UL;
        }

        // SB5: Extend and Format are transparent (ignored) for sentence-boundary rules.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsIgnorable(SentenceBreakClass cls)
        {
            const ulong mask =
                (1UL << (int)SentenceBreakClass.Extend) |
                (1UL << (int)SentenceBreakClass.Format);

            return ((1UL << (int)cls) & mask) != 0UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsUpperOrLower(SentenceBreakClass cls)
        {
            const ulong mask =
                (1UL << (int)SentenceBreakClass.Upper) |
                (1UL << (int)SentenceBreakClass.Lower);

            return ((1UL << (int)cls) & mask) != 0UL;
        }

        // SB8 forward-scan blocker set: classes that terminate the lookahead without
        // matching Lower. Lower itself is the TARGET and is NOT included here.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsSb8Blocker(SentenceBreakClass cls)
        {
            const ulong mask =
                (1UL << (int)SentenceBreakClass.OLetter) |
                (1UL << (int)SentenceBreakClass.Upper) |
                (1UL << (int)SentenceBreakClass.Sep) |
                (1UL << (int)SentenceBreakClass.CarriageReturn) |
                (1UL << (int)SentenceBreakClass.LineFeed) |
                (1UL << (int)SentenceBreakClass.STerm) |
                (1UL << (int)SentenceBreakClass.ATerm);

            return ((1UL << (int)cls) & mask) != 0UL;
        }

        // Stages of the (STerm | ATerm) Close* Sp* (Sep | CR | LF)? grammar, in the order
        // they may appear. A match only ever moves forward through them.
        private enum TerminatorStage : byte
        {
            None,
            Terminator,
            Close,
            Space,
            Separator
        }

        private readonly struct SentenceBreakUnit(Codepoint codepoint, int start, int end)
        {
            public SentenceBreakClass SentenceBreakClass { get; } = codepoint.SentenceBreakClass;

            public int Start { get; } = start;

            public int End { get; } = end;
        }
    }
}
