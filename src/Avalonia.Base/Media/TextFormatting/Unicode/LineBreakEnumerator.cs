using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Avalonia.Media.TextFormatting.Unicode
{
    public ref struct LineBreakEnumerator
    {
        private const char DotCircle = '\u25CC';

        private static readonly BreakUnit s_sot = new() { StartOfText = true };
        private static readonly BreakUnit s_eot = new() { EndOfText = true };

        public readonly ReadOnlySpan<char> _text;
        private readonly LineBreakState _state;

        public LineBreakEnumerator(ReadOnlySpan<char> text)
        {
            _text = text;
            _state = new LineBreakState();
        }

        public readonly bool MoveNext([NotNullWhen(true)] out LineBreak lineBreak)
        {
            lineBreak = default;

            if (_state.Current.EndOfText)
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
        private static bool IsBreakClass(LineBreakClass cls)
        {
            const ulong mask =
                (1UL << (int)LineBreakClass.MandatoryBreak) |
                (1UL << (int)LineBreakClass.LineFeed) |
                (1UL << (int)LineBreakClass.CarriageReturn) |
                (1UL << (int)LineBreakClass.NextLine);

            return ((1UL << (int)cls) & mask) != 0UL;
        }

        private static LineBreak GetLineBreak(ReadOnlySpan<char> text, LineBreakState state, bool isRequired)
        {
            var positionMeasure = state.Current.Start + state.Current.Length;
            var positionWrap = positionMeasure;

            switch (state.Current.LineBreakClass)
            {
                case LineBreakClass.Space:
                case LineBreakClass.CarriageReturn:
                case LineBreakClass.LineFeed:
                    {
                        if (state.Previous.LineBreakClass == LineBreakClass.CarriageReturn)
                        {
                            positionMeasure = FindPriorNonWhitespace(text, state.Previous.Start);
                        }
                        else
                        {
                            positionMeasure = FindPriorNonWhitespace(text, positionMeasure);
                        }

                        break;
                    }
            }

            return new LineBreak(positionMeasure, positionWrap, isRequired);
        }

        private static int FindPriorNonWhitespace(ReadOnlySpan<char> text, int from)
        {
            if (from > 0)
            {
                var cp = Codepoint.ReadAt(text, from - 1, out var count);

                var cls = cp.LineBreakClass;

                if (IsBreakClass(cls))
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
            foreach (var rule in s_rules)
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
                        return GetLineBreak(text, state, IsBreakClass(state.Current.LineBreakClass));
                    default:
                        throw new InvalidOperationException("Invalid state.");
                }
            }

            return null;
        }

        private static RuleResult QuotationAndRegionalIndicator(ReadOnlySpan<char> text, LineBreakState state)
        {
            if (state.Current.Inherited)
            {
                return RuleResult.Pass;
            }

            if (state.Current.LineBreakClass == LineBreakClass.RegionalIndicator)
            {
                if(++state.RegionalIndicator % 2 == 0)
                {
                    state.RegionalIndicator = 0;
                }
            }

            switch (state.Current.LineBreakClass)
            {
                case LineBreakClass.Quotation:
                    {
                        if (++state.Quotation % 2 == 0)
                        {
                            state.Quotation -= 2;
                        }
                        break;
                    }
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB3: Always break at the end of text.
        /// </summary>
        private static RuleResult LB03(ReadOnlySpan<char> text, LineBreakState state)
        {
            if (state.Current.EndOfText)
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
            if (state.Current.LineBreakClass == LineBreakClass.MandatoryBreak)
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
            switch (state.Current.LineBreakClass)
            {
                case LineBreakClass.CarriageReturn:
                    if (state.Next(text).LineBreakClass == LineBreakClass.LineFeed)
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
            if (IsBreakClass(state.Next(text).LineBreakClass))
            {
                return RuleResult.NoBreak;
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB7: Do not break before spaces or zero width space.
        /// </summary>
        private static RuleResult LB07(ReadOnlySpan<char> text, LineBreakState state)
        {
            // × SP
            // × ZW
            switch (state.Next(text).LineBreakClass)
            {
                case LineBreakClass.Space:
                case LineBreakClass.ZWSpace:
                    {
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
            if (state.LastBeforeSpace.LineBreakClass == LineBreakClass.ZWSpace && state.Next(text).LineBreakClass != LineBreakClass.Space)
            {
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
            if (state.Current.LineBreakClass == LineBreakClass.ZWJ)
            {
                return RuleResult.NoBreak;
            }

            return RuleResult.Pass;
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

            var cls = state.Current.LineBreakClass;

            if (IsBreakClass(cls) || cls == LineBreakClass.Space || cls == LineBreakClass.ZWSpace)
            {
                return RuleResult.Pass;
            }

            switch (state.Next(text).LineBreakClass)
            {
                case LineBreakClass.CombiningMark:
                case LineBreakClass.ZWJ:
                    {
                        state.IgnoreNext(text);

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
            if (state.Current.LineBreakClass == LineBreakClass.CombiningMark)
            {
                state.Current = state.Current with { LineBreakClass = LineBreakClass.Alphabetic, Inherited = true };
            }

            var next = state.Next(text);

            if (next.LineBreakClass == LineBreakClass.CombiningMark)
            {
                state.ReplaceNext(next with { LineBreakClass = LineBreakClass.Alphabetic, Inherited = true });
            }
            return RuleResult.Pass;
        }

        /// <summary>
        /// LB11: Do not break before or after Word joiner and related characters.
        /// </summary>
        private static RuleResult LB11(ReadOnlySpan<char> text, LineBreakState state)
        {
            if (state.Next(text).LineBreakClass == LineBreakClass.WordJoiner /* × WJ */
                || state.Current.LineBreakClass == LineBreakClass.WordJoiner /* WJ × */)
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
            if (state.Current.LineBreakClass == LineBreakClass.Glue)
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
            if (state.Next(text).LineBreakClass == LineBreakClass.Glue)
            {
                switch (state.Current.LineBreakClass)
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
            // × CL
            // × CP
            // × EX
            // × SY
            switch (state.Next(text).LineBreakClass)
            {
                case LineBreakClass.ClosePunctuation:
                case LineBreakClass.CloseParenthesis:
                case LineBreakClass.Exclamation:
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
            if (state.LastBeforeWhitespace.LineBreakClass == LineBreakClass.OpenPunctuation)
            {
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
            if (state.Quotation > 0 && state.LastBeforeWhitespace.Codepoint.GeneralCategory == GeneralCategory.InitialPunctuation && 
                state.LastBeforeWhitespace.LineBreakClass == LineBreakClass.Quotation)
            {
                //at the start of the line
                if (state.Current.StartOfText)
                {
                    return RuleResult.NoBreak;
                }

                //at the start of the line
                if (IsBreakClass(state.Previous.LineBreakClass))
                {
                    return RuleResult.NoBreak;
                }

                if (state.Current.Inherited)
                {
                    //LineBreakClass.Glue
                    return RuleResult.NoBreak;
                }

                //after a space
                switch (state.Current.LineBreakClass)
                {
                    case LineBreakClass.Glue:
                    case LineBreakClass.Space:
                    case LineBreakClass.ZWSpace:
                        return RuleResult.NoBreak;
                }

                //after a space
                switch (state.Previous.LineBreakClass)
                {
                    case LineBreakClass.Glue:
                    case LineBreakClass.Space:
                    case LineBreakClass.ZWSpace:
                        return RuleResult.NoBreak;
                }

                // after opening punctuation
                switch (LineBreakState.Before(text, state.LastBeforeWhitespace).LineBreakClass)
                {
                    case LineBreakClass.OpenPunctuation:                  
                        return RuleResult.NoBreak;
                }

                // after an unresolved quotation mark
                if (state.Quotation - 1 > 0)
                {
                    return RuleResult.NoBreak;
                }
            }

            return RuleResult.Pass;         
        }

        /// <summary>
        /// LB15b: Do not break before an unresolved final punctuation that lies at the end of the line,
        /// before a space, before a prohibited break, or before an unresolved quotation mark, even after spaces.
        /// </summary>
        private static RuleResult LB15b(ReadOnlySpan<char> text, LineBreakState state)
        {
            // × [\p{Pf}&QU] ( SP | GL | WJ | CL | QU | CP | EX | IS | SY | BK | CR | LF | NL | ZW | eot)
            if (state.Next(text).Codepoint.GeneralCategory == GeneralCategory.FinalPunctuation && (state.Next(text).LineBreakClass == LineBreakClass.Quotation))
            {
                var after = LineBreakState.After(text, state.Next(text));

                if (after.EndOfText)
                { // Only on eot
                    return RuleResult.NoBreak;
                }

                if (IsBreakClass(after.LineBreakClass))
                {
                    return RuleResult.NoBreak;
                }

                switch (after.LineBreakClass)
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
        /// LB15c: Break before a decimal mark that follows a space, for instance, in ‘subtract .5’.
        /// </summary>
        private static RuleResult LB15c(ReadOnlySpan<char> text, LineBreakState state)
        {
            // SP ÷ IS NU
            if (state.Current.LineBreakClass == LineBreakClass.Space)
            {
                switch (state.Next(text).LineBreakClass)
                {
                    case LineBreakClass.InfixNumeric when LineBreakState.After(text, state.Next(text)).LineBreakClass == LineBreakClass.Numeric:
                        {
                            return RuleResult.MayBreak;
                        }
                }
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB15d: Otherwise, do not break before ‘;’, ‘,’, or ‘.’, even after spaces.
        /// </summary>
        private static RuleResult LB15d(ReadOnlySpan<char> text, LineBreakState state)
        {
            // × IS
            if (state.Next(text).LineBreakClass == LineBreakClass.InfixNumeric)
            {
                return RuleResult.NoBreak;
            }

            return RuleResult.Pass;
        }


        /// <summary>
        /// LB16: Do not break between closing punctuation and a nonstarter (lb=NS),
        /// even with intervening spaces.
        /// </summary>
        private static RuleResult LB16(ReadOnlySpan<char> text, LineBreakState state)
        {
            switch (state.LastBeforeWhitespace.LineBreakClass)
            {
                case LineBreakClass.ClosePunctuation:
                case LineBreakClass.CloseParenthesis:
                    {
                        var classAfterSpaces = LineBreakState.ClassAfterSpaces(text, state.Current);

                        switch (classAfterSpaces)
                        {
                            case LineBreakClass.ConditionalJapaneseStarter:
                            case LineBreakClass.Nonstarter:
                                {
                                    return RuleResult.NoBreak;
                                }
                        }

                        break;
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
            if (state.LastBeforeWhitespace.LineBreakClass == LineBreakClass.BreakBoth)
            {
                if (LineBreakState.ClassAfterSpaces(text, state.Current) == LineBreakClass.BreakBoth)
                {
                    return RuleResult.NoBreak;
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
            if (state.Current.LineBreakClass == LineBreakClass.Space)
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
            var next = state.Next(text);
     
            if (next.LineBreakClass == LineBreakClass.Quotation)
            {
                // × [QU - \p{Pi}]
                if (next.Codepoint.GeneralCategory != GeneralCategory.InitialPunctuation)
                {
                    return RuleResult.NoBreak;
                }

                //[^$EastAsian] × QU
                if (!state.LastBeforeWhitespace.Codepoint.IsEastAsian)
                {
                    return RuleResult.NoBreak;
                }

                var after = LineBreakState.After(text, next);

                //× QU ( [^$EastAsian] | eot )
                if(!after.Codepoint.IsEastAsian || after.EndOfText)
                {
                    return RuleResult.NoBreak;
                }
            }

            // [QU - \p{Pf}] ×
            if (state.LastBeforeWhitespace.LineBreakClass == LineBreakClass.Quotation && state.LastBeforeWhitespace.Codepoint.GeneralCategory != GeneralCategory.InitialPunctuation)
            {
                return RuleResult.NoBreak;
            }

            if(state.LastBeforeSpace.LineBreakClass == LineBreakClass.Quotation)
            {
                //QU × [^$EastAsian]
                if (!state.Next(text).Codepoint.IsEastAsian)
                {
                    return RuleResult.NoBreak;
                }

                var before = LineBreakState.Before(text, state.LastBeforeSpace);

                //( sot | [^$EastAsian] ) QU ×
                if (before.StartOfText || !before.Codepoint.IsEastAsian)
                {
                    return RuleResult.NoBreak;
                }               
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
            if ((state.Current.LineBreakClass == LineBreakClass.ContingentBreak) || (state.Next(text).LineBreakClass == LineBreakClass.ContingentBreak))
            {
                return RuleResult.MayBreak;
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB20a: Do not break after a word-initial hyphen.
        /// </summary>
        private static RuleResult LB20a(ReadOnlySpan<char> text, LineBreakState state)
        {
            // (sot | BK | CR | LF | NL | SP | ZW | CB | GL)(HY | [\u2010]) × AL
            if (IsMatch(state.Previous) && state.Next(text).LineBreakClass == LineBreakClass.Alphabetic)
            {
                if (state.LastBeforeWhitespace.LineBreakClass == LineBreakClass.Hyphen || state.LastBeforeWhitespace.Codepoint.Value == 8208)
                {
                    return RuleResult.NoBreak;
                }
            }

            return RuleResult.Pass;

            static bool IsMatch(BreakUnit unit)
            {
                if (unit.StartOfText)
                {
                    return true;
                }

                if (IsBreakClass(unit.LineBreakClass))
                {
                    return true;
                }

                switch (unit.LineBreakClass)
                {
                    case LineBreakClass.Space:
                    case LineBreakClass.ZWSpace:
                    case LineBreakClass.ContingentBreak:
                    case LineBreakClass.Glue:
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        /// LB21: Do not break before hyphen-minus, other hyphens, fixed-width spaces, small kana, and other non-starters, or after acute accents.
        /// </summary>
        private static RuleResult LB21(ReadOnlySpan<char> text, LineBreakState state)
        {
            // × (BA | HY | NS)
            switch (state.Next(text).LineBreakClass)
            {
                // [21.01]
                case LineBreakClass.BreakAfter:
                // [21.01]
                case LineBreakClass.Hyphen:
                // [21.01]
                case LineBreakClass.Nonstarter:
                    return RuleResult.NoBreak;
            }

            // [21.04] BB ×
            if (state.Current.LineBreakClass == LineBreakClass.BreakBefore)
            {
                return RuleResult.NoBreak;
            }

            return RuleResult.Pass;
        }


        /// <summary>
        /// LB21a: Don't break after Hebrew + Hyphen.
        /// </summary>
        private static RuleResult LB21a(ReadOnlySpan<char> text, LineBreakState state)
        {
            if(state.Next(text).LineBreakClass != LineBreakClass.HebrewLetter)
            {
                // [21.1] HL(HY|NonEastAsianBA) × [^HL]
                if (state.Previous.LineBreakClass == LineBreakClass.HebrewLetter
                    && state.Current.LineBreakClass is LineBreakClass.Hyphen or LineBreakClass.BreakAfter && !state.Current.Codepoint.IsEastAsian)
                {
                    return RuleResult.NoBreak;
                }
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB21b: Don’t break between Solidus and Hebrew letters.
        /// </summary>
        private static RuleResult LB21b(ReadOnlySpan<char> text, LineBreakState state)
        {
            // [21.2] SY × HL
            if ((state.Current.LineBreakClass == LineBreakClass.BreakSymbols) && (state.Next(text).LineBreakClass == LineBreakClass.HebrewLetter))
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
            if (state.Next(text).LineBreakClass == LineBreakClass.Inseparable)
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
            switch (state.Current.LineBreakClass)
            {
                case LineBreakClass.Alphabetic:
                case LineBreakClass.HebrewLetter:
                    {
                        // (AL | HL) × NU
                        if (state.Next(text).LineBreakClass == LineBreakClass.Numeric)
                        {
                            return RuleResult.NoBreak;
                        }

                        break;
                    }

                case LineBreakClass.Numeric:
                    {
                        // NU × (AL | HL)
                        if (state.Next(text).LineBreakClass is LineBreakClass.Alphabetic or LineBreakClass.HebrewLetter)
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
            if ((state.Current.LineBreakClass == LineBreakClass.PrefixNumeric)
                && IsMatch(state.Next(text).LineBreakClass))
            {
                return RuleResult.NoBreak;
            }

            // (ID | EB | EM) × PO
            if ((state.Next(text).LineBreakClass == LineBreakClass.PostfixNumeric)
                && IsMatch(state.Current.LineBreakClass))
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
            if (state.Current.LineBreakClass is LineBreakClass.PrefixNumeric or LineBreakClass.PostfixNumeric
                && state.Next(text).LineBreakClass is LineBreakClass.Alphabetic or LineBreakClass.HebrewLetter)
            {
                return RuleResult.NoBreak;
            }
            // (AL | HL) × (PR | PO)
            if (state.Current.LineBreakClass is LineBreakClass.Alphabetic or LineBreakClass.HebrewLetter
                && state.Next(text).LineBreakClass is LineBreakClass.PrefixNumeric or LineBreakClass.PostfixNumeric)
            {
                return RuleResult.NoBreak;
            }

            return RuleResult.Pass;
        }


        /// <summary>
        /// LB25: Do not break between the following pairs of classes relevant to numbers
        /// </summary>
        private static RuleResult LB25(ReadOnlySpan<char> text, LineBreakState state)
        {
            switch (state.Next(text).LineBreakClass)
            {
                // [25.06] NU(SY|IS)* x PR
                case LineBreakClass.PrefixNumeric:
                    {
                        switch (state.Current.LineBreakClass)
                        {
                            // [25.04] NU(SY|IS)* CP × PR
                            case LineBreakClass.CloseParenthesis:
                                {
                                    switch (state.Previous.LineBreakClass)
                                    {
                                        case LineBreakClass.Numeric:
                                            {
                                                return RuleResult.NoBreak;
                                            }

                                        case LineBreakClass.BreakSymbols:
                                        case LineBreakClass.InfixNumeric:
                                            {
                                                if (LineBreakState.Before(text, state.Previous).LineBreakClass == LineBreakClass.Numeric)
                                                {
                                                    return RuleResult.NoBreak;
                                                }

                                                break;
                                            }
                                    }

                                    break;
                                }

                            case LineBreakClass.Numeric:
                                {
                                    return RuleResult.NoBreak;
                                }
                            case LineBreakClass.BreakSymbols:
                            case LineBreakClass.InfixNumeric:
                                {
                                    if (state.Previous.LineBreakClass == LineBreakClass.Numeric)
                                    {
                                        return RuleResult.NoBreak;
                                    }

                                    break;
                                }
                            // [25.03] NU(SY|IS)* CL × PR
                            case LineBreakClass.ClosePunctuation:
                                {
                                    switch (state.Previous.LineBreakClass)
                                    {
                                        case LineBreakClass.Numeric:
                                            {
                                                return RuleResult.NoBreak;
                                            }
                                        case LineBreakClass.BreakSymbols:
                                        case LineBreakClass.InfixNumeric:
                                            {
                                                if (state.Previous.LineBreakClass == LineBreakClass.Numeric)
                                                {
                                                    return RuleResult.NoBreak;
                                                }

                                                break;
                                            }
                                    }

                                    break;
                                }
                        }

                        break;
                    }
                // [25.15] NU(SY|IS)* ×	NU
                case LineBreakClass.Numeric:
                    {
                        switch (state.Current.LineBreakClass)
                        {
                            case LineBreakClass.Numeric:
                                {
                                    return RuleResult.NoBreak;
                                }
                            case LineBreakClass.BreakSymbols:
                            case LineBreakClass.InfixNumeric:
                                {
                                    if (state.Previous.LineBreakClass == LineBreakClass.Numeric)
                                    {
                                        return RuleResult.NoBreak;
                                    }

                                    break;
                                }
                        }

                        break;
                    }

                case LineBreakClass.PostfixNumeric:
                    {
                        switch (state.Current.LineBreakClass)
                        {
                            // [25.01] NU(SY|IS)* CL × PO
                            case LineBreakClass.ClosePunctuation:
                                {
                                    switch (state.Previous.LineBreakClass)
                                    {
                                        case LineBreakClass.Numeric:
                                            {
                                                return RuleResult.NoBreak;
                                            }
                                        case LineBreakClass.BreakSymbols:
                                        case LineBreakClass.InfixNumeric:
                                            {
                                                if (state.Previous.LineBreakClass == LineBreakClass.Numeric)
                                                {
                                                    return RuleResult.NoBreak;
                                                }

                                                break;
                                            }
                                    }

                                    break;
                                }
                            // [25.05] NU(SY|IS)* ×	PO
                            case LineBreakClass.Numeric:
                                {
                                    return RuleResult.NoBreak;
                                }
                            case LineBreakClass.BreakSymbols:
                            case LineBreakClass.InfixNumeric:
                                {
                                    if (state.Previous.LineBreakClass == LineBreakClass.Numeric)
                                    {
                                        return RuleResult.NoBreak;
                                    }

                                    break;
                                }
                        }

                        break;
                    }
            }

            if (state.Current.LineBreakClass == LineBreakClass.PrefixNumeric)
            {
                switch (state.Next(text).LineBreakClass)
                {
                    case LineBreakClass.OpenPunctuation:
                        {
                            var afterNext = LineBreakState.After(text, state.Next(text));

                            // [25.1] PR × OP NU
                            if (afterNext.LineBreakClass == LineBreakClass.Numeric)
                            {
                                return RuleResult.NoBreak;
                            }

                            // PR × OP IS NU
                            if (afterNext.LineBreakClass == LineBreakClass.InfixNumeric && LineBreakState.After(text, afterNext).LineBreakClass == LineBreakClass.Numeric)
                            {
                                return RuleResult.NoBreak;
                            }

                            break;
                        }
                    // PR × NU
                    case LineBreakClass.Numeric:
                        {
                            return RuleResult.NoBreak;
                        }
                }
            }

            if (state.Current.LineBreakClass == LineBreakClass.PostfixNumeric)
            {
                switch (state.Next(text).LineBreakClass)
                {
                    case LineBreakClass.OpenPunctuation:
                        {
                            var afterNext = LineBreakState.After(text, state.Next(text));

                            // PO × OP NU
                            if (afterNext.LineBreakClass == LineBreakClass.Numeric)
                            {
                                return RuleResult.NoBreak;
                            }

                            // PO × OP IS NU
                            if (afterNext.LineBreakClass == LineBreakClass.InfixNumeric && LineBreakState.After(text, afterNext).LineBreakClass == LineBreakClass.Numeric)
                            {
                                return RuleResult.NoBreak;
                            }

                            break;
                        }
                    // PO × NU
                    case LineBreakClass.Numeric:
                        {
                            return RuleResult.NoBreak;
                        }
                }
            }

            switch (state.Current.LineBreakClass)
            {
                // HY × NU
                // [25.14] IS × NU
                case LineBreakClass.Hyphen:
                case LineBreakClass.InfixNumeric:
                    {
                        if (state.Next(text).LineBreakClass == LineBreakClass.Numeric)
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
            switch (state.Current.LineBreakClass)
            {
                case LineBreakClass.JL:
                    {
                        // JL × (JL | JV | H2 | H3)
                        switch (state.Next(text).LineBreakClass)
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
                        switch (state.Next(text).LineBreakClass)
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
                        if (state.Next(text).LineBreakClass == LineBreakClass.JT)
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
            switch (state.Current.LineBreakClass)
            {
                case LineBreakClass.JL:
                case LineBreakClass.JV:
                case LineBreakClass.JT:
                case LineBreakClass.H2:
                case LineBreakClass.H3:
                    {
                        // (JL | JV | JT | H2 | H3) × PO
                        if (state.Next(text).LineBreakClass == LineBreakClass.PostfixNumeric)
                        {
                            return RuleResult.NoBreak;
                        }
                        break;
                    }
                case LineBreakClass.PrefixNumeric:
                    {
                        // PR × (JL | JV | JT | H2 | H3)
                        switch (state.Next(text).LineBreakClass)
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
            // [28.0] (AL | HL) × (AL | HL)
            switch (state.Current.LineBreakClass)
            {
                case LineBreakClass.Alphabetic:
                case LineBreakClass.HebrewLetter:
                    {
                        switch (state.Next(text).LineBreakClass)
                        {
                            case LineBreakClass.Alphabetic:
                            case LineBreakClass.HebrewLetter:
                                {
                                    return RuleResult.NoBreak;
                                }
                        }

                        break;
                    }
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB28a: Do not break inside the orthographic syllables of Brahmic scripts.
        /// </summary>
        private static RuleResult LB28a(ReadOnlySpan<char> text, LineBreakState state)
        {
            // [28.11] AP × (AK | DottedCircle | AS)
            if ((state.Current.LineBreakClass == LineBreakClass.AksaraPrebase) && isMatch(state.Next(text)))
            {
                return RuleResult.NoBreak;
            }

            // [28.12] (AK | DottedCircle | AS) × (VF | VI)
            if (isMatch(state.LastBeforeWhitespace)
                && ((state.Next(text).LineBreakClass == LineBreakClass.ViramaFinal) || (state.Next(text).LineBreakClass == LineBreakClass.Virama)))
            {
                return RuleResult.NoBreak;
            }

            // [28.13] (AK | DottedCircle| AS) VI × (AK | DottedCircle)
            if (isMatch(state.Previous)
                && state.Current.LineBreakClass == LineBreakClass.Virama
                && ((state.Next(text).LineBreakClass == LineBreakClass.Aksara) || (state.Next(text).Codepoint == DotCircle)))
            {
                return RuleResult.NoBreak;
            }

            // [28.14] (AK | DottedCircle | AS) × (AK | DottedCircle | AS) VF
            if (isMatch(state.LastBeforeWhitespace)
                && isMatch(state.Next(text))
                && (LineBreakState.After(text, state.Next(text)).LineBreakClass == LineBreakClass.ViramaFinal))
            {
                return RuleResult.NoBreak;
            }

            return RuleResult.Pass;

            // (AK | DottedCircle | AS)
            static bool isMatch(BreakUnit chr)
            {
                return (chr.LineBreakClass == LineBreakClass.Aksara) || (chr.Codepoint == DotCircle) || (chr.LineBreakClass == LineBreakClass.AksaraStart);
            }
        }

        /// <summary>
        /// LB29: Do not break between numeric punctuation and alphabetics (“e.g.”).
        /// </summary>
        private static RuleResult LB29(ReadOnlySpan<char> text, LineBreakState state)
        {
            // IS × (AL | HL)
            if ((state.Current.LineBreakClass == LineBreakClass.InfixNumeric)
              && ((state.Next(text).LineBreakClass == LineBreakClass.Alphabetic) || (state.Next(text).LineBreakClass == LineBreakClass.HebrewLetter)))
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
            switch (state.Current.LineBreakClass)
            {
                case LineBreakClass.Alphabetic:
                case LineBreakClass.HebrewLetter:
                case LineBreakClass.Numeric:
                    {
                        var next = state.Next(text);

                        // (AL | HL | NU) × [OP-[\p{ea=F}\p{ea=W}\p{ea=H}]]
                        if ((next.LineBreakClass == LineBreakClass.OpenPunctuation) && !next.Codepoint.IsEastAsian)
                        {
                            return RuleResult.NoBreak;
                        }
                        break;
                    }

                case LineBreakClass.CloseParenthesis:
                    // [CP-[\p{ea=F}\p{ea=W}\p{ea=H}]] × (AL | HL | NU)
                    if (!state.Current.Codepoint.IsEastAsian)
                    {
                        switch (state.Next(text).LineBreakClass)
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
            if (state.RegionalIndicator > 0 && state.Next(text).LineBreakClass == LineBreakClass.RegionalIndicator)
            {
                if (state.RegionalIndicator + 1 == 2)
                {
                    return RuleResult.NoBreak;
                }
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
            if ((state.Current.LineBreakClass == LineBreakClass.EBase) && (state.Next(text).LineBreakClass == LineBreakClass.EModifier))
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
            if (state.Next(text).LineBreakClass == LineBreakClass.EModifier &&
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

        private enum RuleResult
        {
            Pass,
            NoBreak,
            MayBreak,
            MustBreak
        }

        private readonly struct BreakUnit
        {
            public BreakUnit()
            {
                LineBreakClass = LineBreakClass.Unknown;
            }

            public BreakUnit(BreakUnit other, LineBreakClass lineBreakClass)
            {
                Codepoint = other.Codepoint;
                Start = other.Start;
                Length = other.Length;
                LineBreakClass = lineBreakClass;
                EndOfText = other.EndOfText;
            }

            public BreakUnit(Codepoint codepoint, int start, int length)
            {
                Codepoint = codepoint;
                Start = start;
                Length = length;
                LineBreakClass = MapClass(codepoint);
            }

            public int Start { get; }
            public int Length { get; }
            public Codepoint Codepoint { get; }
            public bool EndOfText { get; init; }
            public bool StartOfText { get; init; }
            public LineBreakClass LineBreakClass { get; init; }
            public bool Ignored { get; init; }
            public bool Inherited { get; init; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static LineBreakClass MapClass(Codepoint cp)
            {
                if (cp.Value == 327685)
                {
                    return LineBreakClass.Alphabetic;
                }

                if (cp.Value == 327685)
                {
                    return LineBreakClass.Alphabetic;
                }

                // LB 1
                // ==========================================
                // Resolved Original    General_Category
                // ==========================================
                // AL       AI, SG, XX  Any
                // CM       SA          Only Mn or Mc
                // AL       SA          Any except Mn and Mc
                // NS       CJ          Any
                var cls = cp.LineBreakClass;

                const ulong specialMask =
                    (1UL << (int)LineBreakClass.Ambiguous) |
                    (1UL << (int)LineBreakClass.Surrogate) |
                    (1UL << (int)LineBreakClass.Unknown) |
                    (1UL << (int)LineBreakClass.ComplexContext) |
                    (1UL << (int)LineBreakClass.ConditionalJapaneseStarter);

                if (((1UL << (int)cls) & specialMask) != 0UL)
                {
                    switch (cls)
                    {
                        case LineBreakClass.Ambiguous:
                        case LineBreakClass.Surrogate:
                        case LineBreakClass.Unknown:
                            return LineBreakClass.Alphabetic;

                        case LineBreakClass.ComplexContext:
                            return cp.GeneralCategory is GeneralCategory.NonspacingMark or GeneralCategory.SpacingMark
                                ? LineBreakClass.CombiningMark
                                : LineBreakClass.Alphabetic;

                        case LineBreakClass.ConditionalJapaneseStarter:
                            return LineBreakClass.Nonstarter;
                    }
                }

                return cls;
            }
        }

        private class LineBreakState
        {
            private BreakUnit? _next;
            private BreakUnit _previous;

            public LineBreakState()
            {
                _next = null;

                _previous = s_sot;
                Current = s_sot;
                LastBeforeSpace = s_sot;
                LastBeforeWhitespace = s_sot;
            }

            public BreakUnit Current { get; set; }

            public BreakUnit Previous
            {
                get
                {
                    if (_previous.Ignored || _previous.Inherited)
                    {
                        _previous = LastBeforeWhitespace;
                    }

                    return _previous;
                }
            }

            public BreakUnit Next(ReadOnlySpan<char> text)
            {
                return _next ??= Peek(text);
            }

            public static BreakUnit After(ReadOnlySpan<char> text, BreakUnit current)
            {
                if (current.EndOfText)
                {
                    return s_eot;
                }

                return PeekAt(text, current.Start + current.Length);
            }

            public static BreakUnit Before(ReadOnlySpan<char> text, BreakUnit current)
            {
                if (current.StartOfText)
                {
                    return s_sot;
                }

                var position = current.Start - 1;

                var unit = PeekAt(text, position);

                return unit;
            }

            public void IgnoreNext(ReadOnlySpan<char> text)
            {
                _next = Next(text) with { Ignored = true };
            }

            public void ReplaceNext(BreakUnit next)
            {
                _next = next;   
            }

            public int Position { get; private set; }

            public int RegionalIndicator { get; set; }

            public int Quotation { get; set; }

            public BreakUnit LastBeforeWhitespace { get; set; }

            public BreakUnit LastBeforeSpace { get; set; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static BreakUnit PeekAt(ReadOnlySpan<char> text, int index)
            {
                if (text.Length == 0)
                {
                    return new BreakUnit(Codepoint.ReplacementCodepoint, index, 0)
                    {
                        StartOfText = true
                    };
                }

                if (index >= text.Length)
                {
                    return s_eot;
                }

                var codepoint = Codepoint.ReadAt(text, index, out var count);

                return new BreakUnit(codepoint, index, count)
                {
                    EndOfText = index + count == text.Length,
                    StartOfText = index == 0
                };
            }

            public BreakUnit Peek(ReadOnlySpan<char> text)
            {
                return PeekAt(text, Position);
            }

            public BreakUnit Read(ReadOnlySpan<char> text)
            {
                _previous = Current;

                var next = Next(text);

                if (!next.Codepoint.IsWhiteSpace)
                {
                    LastBeforeWhitespace = next;
                }

                if (next.LineBreakClass != LineBreakClass.Space)
                {
                    LastBeforeSpace = next;
                }

                Current = next;

                Position += next.Length;

                _next = null;

                if (Current.Ignored)
                {
                    Current = Current with { LineBreakClass = Previous.LineBreakClass, Inherited = true };
                }

                return next;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static LineBreakClass ClassAfterSpaces(ReadOnlySpan<char> text, BreakUnit current)
            {
                var position = current.Start + current.Length;

                if (position >= text.Length)
                {
                    return current.LineBreakClass;
                }

                var enumerator = new CodepointEnumerator(text.Slice(position));

                Codepoint cp;

                while (enumerator.MoveNext(out cp) && cp.LineBreakClass == LineBreakClass.Space)
                { }

                return cp.LineBreakClass;
            }
        }

        private delegate RuleResult BreakUnitDelegate(ReadOnlySpan<char> text, LineBreakState state);

        private static readonly BreakUnitDelegate[] s_rules = [
            QuotationAndRegionalIndicator,
            LB03,
            LB04,
            LB05,
            LB06,
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
            LB15a,
            LB15b,
            LB15c,
            LB15d,
            LB16,
            LB17,
            LB18,
            LB19,
            LB20,
            LB20a,
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
