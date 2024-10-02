using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using static System.Net.Mime.MediaTypeNames;

namespace Avalonia.Media.TextFormatting.Unicode
{
    internal ref struct LineBreakEnumeratorV151
    {
        private static readonly BreakUnit s_eot = new() { eot = true };
        private static readonly BreakUnit s_sot = new() { sot = true };

        public readonly ReadOnlySpan<char> _text;
        private readonly LineBreakState _state;

        private class LineBreakState
        {

            private BreakUnit? _next;

            public LineBreakState()
            {
                _next = null;

                Previous = s_sot;
                Current = s_sot;
            }

            public BreakUnit Previous { get; private set; }

            public BreakUnit Current { get; private set; }

            public BreakUnit Next(ReadOnlySpan<char> text)
            {
                return _next ??= Peek(text);
            }

            public BreakUnit AfterNext(ReadOnlySpan<char> text)
            {
                var next = Next(text);

                if (next.eot)
                {
                    return next;
                }

                return PeekAt(text, next.Start + next.Length);
            }

            public int Position { get; private set; }

            public int RegionalIndicator { get; set; }

            public bool Spaces { get; set; }

            public bool LB8 { get; set; }

            private static BreakUnit PeekAt(ReadOnlySpan<char> text, int index)
            {
                if (index >= text.Length)
                {
                    return s_eot;
                }

                var codepoint = Codepoint.ReadAt(text, index, out var count);

                if (codepoint == Codepoint.ReplacementCodepoint)
                {
                    return s_eot;
                }

                return new BreakUnit(codepoint, index, count);
            }

            public BreakUnit Peek(ReadOnlySpan<char> text)
            {
                return PeekAt(text, Position);
            }

            public BreakUnit Read(ReadOnlySpan<char> text)
            {
                var next = Peek(text);

                if (next.eot)
                {
                    return next;
                }

                Previous = Current;

                Position += next.Length;

                Current = next;

                _next = null;

                return next;
            }

            internal static LineBreakClass ClassAfterSpaces(ReadOnlySpan<char> text, BreakUnit current)
            {
                var position = current.Start + current.Length;

                if (position >= text.Length)
                {
                    return current.cls;
                }

                var enumerator = new CodepointEnumerator(text.Slice(position));

                var cp = current.Codepoint;

                while (enumerator.MoveNext(out cp) && cp.LineBreakClass == LineBreakClass.Space)
                { }

                return cp.LineBreakClass;
            }
        }

        public LineBreakEnumeratorV151(ReadOnlySpan<char> text)
        {
            _text = text;
            _state = new LineBreakState();
        }

        public readonly bool MoveNext([NotNullWhen(true)] out LineBreak lineBreak)
        {
            lineBreak = default;

            if (_state.Position >= _text.Length)
            {
                return false;
            }

            LineBreak? result = null;

            while (result == null)
            {
                _state.Read(_text);

                result = ExecuteRules(_text, _state);
            }

            if (result == null)
            {
                return false;
            }

            lineBreak = result.Value;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsMandatoryBreakOrLineFeedOrCarriageReturn(LineBreakClass cls)
        {
            const ulong mask =
                (1UL << (int)LineBreakClass.MandatoryBreak) |
                (1UL << (int)LineBreakClass.LineFeed) |
                (1UL << (int)LineBreakClass.CarriageReturn);

            return ((1UL << (int)cls) & mask) != 0UL;
        }

        private static LineBreak GetLineBreak(ReadOnlySpan<char> text, LineBreakState state, bool isRequired)
        {
            var positionMeasure = state.Current.Start;
            var positionWrap = positionMeasure;

            switch (state.Current.cls)
            {
                case LineBreakClass.Space:
                    {
                        positionWrap = state.Current.Start + state.Current.Length;

                        if(state.Previous.cls == LineBreakClass.Space)
                        {
                            positionMeasure = FindPriorNonWhitespace(text, positionMeasure);
                        }

                        break;
                    }
                case LineBreakClass.CarriageReturn:
                case LineBreakClass.LineFeed:
                    {
                        positionWrap += 1;

                        break;
                    }
            }

            if(state.Position >= text.Length)
            {
                if(positionMeasure == positionWrap)
                {
                    positionWrap += 1;
                }

                isRequired = false;
            }

            return new LineBreak(positionMeasure, positionWrap, isRequired);
        }

        private static int FindPriorNonWhitespace(ReadOnlySpan<char> text, int from)
        {
            if (from > 0)
            {
                var cp = Codepoint.ReadAt(text, from - 1, out var count);

                var cls = cp.LineBreakClass;

                if (IsMandatoryBreakOrLineFeedOrCarriageReturn(cls))
                {
                    from -= count;
                }
            }

            while (from > 0)
            {
                var cp = Codepoint.ReadAt(text, from - 1, out var count);

                var cls = cp.LineBreakClass;

                if (cls == LineBreakClass.Space)
                {
                    from -= count;
                }
                else
                {
                    break;
                }
            }

            return from;
        }

        private static LineBreak? ExecuteRules(ReadOnlySpan<char> text, LineBreakState state)
        {
            foreach (var rule in Rules)
            {
                var res = rule.Invoke(text, state);

                switch (res)
                {
                    case RuleResult.Pass:
                        continue;
                    case RuleResult.NoBreak:
                        return null;
                    case RuleResult.MayBreak:
                    case RuleResult.MustBreak:
                        return GetLineBreak(text, state, res == RuleResult.MustBreak);
                    default:
                        throw new InvalidOperationException("Invalid state.");
                }
            }

            return null;
        }

        /// <summary>
        /// LB2: Never break at the start of text.
        /// </summary>
        private static RuleResult LB02(ReadOnlySpan<char> text, LineBreakState state)
        {
            // sot ×
            if (state.Current.Start == 0 && state.Next(text).eot)
            {
                return RuleResult.NoBreak;
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB3: Always break at the end of text.
        /// </summary>
        private static RuleResult LB03(ReadOnlySpan<char> text, LineBreakState state)
        {
            if (state.Next(text).eot && (state.Current.Length == 0 || state.Current.Length != state.Position))
            {
                return RuleResult.MustBreak;
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB4: Always break after hard line breaks.
        /// </summary>
        private static RuleResult LB04(ReadOnlySpan<char> text, LineBreakState state)
        {
            // BK !
            if (state.Current.cls == LineBreakClass.MandatoryBreak)
            {
                return RuleResult.MustBreak;
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB5: Treat CR followed by LF, as well as CR, LF, and NL as hard line
        /// </summary>
        private static RuleResult LB05(ReadOnlySpan<char> text, LineBreakState state)
        {
            switch (state.Current.cls)
            {
                case LineBreakClass.CarriageReturn:
                    if (state.Next(text).cls == LineBreakClass.LineFeed)
                    {
                        return RuleResult.NoBreak; // CR × LF
                    }

                    return RuleResult.MustBreak; // CR !

                case LineBreakClass.LineFeed: // LF !
                case LineBreakClass.NextLine: // NL !
                    return RuleResult.MustBreak;
                default:
                    return RuleResult.Pass;
            }
        }

        /// <summary>
        /// LB6: Do not break before hard line breaks.
        /// </summary>
        /// <returns></returns>
        private static RuleResult LB06(ReadOnlySpan<char> text, LineBreakState state)
        {
            // × ( BK | CR | LF | NL )
            if (IsBreakClass(state.Next(text).cls))
            {
                return RuleResult.NoBreak;
            }

            return RuleResult.Pass;
        }

        private static RuleResult EndOfSpaces(ReadOnlySpan<char> text, LineBreakState state)
        {
            if (state.Current.cls != LineBreakClass.RegionalIndicator)
            {
                state.RegionalIndicator = 0;
            }

            if (!state.Spaces)
            {
                return RuleResult.Pass;
            }

            if (state.Next(text).cls != LineBreakClass.Space)
            {
                state.Spaces = false;
            }

            return RuleResult.NoBreak;

        }

        /// <summary>
        /// LB7: Do not break before spaces or zero width space.
        /// </summary>
        private static RuleResult LB07(ReadOnlySpan<char> text, LineBreakState state)
        {
            // × ZW
            if (state.Next(text).cls == LineBreakClass.ZWSpace)
            {
                return RuleResult.NoBreak;
            }

            // × SP
            if (state.Next(text).cls == LineBreakClass.Space)
            {
                switch (state.Current.cls)
                {
                    case LineBreakClass.ZWSpace: // See LB8
                    case LineBreakClass.OpenPunctuation: // See LB14
                    case LineBreakClass.Quotation: // See LB15
                    case LineBreakClass.ClosePunctuation: // See LB16
                    case LineBreakClass.CloseParenthesis: // See LB16
                    case LineBreakClass.BreakBoth: // See LB17
                        break;
                    default:
                        return RuleResult.NoBreak;
                }
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB8: Break before any character following a zero-width space, even if one or more spaces intervene.
        /// </summary>
        private static RuleResult LB08(ReadOnlySpan<char> text, LineBreakState state)
        {
            // LB8 is special, because it breaks after the run of spaces, unlike
            // the .spaces cases, which NO_BREAK after the rub of spaces.

            // ZW SP* ÷
            if (state.LB8)
            {
                // Assume state.Next(text).cls !== SP, because LB7
                state.LB8 = false;

                return RuleResult.MayBreak;
            }

            if (state.Current.cls == LineBreakClass.ZWSpace)
            {
                if (state.Next(text).cls == LineBreakClass.Space)
                {
                    state.LB8 = true;

                    return RuleResult.NoBreak;
                }

                return RuleResult.MayBreak;
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB8a: Do not break after a zero width joiner.
        /// </summary>
        private static RuleResult LB08a(ReadOnlySpan<char> text, LineBreakState state)
        {
            // ZWJ ×
            if (state.Current.cls == LineBreakClass.ZWJ)
            {
                return RuleResult.NoBreak;
            }

            return RuleResult.Pass;
        }

        private static bool IsBreakClass(LineBreakClass cls)
        {
            switch (cls)
            {
                case LineBreakClass.MandatoryBreak:
                case LineBreakClass.CarriageReturn:
                case LineBreakClass.LineFeed:
                case LineBreakClass.NextLine:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// LB9: Do not break a combining character sequence;
        /// treat it as if it has the line breaking class of the base character in all of the following rules.
        /// Treat ZWJ as if it were CM.
        /// </summary>
        private static RuleResult LB09(ReadOnlySpan<char> text, LineBreakState state)
        {
            // Treat X (CM | ZWJ)* as if it were X.
            // where X is any line break class except BK, CR, LF, NL, SP, or ZW.

            var cls = state.Current.cls;

            if (IsBreakClass(cls) || cls == LineBreakClass.Space || cls == LineBreakClass.ZWSpace)
            {
                return RuleResult.Pass;
            }

            switch (state.Next(text).cls)
            {
                case LineBreakClass.CombiningMark:
                case LineBreakClass.ZWJ:
                    {
                        state.Next(text).Ignored = true;

                        return RuleResult.NoBreak;
                    }
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB10: Treat any remaining combining mark or ZWJ as AL.
        /// </summary>
        private static RuleResult LB10(ReadOnlySpan<char> text, LineBreakState state)
        {
            if (state.Current.cls == LineBreakClass.CombiningMark)
            {
                state.Current.cls = LineBreakClass.Alphabetic;
            }

            if (state.Next(text).cls == LineBreakClass.CombiningMark)
            {
                state.Next(text).cls = LineBreakClass.Alphabetic;
            }
            return RuleResult.Pass;
        }

        /// <summary>
        /// LB11: Do not break before or after Word joiner and related characters.
        /// </summary>
        private static RuleResult LB11(ReadOnlySpan<char> text, LineBreakState state)
        {
            if (state.Next(text).cls == LineBreakClass.WordJoiner /* × WJ */
                || state.Current.cls == LineBreakClass.WordJoiner /* WJ × */)
            {
                return RuleResult.NoBreak;
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB12: Do not break after NBSP and related characters.
        /// </summary>
        private static RuleResult LB12(ReadOnlySpan<char> text, LineBreakState state)
        {
            // GL ×
            if (state.Current.cls == LineBreakClass.Glue)
            {
                return RuleResult.NoBreak;
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB12a: Do not break before NBSP and related characters, except after spaces and hyphens.
        /// </summary>
        private static RuleResult LB12a(ReadOnlySpan<char> text, LineBreakState state)
        {
            // [^SP BA HY] × GL
            if (state.Next(text).cls == LineBreakClass.Glue)
            {
                switch (state.Current.cls)
                {
                    case LineBreakClass.Space:
                    case LineBreakClass.BreakAfter:
                    case LineBreakClass.Hyphen:
                        return RuleResult.Pass;
                    default:
                        return RuleResult.NoBreak;
                }
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB13: Do not break before ‘]’ or ‘!’ or ‘;’ or ‘/’, even after spaces.
        /// </summary>
        private static RuleResult LB13(ReadOnlySpan<char> text, LineBreakState state)
        {
            // × CL (e.g.)
            switch (state.Next(text).cls)
            {
                case LineBreakClass.ClosePunctuation:
                case LineBreakClass.CloseParenthesis:
                case LineBreakClass.Exclamation:
                case LineBreakClass.InfixNumeric:
                case LineBreakClass.BreakSymbols:
                    return RuleResult.NoBreak;
                default:
                    return RuleResult.Pass;
            }
        }

        /// <summary>
        /// LB14: Do not break after ‘[’, even after spaces.
        /// </summary>
        private static RuleResult LB14(ReadOnlySpan<char> text, LineBreakState state)
        {
            // OP SP* ×
            if (state.Current.cls == LineBreakClass.OpenPunctuation)
            {
                if (state.Next(text).cls == LineBreakClass.Space)
                {
                    state.Spaces = true;
                }

                return RuleResult.NoBreak;
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB15a: Do not break after an unresolved initial punctuation that lies at the start of the line,
        /// after a space, after opening punctuation, or after an unresolved quotation mark, even after spaces.
        /// </summary>
        private static RuleResult LB15a(ReadOnlySpan<char> text, LineBreakState state)
        {
            // (sot | BK | CR | LF | NL | OP | QU | GL | SP | ZW) [\p{Pi}&QU] SP* ×
            if (IsMatch(state.Previous) &&
                state.Current.Codepoint.GeneralCategory == GeneralCategory.InitialPunctuation &&
                state.Current.cls == LineBreakClass.Quotation)
            {
                state.Spaces = true;

                return RuleResult.NoBreak;
            }

            return RuleResult.Pass;

            static bool IsMatch(BreakUnit unit)
            {
                if (unit.sot)
                {
                    return true;
                }

                if (IsBreakClass(unit.cls))
                {
                    return true;
                }

                switch (unit.cls)
                {
                    case LineBreakClass.OpenPunctuation:
                    case LineBreakClass.Quotation:
                    case LineBreakClass.Glue:
                    case LineBreakClass.Space:
                    case LineBreakClass.ZWSpace:
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        /// LB15b: Do not break before an unresolved final punctuation that lies at the end of the line,
        /// before a space, before a prohibited break, or before an unresolved quotation mark, even after spaces.
        /// </summary>
        private static RuleResult LB15b(ReadOnlySpan<char> text, LineBreakState state)
        {
            // × [\p{Pf}&QU] ( SP | GL | WJ | CL | QU | CP | EX | IS | SY | BK | CR | LF | NL | ZW | eot)
            if (state.Next(text).Codepoint.GeneralCategory == GeneralCategory.FinalPunctuation && (state.Next(text).cls == LineBreakClass.Quotation))
            {
                var after = state.AfterNext(text);

                if (after == null)
                { // Only on eot
                    return RuleResult.NoBreak;
                }

                if (IsBreakClass(after.cls))
                {
                    return RuleResult.NoBreak;
                }

                switch (after.cls)
                {
                    case LineBreakClass.Space:
                    case LineBreakClass.Glue:
                    case LineBreakClass.WordJoiner:
                    case LineBreakClass.ClosePunctuation:
                    case LineBreakClass.Quotation:
                    case LineBreakClass.CloseParenthesis:
                    case LineBreakClass.Exclamation:
                    case LineBreakClass.InfixNumeric:
                    case LineBreakClass.BreakSymbols:
                    case LineBreakClass.ZWSpace:
                        return RuleResult.NoBreak;
                }
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB16: Do not break between closing punctuation and a nonstarter (lb=NS),
        /// even with intervening spaces.
        /// </summary>
        private static RuleResult LB16(ReadOnlySpan<char> text, LineBreakState state)
        {
            // (CL | CP) SP* × NS
            if ((state.Current.cls == LineBreakClass.ClosePunctuation) || (state.Current.cls == LineBreakClass.CloseParenthesis))
            {
                if (LineBreakState.ClassAfterSpaces(text, state.Current) == LineBreakClass.Nonstarter)
                {
                    if (state.Next(text).cls == LineBreakClass.Space)
                    {
                        state.Spaces = true;
                    }

                    return RuleResult.NoBreak;
                }
                if (state.Next(text).cls == LineBreakClass.Space)
                {
                    return RuleResult.NoBreak; // LB7
                }
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB17: Do not break within ‘——’, even with intervening spaces.
        /// </summary>
        private static RuleResult LB17(ReadOnlySpan<char> text, LineBreakState state)
        {
            // B2 SP* × B2
            if (state.Current.cls == LineBreakClass.BreakBoth)
            {
                if (LineBreakState.ClassAfterSpaces(text, state.Current) == LineBreakClass.BreakBoth)
                {
                    if (state.Next(text).cls != LineBreakClass.Space)
                    {
                        return RuleResult.NoBreak; // LB7
                    }

                    state.Spaces = true;

                    return RuleResult.NoBreak;
                }

                if (state.Next(text).cls == LineBreakClass.Space)
                {
                    return RuleResult.NoBreak; // LB7
                }
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB18: Break after spaces.
        /// </summary>
        private static RuleResult LB18(ReadOnlySpan<char> text, LineBreakState state)
        {
            // SP ÷
            if (state.Current.cls == LineBreakClass.Space)
            {
                return RuleResult.MayBreak;
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB19: Do not break before or after quotation marks, such as ‘ ” ’.
        /// </summary>
        private static RuleResult LB19(ReadOnlySpan<char> text, LineBreakState state)
        {
            // × QU
            // QU ×
            if ((state.Current.cls == LineBreakClass.Quotation) || (state.Next(text).cls == LineBreakClass.Quotation))
            {
                return RuleResult.NoBreak;
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB20: Break before and after unresolved CB.
        /// </summary>
        private static RuleResult LB20(ReadOnlySpan<char> text, LineBreakState state)
        {
            // ÷ CB
            // CB ÷
            if ((state.Current.cls == LineBreakClass.ContingentBreak) || (state.Next(text).cls == LineBreakClass.ContingentBreak))
            {
                return RuleResult.MayBreak;
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB21: Do not break before hyphen-minus, other hyphens, fixed-width spaces, small kana, and other non-starters, or after acute accents.
        /// </summary>
        private static RuleResult LB21(ReadOnlySpan<char> text, LineBreakState state)
        {
            // BB ×
            if (state.Current.cls == LineBreakClass.BreakBefore)
            {
                return RuleResult.NoBreak;
            }

            // × (BA | HY | NS)
            switch (state.Next(text).cls)
            {
                case LineBreakClass.BreakAfter:
                case LineBreakClass.Hyphen:
                case LineBreakClass.Nonstarter:
                    return RuleResult.NoBreak;
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB21a: Don't break after Hebrew + Hyphen.
        /// </summary>
        private static RuleResult LB21a(ReadOnlySpan<char> text, LineBreakState state)
        {
            // HL (HY | BA) ×
            if (state.Previous.cls == LineBreakClass.HebrewLetter
                && state.Current.cls is LineBreakClass.Hyphen or LineBreakClass.BreakAfter)
            {
                return RuleResult.NoBreak;
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB21b: Don’t break between Solidus and Hebrew letters.
        /// </summary>
        private static RuleResult LB21b(ReadOnlySpan<char> text, LineBreakState state)
        {
            // SY × HL
            if ((state.Current.cls == LineBreakClass.BreakSymbols) && (state.Next(text).cls == LineBreakClass.HebrewLetter))
            {
                return RuleResult.NoBreak;
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB22: Do not break before ellipses.
        /// </summary>
        private static RuleResult LB22(ReadOnlySpan<char> text, LineBreakState state)
        {
            // × IN
            if (state.Next(text).cls == LineBreakClass.Inseparable)
            {
                return RuleResult.NoBreak;
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB23: Do not break between digits and letters.
        /// </summary>
        private static RuleResult LB23(ReadOnlySpan<char> text, LineBreakState state)
        {
            switch (state.Current.cls)
            {
                case LineBreakClass.Alphabetic:
                case LineBreakClass.HebrewLetter:
                    {
                        // (AL | HL) × NU
                        if (state.Next(text).cls == LineBreakClass.Numeric)
                        {
                            return RuleResult.NoBreak;
                        }

                        break;
                    }

                case LineBreakClass.Numeric:
                    {
                        // NU × (AL | HL)
                        if (state.Next(text).cls is LineBreakClass.Alphabetic or LineBreakClass.HebrewLetter)
                        {
                            return RuleResult.NoBreak;
                        }

                        break;
                    }

            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB23a: Do not break between numeric prefixes and ideographs, or between
        /// ideographs and numeric postfixes.
        /// </summary>
        private static RuleResult LB23a(ReadOnlySpan<char> text, LineBreakState state)
        {
            // PR × (ID | EB | EM)
            if ((state.Current.cls == LineBreakClass.PrefixNumeric)
                && IsMatch(state.Next(text).cls))
            {
                return RuleResult.NoBreak;
            }

            // (ID | EB | EM) × PO
            if ((state.Next(text).cls == LineBreakClass.PostfixNumeric)
                && IsMatch(state.Current.cls))
            {
                return RuleResult.NoBreak;
            }

            return RuleResult.Pass;

            static bool IsMatch(LineBreakClass cls)
            {
                switch (cls)
                {
                    case LineBreakClass.Ideographic:
                    case LineBreakClass.EBase:
                    case LineBreakClass.EModifier:
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        /// LB24: Do not break between numeric prefix/postfix and letters, or between
        /// letters and prefix/postfix.
        /// </summary>
        private static RuleResult LB24(ReadOnlySpan<char> text, LineBreakState state)
        {
            // (PR | PO) × (AL | HL)
            if (state.Current.cls is LineBreakClass.PrefixNumeric or LineBreakClass.PostfixNumeric
                && state.Next(text).cls is LineBreakClass.Alphabetic or LineBreakClass.HebrewLetter)
            {
                return RuleResult.NoBreak;
            }
            // (AL | HL) × (PR | PO)
            if (state.Current.cls is LineBreakClass.Alphabetic or LineBreakClass.HebrewLetter
                && state.Next(text).cls is LineBreakClass.PrefixNumeric or LineBreakClass.PostfixNumeric)
            {
                return RuleResult.NoBreak;
            }

            // Super expensive?:
            // ( PR | PO) ? ( OP | HY ) ? NU (NU | SY | IS) * (CL | CP) ? ( PR | PO) ?
            return RuleResult.Pass;
        }


        /// <summary>
        /// LB25: Do not break between the following pairs of classes relevant to numbers
        /// </summary>
        private static RuleResult LB25(ReadOnlySpan<char> text, LineBreakState state)
        {
            switch (state.Current.cls)
            {
                case LineBreakClass.Numeric:
                    {
                        // NU × PO
                        // NU × PR
                        // NU × NU
                        if (state.Next(text).cls is LineBreakClass.PostfixNumeric or LineBreakClass.PrefixNumeric or LineBreakClass.Numeric)
                        {
                            return RuleResult.NoBreak;
                        }
                        break;
                    }

                case LineBreakClass.ClosePunctuation:
                case LineBreakClass.CloseParenthesis:
                    {
                        // CL × PO
                        // CL × PR
                        // CP × PO
                        // CP × PR
                        if (state.Next(text).cls is LineBreakClass.PostfixNumeric or LineBreakClass.PrefixNumeric)
                        {
                            return RuleResult.NoBreak;
                        }
                        break;
                    }
                case LineBreakClass.PostfixNumeric:
                case LineBreakClass.PrefixNumeric:
                    {
                        // PO × OP
                        // PO × NU
                        // PR × OP
                        // PR × NU
                        if (state.Next(text).cls is LineBreakClass.OpenPunctuation or LineBreakClass.Numeric)
                        {
                            return RuleResult.NoBreak;
                        }
                        break;
                    }
                case LineBreakClass.HebrewLetter:
                case LineBreakClass.InfixNumeric:
                case LineBreakClass.BreakSymbols:
                    {
                        // HY × NU
                        // IS × NU
                        // SY × NU
                        if (state.Next(text).cls == LineBreakClass.Numeric)
                        {
                            return RuleResult.NoBreak;
                        }
                        break;
                    }
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB26: Do not break a Korean syllable.
        /// </summary>
        private static RuleResult LB26(ReadOnlySpan<char> text, LineBreakState state)
        {
            switch (state.Current.cls)
            {
                case LineBreakClass.JL:
                    {
                        // JL × (JL | JV | H2 | H3)
                        switch (state.Next(text).cls)
                        {
                            case LineBreakClass.JL:
                            case LineBreakClass.JV:
                            case LineBreakClass.H2:
                            case LineBreakClass.H3:
                                return RuleResult.NoBreak;
                        }

                        break;
                    }
                case LineBreakClass.JV:
                case LineBreakClass.H2:
                    {
                        // (JV | H2) × (JV | JT)
                        switch (state.Next(text).cls)
                        {
                            case LineBreakClass.JV:
                            case LineBreakClass.JT:
                                return RuleResult.NoBreak;
                        }
                        break;
                    }
                case LineBreakClass.JT:
                case LineBreakClass.H3:
                    {
                        // (JT | H3) × JT
                        if (state.Next(text).cls == LineBreakClass.JT)
                        {
                            return RuleResult.NoBreak;
                        }
                        break;
                    }
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB27: Treat a Korean Syllable Block the same as ID.
        /// </summary>
        private static RuleResult LB27(ReadOnlySpan<char> text, LineBreakState state)
        {
            switch (state.Current.cls)
            {
                case LineBreakClass.JL:
                case LineBreakClass.JV:
                case LineBreakClass.JT:
                case LineBreakClass.H2:
                case LineBreakClass.H3:
                    {
                        // (JL | JV | JT | H2 | H3) × PO
                        if (state.Next(text).cls == LineBreakClass.PostfixNumeric)
                        {
                            return RuleResult.NoBreak;
                        }
                        break;
                    }
                case LineBreakClass.PrefixNumeric:
                    {
                        // PR × (JL | JV | JT | H2 | H3)
                        switch (state.Next(text).cls)
                        {
                            case LineBreakClass.JL:
                            case LineBreakClass.JV:
                            case LineBreakClass.JT:
                            case LineBreakClass.H2:
                            case LineBreakClass.H3:
                                return RuleResult.NoBreak;
                        }

                        break;
                    }
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB28: Do not break between alphabetics (“at”).
        /// </summary>
        private static RuleResult LB28(ReadOnlySpan<char> text, LineBreakState state)
        {
            // (AL | HL) × (AL | HL)
            if (((state.Current.cls == LineBreakClass.Alphabetic) || (state.Current.cls == LineBreakClass.HebrewLetter))
              && ((state.Next(text).cls == LineBreakClass.Alphabetic) || (state.Next(text).cls == LineBreakClass.HebrewLetter)))
            {
                return RuleResult.NoBreak;
            }

            return RuleResult.Pass;
        }

        private const char DotCircle = '\u25CC';

        /// <summary>
        /// LB28a: Do not break inside the orthographic syllables of Brahmic scripts.
        /// </summary>
        private static RuleResult LB28a(ReadOnlySpan<char> text, LineBreakState state)
        {
            // AP × (AK | ◌ | AS)
            if ((state.Current.cls == LineBreakClass.AksaraPrebase) && isMatch(state.Next(text)))
            {
                return RuleResult.NoBreak;
            }

            // (AK | ◌ | AS) × (VF | VI)
            if (isMatch(state.Current)
                && ((state.Next(text).cls == LineBreakClass.ViramaFinal) || (state.Next(text).cls == LineBreakClass.Virama)))
            {
                return RuleResult.NoBreak;
            }

            // (AK | ◌ | AS) VI × (AK | ◌)
            if (isMatch(state.Previous)
                && (state.Current.cls == LineBreakClass.Virama)
                && ((state.Next(text).cls == LineBreakClass.Aksara) || (state.Next(text).Codepoint == DotCircle)))
            {
                return RuleResult.NoBreak;
            }

            // (AK | ◌ | AS) × (AK | ◌ | AS) VF
            if (isMatch(state.Current)
                && isMatch(state.Next(text))
                && (state.AfterNext(text).cls == LineBreakClass.ViramaFinal))
            {
                return RuleResult.NoBreak;
            }

            return RuleResult.Pass;

            /**
             * AK | ◌ | AS
             */
            static bool isMatch(BreakUnit chr)
            {
                return (chr.cls == LineBreakClass.Aksara) || (chr.Codepoint == DotCircle) || (chr.cls == LineBreakClass.AksaraStart);
            }
        }

        /// <summary>
        /// LB29: Do not break between numeric punctuation and alphabetics (“e.g.”).
        /// </summary>
        private static RuleResult LB29(ReadOnlySpan<char> text, LineBreakState state)
        {
            // IS × (AL | HL)
            if ((state.Current.cls == LineBreakClass.InfixNumeric)
              && ((state.Next(text).cls == LineBreakClass.Alphabetic) || (state.Next(text).cls == LineBreakClass.HebrewLetter)))
            {
                return RuleResult.NoBreak;
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB30: Do not break between letters, numbers, or ordinary symbols and opening or closing parentheses.
        /// </summary>
        private static RuleResult LB30(ReadOnlySpan<char> text, LineBreakState state)
        {
            switch (state.Current.cls)
            {
                case LineBreakClass.Alphabetic:
                case LineBreakClass.HebrewLetter:
                case LineBreakClass.Numeric:
                    // (AL | HL | NU) × [OP-[\p{ea=F}\p{ea=W}\p{ea=H}]]
                    if ((state.Next(text).cls == LineBreakClass.OpenPunctuation) && state.Next(text).Codepoint.EastAsianWidthClass == EastAsianWidthClass.Ambiguous)
                    {
                        return RuleResult.NoBreak;
                    }
                    break;
                case LineBreakClass.CloseParenthesis:
                    // [CP-[\p{ea=F}\p{ea=W}\p{ea=H}]] × (AL | HL | NU)
                    if (state.Current.Codepoint.EastAsianWidthClass == EastAsianWidthClass.Ambiguous)
                    {
                        switch (state.Next(text).cls)
                        {
                            case LineBreakClass.Alphabetic:
                            case LineBreakClass.HebrewLetter:
                            case LineBreakClass.Numeric:
                                {
                                    return RuleResult.NoBreak;
                                }
                        }

                    }
                    break;
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB30a: Break between two regional indicator symbols if and only if there
        /// are an even number of regional indicators preceding the position of the
        /// break.
        /// </summary>
        /// <returns></returns>
        private static RuleResult LB30a(ReadOnlySpan<char> text, LineBreakState state)
        {
            if (state.Current.cls == LineBreakClass.RegionalIndicator)
            {
                if (state.Next(text).cls == LineBreakClass.RegionalIndicator)
                {
                    if (++state.RegionalIndicator % 2 != 0)
                    {
                        return RuleResult.NoBreak;
                    }
                }
            }
            else
            {
                state.RegionalIndicator = 0;
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB30b: Do not break between an emoji base (or potential emoji) and an emoji modifier.
        /// </summary>
        /// <returns></returns>
        private static RuleResult LB30b(ReadOnlySpan<char> text, LineBreakState state)
        {
            // EB × EM
            if ((state.Current.cls == LineBreakClass.EBase) && (state.Next(text).cls == LineBreakClass.EModifier))
            {
                return RuleResult.NoBreak;
            }

            // [\p{Extended_Pictographic}&&\p{Cn}] × EM
            //
            // The Extended_Pictographic property is used to customize segmentation (as
            // described in [UAX29] and [UAX14]) so that possible future emoji ZWJ
            // sequences will not break grapheme clusters, words, or lines. Unassigned
            // codepoints with Line_Break=ID in some blocks are also assigned the
            // Extended_Pictographic property. Those blocks are intended for future
            // allocation of emoji characters.
            if (state.Next(text).cls == LineBreakClass.EModifier &&
                state.Current.Codepoint.GraphemeBreakClass == GraphemeBreakClass.ExtendedPictographic &&
                state.Current.Codepoint.GeneralCategory == GeneralCategory.Unassigned)
            {
                return RuleResult.NoBreak;
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB31: Break everywhere else.
        /// </summary>
        private static RuleResult LB31(ReadOnlySpan<char> text, LineBreakState state)
        {
            return RuleResult.MayBreak;
        }

        public enum RuleResult
        {
            Pass,
            NoBreak,
            MayBreak,
            MustBreak
        }

        public class BreakUnit
        {
            public BreakUnit()
            {
                cls = LineBreakClass.Unknown;
            }

            public BreakUnit(Codepoint codepoint, int start, int length)
            {
                Codepoint = codepoint;
                Start = start;
                Length = length;
                cls = codepoint.LineBreakClass;
            }

            public int Start { get; }
            public int Length { get; }
            public Codepoint Codepoint { get; }
            public LineBreakClass cls { get; set; }
            public bool eot { get; init; }
            public bool sot { get; init; }
            public bool Ignored { get; set; }
        }

        private delegate RuleResult BreakUnitDelegate(ReadOnlySpan<char> text, LineBreakState state);

        private static BreakUnitDelegate[] Rules = [
            LB02,
            LB03,
            LB04,
            LB05,
            LB06,
            EndOfSpaces, // Must be before LB7.
            LB07,
            LB08,
            LB08a,
            LB09,
            LB10,
            LB11,
            LB12,
            LB12a,
            LB13,
            LB14,
            // LB15,
            LB15a,
            LB15b,
            LB16,
            LB17,
            LB18,
            LB19,
            LB20,
            LB21a, // Must be before LB21
            LB21,
            LB21b,
            LB22,
            LB23,
            LB23a,
            LB24,
            LB25,
            LB26,
            LB27,
            LB28,
            LB28a,
            LB29,
            LB30,
            LB30a,
            LB30b,
            LB31,
        ];
    }
}
