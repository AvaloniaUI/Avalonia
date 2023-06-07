// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.
// Ported from: https://github.com/SixLabors/Fonts/

using System;
using System.Runtime.CompilerServices;

namespace Avalonia.Media.TextFormatting.Unicode
{
    /// <summary>
    /// Implementation of the Unicode Line Break Algorithm. UAX:14
    /// <see href="https://www.unicode.org/reports/tr14/tr14-37.html"/>
    /// </summary>
    public ref struct LineBreakEnumerator
    {
        private readonly ReadOnlySpan<char> _text;
        private int _position;
        private int _lastPosition;
        private LineBreakClass _currentClass;
        private LineBreakClass _nextClass;
        private bool _first;
        private int _alphaNumericCount;
        private bool _lb8a;
        private bool _lb21a;
        private bool _lb22ex;
        private bool _lb24ex;
        private bool _lb25ex;
        private bool _lb30;
        private int _lb30a;
        private bool _lb31;

        public LineBreakEnumerator(ReadOnlySpan<char> text)
            : this()
        {
            _text = text;
            _position = 0;
            _currentClass = LineBreakClass.Unknown;
            _nextClass = LineBreakClass.Unknown;
            _first = true;
            _lb8a = false;
            _lb21a = false;
            _lb22ex = false;
            _lb24ex = false;
            _lb25ex = false;
            _alphaNumericCount = 0;
            _lb31 = false;
            _lb30 = false;
            _lb30a = 0;
        }

        public bool MoveNext(out LineBreak lineBreak)
        {
            // Get the first char if we're at the beginning of the string.
            if (_first)
            {
                var firstClass = NextCharClass();
                _first = false;
                _currentClass = MapFirst(firstClass);
                _nextClass = firstClass;
                _lb8a = firstClass == LineBreakClass.ZWJ;
                _lb30a = 0;
            }

            while (_position < _text.Length)
            {
                _lastPosition = _position;
                var lastClass = _nextClass;
                _nextClass = NextCharClass();

                // Explicit newline
                switch (_currentClass)
                {
                    case LineBreakClass.MandatoryBreak:
                    case LineBreakClass.CarriageReturn when _nextClass != LineBreakClass.LineFeed:
                    {
                        _currentClass = MapFirst(_nextClass);
                        lineBreak = new LineBreak(FindPriorNonWhitespace(_lastPosition), _lastPosition, true);
                        return true;
                    }
                }

                var shouldBreak = GetSimpleBreak() ?? GetPairTableBreak(lastClass);

                // Rule LB8a
                _lb8a = _nextClass == LineBreakClass.ZWJ;

                if (shouldBreak)
                {
                    lineBreak = new LineBreak(FindPriorNonWhitespace(_lastPosition), _lastPosition);
                    return true;
                }
            }

            if (_position >= _text.Length)
            {
                if (_lastPosition < _text.Length)
                {
                    _lastPosition = _text.Length;

                    var required = false;

                    switch (_currentClass)
                    {
                        case LineBreakClass.MandatoryBreak:
                        case LineBreakClass.CarriageReturn when _nextClass != LineBreakClass.LineFeed:
                            required = true;
                            break;
                    }

                    lineBreak = new LineBreak(FindPriorNonWhitespace(_lastPosition), _lastPosition, required);
                    return true;
                }
            }

            lineBreak = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static LineBreakClass MapClass(Codepoint cp)
        {
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static LineBreakClass MapFirst(LineBreakClass c)
        {
            switch (c)
            {
                case LineBreakClass.LineFeed:
                case LineBreakClass.NextLine:
                    return LineBreakClass.MandatoryBreak;

                case LineBreakClass.Space:
                    return LineBreakClass.WordJoiner;

                default:
                    return c;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsAlphaNumeric(LineBreakClass cls)
        {
            const ulong mask =
                (1UL << (int)LineBreakClass.Alphabetic) |
                (1UL << (int)LineBreakClass.HebrewLetter) |
                (1UL << (int)LineBreakClass.Numeric);

            return ((1UL << (int)cls) & mask) != 0UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsPrefixPostfixNumericOrSpace(LineBreakClass cls)
        {
            const ulong mask =
                (1UL << (int)LineBreakClass.PostfixNumeric) |
                (1UL << (int)LineBreakClass.PrefixNumeric) |
                (1UL << (int)LineBreakClass.Space);

            return ((1UL << (int)cls) & mask) != 0UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsPrefixPostfixNumeric(LineBreakClass cls)
        {
            const ulong mask =
                (1UL << (int)LineBreakClass.PostfixNumeric) |
                (1UL << (int)LineBreakClass.PrefixNumeric);

            return ((1UL << (int)cls) & mask) != 0UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsClosePunctuationOrParenthesis(LineBreakClass cls)
        {
            const ulong mask =
                (1UL << (int)LineBreakClass.ClosePunctuation) |
                (1UL << (int)LineBreakClass.CloseParenthesis);

            return ((1UL << (int)cls) & mask) != 0UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsClosePunctuationOrInfixNumericOrBreakSymbols(LineBreakClass cls)
        {
            const ulong mask =
                (1UL << (int)LineBreakClass.ClosePunctuation) |
                (1UL << (int)LineBreakClass.InfixNumeric) |
                (1UL << (int)LineBreakClass.BreakSymbols);

            return ((1UL << (int)cls) & mask) != 0UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsSpaceOrWordJoinerOrAlphabetic(LineBreakClass cls)
        {
            const ulong mask =
                (1UL << (int)LineBreakClass.Space) |
                (1UL << (int)LineBreakClass.WordJoiner) |
                (1UL << (int)LineBreakClass.Alphabetic);

            return ((1UL << (int)cls) & mask) != 0UL;
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

        private LineBreakClass PeekNextCharClass()
        {
            var cp = Codepoint.ReadAt(_text, _position, out _);
            
            return MapClass(cp);
        }

        // Get the next character class
        private LineBreakClass NextCharClass()
        {
            var cp = Codepoint.ReadAt(_text, _position, out var count);
            var cls = MapClass(cp);
            _position += count;

            // Keep track of alphanumeric + any combining marks.
            // This is used for LB22 and LB30.
            if (IsAlphaNumeric(_currentClass) || _alphaNumericCount > 0 && cls == LineBreakClass.CombiningMark)
            {
                _alphaNumericCount++;
            }

            // Track combining mark exceptions. LB22
            if (cls == LineBreakClass.CombiningMark)
            {
                const ulong lb22ExMask =
                    (1UL << (int)LineBreakClass.MandatoryBreak) |
                    (1UL << (int)LineBreakClass.ContingentBreak) |
                    (1UL << (int)LineBreakClass.Exclamation) |
                    (1UL << (int)LineBreakClass.LineFeed) |
                    (1UL << (int)LineBreakClass.NextLine) |
                    (1UL << (int)LineBreakClass.Space) |
                    (1UL << (int)LineBreakClass.ZWSpace) |
                    (1UL << (int)LineBreakClass.CarriageReturn);

                if (((1UL << (int)_currentClass) & lb22ExMask) != 0UL)
                {
                    _lb22ex = true;
                }

                const ulong lb31Mask =
                    (1UL << (int)LineBreakClass.MandatoryBreak) |
                    (1UL << (int)LineBreakClass.ContingentBreak) |
                    (1UL << (int)LineBreakClass.Exclamation) |
                    (1UL << (int)LineBreakClass.LineFeed) |
                    (1UL << (int)LineBreakClass.NextLine) |
                    (1UL << (int)LineBreakClass.Space) |
                    (1UL << (int)LineBreakClass.ZWSpace) |
                    (1UL << (int)LineBreakClass.CarriageReturn) |
                    (1UL << (int)LineBreakClass.ZWJ);

                // Track combining mark exceptions. LB31
                if (_first || ((1UL << (int)_currentClass) & lb31Mask) != 0UL)
                {
                    _lb31 = true;
                }
            }

            if (_first)
            {
                // Rule LB24
                if (IsClosePunctuationOrParenthesis(cls))
                {
                    _lb24ex = true;
                }

                // Rule LB25
                if (IsClosePunctuationOrInfixNumericOrBreakSymbols(cls))
                {
                    _lb25ex = true;
                }

                if (IsPrefixPostfixNumericOrSpace(cls))
                {
                    _lb31 = true;
                }
            }

            if (_currentClass == LineBreakClass.Alphabetic && IsPrefixPostfixNumericOrSpace(cls))
            {
                _lb31 = true;
            }

            // Reset LB31 if next is U+0028 (Left Opening Parenthesis)
            if (_lb31
                && !IsPrefixPostfixNumeric(_currentClass)
                && cls == LineBreakClass.OpenPunctuation
                && cp.Value == 0x0028)
            {
                _lb31 = false;
            }

            if (IsSpaceOrWordJoinerOrAlphabetic(cls))
            {
                var next = PeekNextCharClass();
                if (IsClosePunctuationOrInfixNumericOrBreakSymbols(next))
                {
                    _lb25ex = true;
                }
            }

            // AlphaNumeric + and combining marks can break for OP except.
            // - U+0028 (Left Opening Parenthesis)
            // - U+005B (Opening Square Bracket)
            // - U+007B (Left Curly Bracket)
            // See custom columns|rules in the text pair table.
            // https://www.unicode.org/Public/13.0.0/ucd/auxiliary/LineBreakTest.html
            _lb30 = _alphaNumericCount > 0
                && cls == LineBreakClass.OpenPunctuation
                && cp.Value != 0x0028
                && cp.Value != 0x005B
                && cp.Value != 0x007B;

            return cls;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool? GetSimpleBreak()
        {
            // handle classes not handled by the pair table
            switch (_nextClass)
            {
                case LineBreakClass.Space:
                    return false;

                case LineBreakClass.MandatoryBreak:
                case LineBreakClass.LineFeed:
                case LineBreakClass.NextLine:
                    _currentClass = LineBreakClass.MandatoryBreak;
                    return false;

                case LineBreakClass.CarriageReturn:
                    _currentClass = LineBreakClass.CarriageReturn;
                    return false;
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // quite long but only one usage
        private bool GetPairTableBreak(LineBreakClass lastClass)
        {
            // If not handled already, use the pair table
            bool shouldBreak = false;
            switch (LineBreakPairTable.Table[(int)_currentClass][(int)_nextClass])
            {
                case LineBreakPairTable.DIBRK: // Direct break
                    shouldBreak = true;
                    break;

                // TODO: Rewrite this so that it defaults to true and rules are set as exceptions.
                case LineBreakPairTable.INBRK: // Possible indirect break

                    // LB31
                    if (_lb31 && _nextClass == LineBreakClass.OpenPunctuation)
                    {
                        shouldBreak = true;
                        _lb31 = false;
                        break;
                    }

                    // LB30
                    if (_lb30)
                    {
                        shouldBreak = true;
                        _lb30 = false;
                        _alphaNumericCount = 0;
                        break;
                    }

                    // LB25
                    if (_lb25ex && (_nextClass == LineBreakClass.PrefixNumeric || _nextClass == LineBreakClass.Numeric))
                    {
                        shouldBreak = true;
                        _lb25ex = false;
                        break;
                    }

                    // LB24
                    if (_lb24ex && (_nextClass == LineBreakClass.PostfixNumeric || _nextClass == LineBreakClass.PrefixNumeric))
                    {
                        shouldBreak = true;
                        _lb24ex = false;
                        break;
                    }

                    // LB18
                    shouldBreak = lastClass == LineBreakClass.Space;
                    break;

                case LineBreakPairTable.CIBRK:
                    shouldBreak = lastClass == LineBreakClass.Space;
                    if (!shouldBreak)
                    {
                        return false;
                    }

                    break;

                case LineBreakPairTable.CPBRK: // prohibited for combining marks
                    if (lastClass != LineBreakClass.Space)
                    {
                        return false;
                    }

                    break;

                case LineBreakPairTable.PRBRK:
                    break;
            }

            // Rule LB22
            if (_nextClass == LineBreakClass.Inseparable)
            {
                switch (lastClass)
                {
                    case LineBreakClass.MandatoryBreak:
                    case LineBreakClass.ContingentBreak:
                    case LineBreakClass.Exclamation:
                    case LineBreakClass.LineFeed:
                    case LineBreakClass.NextLine:
                    case LineBreakClass.Space:
                    case LineBreakClass.ZWSpace:

                        // Allow break
                        break;
                    case LineBreakClass.CombiningMark:
                        if (_lb22ex)
                        {
                            // Allow break
                            _lb22ex = false;
                            break;
                        }

                        shouldBreak = false;
                        break;
                    default:
                        shouldBreak = false;
                        break;
                }
            }

            if (_lb8a)
            {
                shouldBreak = false;
            }

            // Rule LB21a
            if (_lb21a && (_currentClass == LineBreakClass.Hyphen || _currentClass == LineBreakClass.BreakAfter))
            {
                shouldBreak = false;
                _lb21a = false;
            }
            else
            {
                _lb21a = _currentClass == LineBreakClass.HebrewLetter;
            }

            // Rule LB30a
            if (_currentClass == LineBreakClass.RegionalIndicator)
            {
                _lb30a++;
                if (_lb30a == 2 && _nextClass == LineBreakClass.RegionalIndicator)
                {
                    shouldBreak = true;
                    _lb30a = 0;
                }
            }
            else
            {
                _lb30a = 0;
            }

            // Rule LB30b
            if (_nextClass == LineBreakClass.EModifier && _lastPosition > 0)
            {
                // Mahjong Tiles (Unicode block) are extended pictographics but have a class of ID
                // Unassigned codepoints with Line_Break=ID in some blocks are also assigned the Extended_Pictographic property.
                // Those blocks are intended for future allocation of emoji characters.
                var cp = Codepoint.ReadAt(_text, _lastPosition - 1, out int _);

                if (Codepoint.IsInRangeInclusive(cp, 0x1F000, 0x1F02F))
                {
                    shouldBreak = false;
                }
            }

            _currentClass = _nextClass;

            return shouldBreak;
        }
        
        private int FindPriorNonWhitespace(int from)
        {
            if (from > 0)
            {
                var cp = Codepoint.ReadAt(_text, from - 1, out var count);

                var cls = cp.LineBreakClass;

                if (IsMandatoryBreakOrLineFeedOrCarriageReturn(cls))
                {
                    from -= count;
                }
            }

            while (from > 0)
            {
                var cp = Codepoint.ReadAt(_text, from - 1, out var count);

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
    }
}
