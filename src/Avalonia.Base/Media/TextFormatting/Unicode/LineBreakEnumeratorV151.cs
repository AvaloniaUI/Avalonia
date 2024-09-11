using System;

namespace Avalonia.Media.TextFormatting.Unicode
{
    internal ref struct LineBreakEnumeratorV151
    {
        private LineBreakState state = new LineBreakState();

        public LineBreakEnumeratorV151()
        {
        }

        /// <summary>
        /// LB2: Never break at the start of text.
        /// </summary>
        private RuleResult LB02()
        {
            // sot ×
            if (state.cur.Start == 0 && state.next.eot)
            {
                return RuleResult.NoBreak;
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB3: Always break at the end of text.
        /// </summary>
        private RuleResult LB03()
        {
            if (state.next.eot && (state.cur.Length == 0 || state.cur.Length != state.PreviousChunk))
            {
                return RuleResult.MustBreak;
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB4: Always break after hard line breaks.
        /// </summary>
        private RuleResult LB04()
        {
            // BK !
            if (state.cur.cls == LineBreakClass.MandatoryBreak)
            {
                return RuleResult.MustBreak;
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB5: Treat CR followed by LF, as well as CR, LF, and NL as hard line
        /// </summary>
        private RuleResult LB05()
        {
            switch (state.cur.cls)
            {
                case LineBreakClass.CarriageReturn:
                    if (state.next.cls == LineBreakClass.LineFeed)
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
        private RuleResult LB06()
        {
            // × ( BK | CR | LF | NL )
            if (IsBreakClass(state.next.cls))
            {
                return RuleResult.NoBreak;
            }

            return RuleResult.Pass;
        }

        private RuleResult EndOfSpaces()
        {
            if (state.cur.cls != LineBreakClass.RegionalIndicator)
            {
                state.RegionalIndicator = false;
            }

            if (!state.spaces)
            {
                return RuleResult.Pass;
            }

            if (state.next.cls != LineBreakClass.Space)
            {
                state.spaces = false;
            }

            return RuleResult.NoBreak;

        }

        /// <summary>
        /// LB7: Do not break before spaces or zero width space.
        /// </summary>
        private RuleResult LB07()
        {
            // × ZW
            if (state.next.cls == LineBreakClass.ZWSpace)
            {
                return RuleResult.NoBreak;
            }

            // × SP
            if (state.next.cls == LineBreakClass.Space)
            {
                switch (state.cur.cls)
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
        private RuleResult LB08()
        {
            // LB8 is special, because it breaks after the run of spaces, unlike
            // the .spaces cases, which NO_BREAK after the rub of spaces.

            // ZW SP* ÷
            if (state.LB8)
            {
                // Assume state.next.cls !== SP, because LB7
                state.LB8 = false;

                return RuleResult.MayBreak;
            }

            if (state.cur.cls == LineBreakClass.ZWSpace)
            {
                if (state.next.cls == LineBreakClass.Space)
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
        private RuleResult LB08a()
        {
            // ZWJ ×
            if (state.cur.cls == LineBreakClass.ZWJ)
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
        private RuleResult LB09()
        {
            // Treat X (CM | ZWJ)* as if it were X.
            // where X is any line break class except BK, CR, LF, NL, SP, or ZW.

            var cls = state.cur.cls;

            if (IsBreakClass(cls) || cls == LineBreakClass.Space || cls == LineBreakClass.ZWSpace)
            {
                return RuleResult.Pass;
            }

            switch (state.next.cls)
            {
                case LineBreakClass.CombiningMark:
                case LineBreakClass.ZWJ:
                    {
                        state.next.Ignored = true;

                        return RuleResult.NoBreak;
                    }
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB10: Treat any remaining combining mark or ZWJ as AL.
        /// </summary>
        private RuleResult LB10()
        {
            if (state.cur.cls == LineBreakClass.CombiningMark)
            {
                state.cur.cls = LineBreakClass.Alphabetic;
            }

            if (state.next.cls == LineBreakClass.CombiningMark)
            {
                state.next.cls = LineBreakClass.Alphabetic;
            }
            return RuleResult.Pass;
        }

        /// <summary>
        /// LB11: Do not break before or after Word joiner and related characters.
        /// </summary>
        private RuleResult LB11()
        {
            if (state.next.cls == LineBreakClass.WordJoiner /* × WJ */
                || state.cur.cls == LineBreakClass.WordJoiner /* WJ × */)
            {
                return RuleResult.NoBreak;
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB12: Do not break after NBSP and related characters.
        /// </summary>
        private RuleResult LB12()
        {
            // GL ×
            if (state.cur.cls == LineBreakClass.Glue)
            {
                return RuleResult.NoBreak;
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB12a: Do not break before NBSP and related characters, except after spaces and hyphens.
        /// </summary>
        private RuleResult LB12a()
        {
            // [^SP BA HY] × GL
            if (state.next.cls == LineBreakClass.Glue)
            {
                switch (state.cur.cls)
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
        private RuleResult LB13()
        {
            // × CL (e.g.)
            switch (state.next.cls)
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
        private RuleResult LB14()
        {
            // OP SP* ×
            if (state.cur.cls == LineBreakClass.OpenPunctuation)
            {
                if (state.next.cls == LineBreakClass.Space)
                {
                    state.spaces = true;
                }

                return RuleResult.NoBreak;
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB15a: Do not break after an unresolved initial punctuation that lies at the start of the line,
        /// after a space, after opening punctuation, or after an unresolved quotation mark, even after spaces.
        /// </summary>
        private RuleResult LB15a()
        {
            // (sot | BK | CR | LF | NL | OP | QU | GL | SP | ZW) [\p{Pi}&QU] SP* ×
            if (IsMatch(state.prev) &&
                state.cur.Codepoint.GeneralCategory == GeneralCategory.InitialPunctuation &&
                state.cur.cls == LineBreakClass.Quotation)
            {
                state.spaces = true;

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
        private RuleResult Lb15b()
        {
            // × [\p{Pf}&QU] ( SP | GL | WJ | CL | QU | CP | EX | IS | SY | BK | CR | LF | NL | ZW | eot)
            if (state.next.Codepoint.GeneralCategory == GeneralCategory.FinalPunctuation && (state.next.cls == LineBreakClass.Quotation))
            {
                var after = state.AfterNext;

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
        private RuleResult LB16()
        {
            // (CL | CP) SP* × NS
            if ((state.cur.cls == LineBreakClass.ClosePunctuation) || (state.cur.cls == LineBreakClass.CloseParenthesis))
            {
                if (state.ClassAfterSpaces(state.cur) == LineBreakClass.Nonstarter)
                {
                    if (state.next.cls == LineBreakClass.Space)
                    {
                        state.spaces = true;
                    }

                    return RuleResult.NoBreak;
                }
                if (state.next.cls == LineBreakClass.Space)
                {
                    return RuleResult.NoBreak; // LB7
                }
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB17: Do not break within ‘——’, even with intervening spaces.
        /// </summary>
        private RuleResult LB17()
        {
            // B2 SP* × B2
            if (state.cur.cls == LineBreakClass.BreakBoth)
            {
                if (state.ClassAfterSpaces(state.cur) == LineBreakClass.BreakBoth)
                {
                    if (state.next.cls != LineBreakClass.Space)
                    {
                        return RuleResult.NoBreak; // LB7
                    }

                    state.spaces = true;

                    return RuleResult.NoBreak;
                }

                if (state.next.cls == LineBreakClass.Space)
                {
                    return RuleResult.NoBreak; // LB7
                }
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB18: Break after spaces.
        /// </summary>
        private RuleResult LB18()
        {
            // SP ÷
            if (state.cur.cls == LineBreakClass.Space)
            {
                return RuleResult.MayBreak;
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB19: Do not break before or after quotation marks, such as ‘ ” ’.
        /// </summary>
        private RuleResult LB19()
        {
            // × QU
            // QU ×
            if ((state.cur.cls == LineBreakClass.Quotation) || (state.next.cls == LineBreakClass.Quotation))
            {
                return RuleResult.NoBreak;
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB20: Break before and after unresolved CB.
        /// </summary>
        private RuleResult LB20()
        {
            // ÷ CB
            // CB ÷
            if ((state.cur.cls == LineBreakClass.ContingentBreak) || (state.next.cls == LineBreakClass.ContingentBreak))
            {
                return RuleResult.MayBreak;
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB21: Do not break before hyphen-minus, other hyphens, fixed-width spaces, small kana, and other non-starters, or after acute accents.
        /// </summary>
        private RuleResult LB21()
        {
            // BB ×
            if (state.cur.cls == LineBreakClass.BreakBefore)
            {
                return RuleResult.NoBreak;
            }

            // × (BA | HY | NS)
            switch (state.next.cls)
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
        private RuleResult LB21a()
        {
            // HL (HY | BA) ×
            if (state.prev.cls == LineBreakClass.HebrewLetter
                && state.cur.cls is LineBreakClass.Hyphen or LineBreakClass.BreakAfter)
            {
                return RuleResult.NoBreak;
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB21b: Don’t break between Solidus and Hebrew letters.
        /// </summary>
        private RuleResult LB21b()
        {
            // SY × HL
            if ((state.cur.cls == LineBreakClass.BreakSymbols) && (state.next.cls == LineBreakClass.HebrewLetter))
            {
                return RuleResult.NoBreak;
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB22: Do not break before ellipses.
        /// </summary>
        private RuleResult LB22()
        {
            // × IN
            if (state.next.cls == LineBreakClass.Inseparable)
            {
                return RuleResult.NoBreak;
            }

            return RuleResult.Pass;
        }

        /// <summary>
        /// LB23: Do not break between digits and letters.
        /// </summary>
        private RuleResult LB23()
        {
            switch (state.cur.cls)
            {
                case LineBreakClass.Alphabetic:
                case LineBreakClass.HebrewLetter:
                    {
                        // (AL | HL) × NU
                        if (state.next.cls == LineBreakClass.Numeric)
                        {
                            return RuleResult.NoBreak;
                        }

                        break;
                    }

                case LineBreakClass.Numeric:
                    {
                        // NU × (AL | HL)
                        if (state.next.cls is LineBreakClass.Alphabetic or LineBreakClass.HebrewLetter)
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
        private RuleResult LB23a()
        {
            // PR × (ID | EB | EM)
            if ((state.cur.cls == LineBreakClass.PrefixNumeric)
                && IsMatch(state.next.cls))
            {
                return RuleResult.NoBreak;
            }

            // (ID | EB | EM) × PO
            if ((state.next.cls == LineBreakClass.PostfixNumeric)
                && IsMatch(state.cur.cls))
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
        private RuleResult LB24()
        {
            // (PR | PO) × (AL | HL)
            if (state.cur.cls is LineBreakClass.PrefixNumeric or LineBreakClass.PostfixNumeric
                && state.next.cls is LineBreakClass.Alphabetic or LineBreakClass.HebrewLetter)
            {
                return RuleResult.NoBreak;
            }
            // (AL | HL) × (PR | PO)
            if (state.cur.cls is LineBreakClass.Alphabetic or LineBreakClass.HebrewLetter
                && state.next.cls is LineBreakClass.PrefixNumeric or LineBreakClass.PostfixNumeric)
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
        private RuleResult LB25()
        {
            switch (state.cur.cls)
            {
                case LineBreakClass.Numeric:
                    {
                        // NU × PO
                        // NU × PR
                        // NU × NU
                        if (state.next.cls is LineBreakClass.PostfixNumeric or LineBreakClass.PrefixNumeric or LineBreakClass.Numeric)
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
                        if (state.next.cls is LineBreakClass.PostfixNumeric or LineBreakClass.PrefixNumeric)
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
                        if (state.next.cls is LineBreakClass.OpenPunctuation or LineBreakClass.Numeric)
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
                        if (state.next.cls == LineBreakClass.Numeric)
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
        /// <returns></returns>
        private RuleResult LB26()
        {
            switch (state.cur.cls)
            {
                case LineBreakClass.JL:
                    {
                        // JL × (JL | JV | H2 | H3)
                        switch (state.next.cls)
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
                        switch (state.next.cls)
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
                        if (state.next.cls == LineBreakClass.JT)
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
        private RuleResult LB27()
        {
            switch (state.cur.cls)
            {
                case LineBreakClass.JL:
                case LineBreakClass.JV:
                case LineBreakClass.JT:
                case LineBreakClass.H2:
                case LineBreakClass.H3:
                    {
                        // (JL | JV | JT | H2 | H3) × PO
                        if (state.next.cls == LineBreakClass.PostfixNumeric)
                        {
                            return RuleResult.NoBreak;
                        }
                        break;
                    }
                case LineBreakClass.PrefixNumeric:
                    {
                        // PR × (JL | JV | JT | H2 | H3)
                        switch (state.next.cls)
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

        public enum RuleResult
        {
            Pass,
            NoBreak,
            MayBreak,
            MustBreak
        }

        public class BreakUnit
        {
            public int Start { get; init; }
            public int Length { get; init; }
            public Codepoint Codepoint { get; init; }
            public LineBreakClass cls { get; set; }
            public bool eot { get; init; }
            public bool sot { get; init; }
            public bool Ignored { get; set; }
        }

        public class LineBreakState
        {
            public BreakUnit prev { get; set; }

            public BreakUnit cur { get; set; }

            public BreakUnit next { get; set; }

            public int PreviousChunk { get; set; }

            public bool RegionalIndicator { get; set; }

            public bool spaces { get; set; }

            public bool LB8 { get; set; }
            public BreakUnit? AfterNext { get; set; }

            public LineBreakClass ClassAfterSpaces(BreakUnit unit)
            {
                throw new NotImplementedException();
            }
        }
    }
}
