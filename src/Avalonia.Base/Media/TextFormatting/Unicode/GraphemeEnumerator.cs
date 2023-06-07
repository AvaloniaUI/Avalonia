// This source file is adapted from the .NET cross-platform runtime project. 
// (https://github.com/dotnet/runtime/) 
// 
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;

namespace Avalonia.Media.TextFormatting.Unicode
{
    public ref struct GraphemeEnumerator
    {
        private readonly ReadOnlySpan<char> _text;
        private int _currentCodeUnitOffset;
        private int _codeUnitLengthOfCurrentCodepoint;
        private Codepoint _currentCodepoint;

        /// <summary>
        /// Will be <see cref="GraphemeBreakClass.Other"/> if invalid data or EOF reached.
        /// Caller shouldn't need to special-case this since the normal rules will halt on this condition.
        /// </summary>
        private GraphemeBreakClass _currentType;

        public GraphemeEnumerator(ReadOnlySpan<char> text)
        {
            _text = text;
            _currentCodeUnitOffset = 0;
            _codeUnitLengthOfCurrentCodepoint = 0;
            _currentCodepoint = Codepoint.ReplacementCodepoint;
            _currentType = GraphemeBreakClass.Other;
        }

        /// <summary>
        /// Moves to the next <see cref="Grapheme"/>.
        /// </summary>
        /// <returns></returns>
        public bool MoveNext(out Grapheme grapheme)
        {
            var startOffset = _currentCodeUnitOffset;

            if ((uint)startOffset >= (uint)_text.Length)
            {
                grapheme = default;
                return false;
            }

            // Algorithm given at https://www.unicode.org/reports/tr29/#Grapheme_Cluster_Boundary_Rules.

            if (startOffset == 0)
            {
                ReadNextCodepoint();
            }

            var firstCodepoint = _currentCodepoint;

            // First, consume as many Prepend scalars as we can (rule GB9b).
            if (_currentType == GraphemeBreakClass.Prepend)
            {
                do
                {
                    ReadNextCodepoint();
                } while (_currentType == GraphemeBreakClass.Prepend);

                // There were only Prepend scalars in the text
                if ((uint)_currentCodeUnitOffset >= (uint)_text.Length)
                {
                    goto Return;
                }
            }

            // Next, make sure we're not about to violate control character restrictions.
            // Essentially, if we saw Prepend data, we can't have Control | CR | LF data afterward (rule GB5).
            if (_currentCodeUnitOffset > startOffset)
            {
                const uint controlCrLfMask =
                    (1U << (int)GraphemeBreakClass.Control) |
                    (1U << (int)GraphemeBreakClass.CR) |
                    (1U << (int)GraphemeBreakClass.LF);

                if (((1U << (int)_currentType) & controlCrLfMask) != 0U)
                {
                    goto Return;
                }
            }

            // Now begin the main state machine.

            var previousClusterBreakType = _currentType;

            ReadNextCodepoint();

            switch (previousClusterBreakType)
            {
                case GraphemeBreakClass.CR:
                    if (_currentType != GraphemeBreakClass.LF)
                    {
                        goto Return; // rules GB3 & GB4 (only <LF> can follow <CR>)
                    }

                    ReadNextCodepoint();
                    goto case GraphemeBreakClass.LF;

                case GraphemeBreakClass.Control:
                case GraphemeBreakClass.LF:
                    goto Return; // rule GB4 (no data after Control | LF)

                case GraphemeBreakClass.L:
                {
                    if (_currentType == GraphemeBreakClass.L)
                    {
                        ReadNextCodepoint(); // rule GB6 (L x L)
                        goto case GraphemeBreakClass.L;
                    }
                    else if (_currentType == GraphemeBreakClass.V)
                    {
                        ReadNextCodepoint(); // rule GB6 (L x V)
                        goto case GraphemeBreakClass.V;
                    }
                    else if (_currentType == GraphemeBreakClass.LV)
                    {
                        ReadNextCodepoint(); // rule GB6 (L x LV)
                        goto case GraphemeBreakClass.LV;
                    }
                    else if (_currentType == GraphemeBreakClass.LVT)
                    {
                        ReadNextCodepoint(); // rule GB6 (L x LVT)
                        goto case GraphemeBreakClass.LVT;
                    }
                    else
                    {
                        break;
                    }
                }

                case GraphemeBreakClass.LV:
                case GraphemeBreakClass.V:
                {
                    if (_currentType == GraphemeBreakClass.V)
                    {
                        ReadNextCodepoint(); // rule GB7 (LV | V x V)
                        goto case GraphemeBreakClass.V;
                    }
                    else if (_currentType == GraphemeBreakClass.T)
                    {
                        ReadNextCodepoint(); // rule GB7 (LV | V x T)
                        goto case GraphemeBreakClass.T;
                    }
                    else
                    {
                        break;
                    }
                }

                case GraphemeBreakClass.LVT:
                case GraphemeBreakClass.T:
                    if (_currentType == GraphemeBreakClass.T)
                    {
                        ReadNextCodepoint(); // rule GB8 (LVT | T x T)
                        goto case GraphemeBreakClass.T;
                    }
                    else
                    {
                        break;
                    }

                case GraphemeBreakClass.ExtendedPictographic:
                    // Attempt processing extended pictographic (rules GB11, GB9).
                    // First, drain any Extend scalars that might exist
                    while (_currentType == GraphemeBreakClass.Extend)
                    {
                        ReadNextCodepoint();
                    }

                    // Now see if there's a ZWJ + extended pictograph again.
                    if (_currentType != GraphemeBreakClass.ZWJ)
                    {
                        break;
                    }

                    ReadNextCodepoint();
                    if (_currentType != GraphemeBreakClass.ExtendedPictographic)
                    {
                        break;
                    }

                    ReadNextCodepoint();
                    goto case GraphemeBreakClass.ExtendedPictographic;

                case GraphemeBreakClass.RegionalIndicator:
                    // We've consumed a single RI scalar. Try to consume another (to make it a pair).

                    if (_currentType == GraphemeBreakClass.RegionalIndicator)
                    {
                        ReadNextCodepoint();
                    }

                    // Standlone RI scalars (or a single pair of RI scalars) can only be followed by trailers.

                    break; // nothing but trailers after the final RI
            }

            const uint gb9Mask =
                (1U << (int)GraphemeBreakClass.Extend) |
                (1U << (int)GraphemeBreakClass.ZWJ) |
                (1U << (int)GraphemeBreakClass.SpacingMark);

            // rules GB9, GB9a
            while (((1U << (int)_currentType) & gb9Mask) != 0U)
            {
                ReadNextCodepoint();
            }

            Return:

            var graphemeLength = _currentCodeUnitOffset - startOffset;
            grapheme = new Grapheme(firstCodepoint, startOffset, graphemeLength);

            return true; // rules GB2, GB999
        }

        private void ReadNextCodepoint()
        {
            // For ill-formed subsequences (like unpaired UTF-16 surrogate code points), we rely on
            // the decoder's default behavior of interpreting these ill-formed subsequences as
            // equivalent to U+FFFD REPLACEMENT CHARACTER. This code point has a boundary property
            // of Other (XX), which matches the modifications made to UAX#29, Rev. 35.
            // See: https://www.unicode.org/reports/tr29/tr29-35.html#Modifications
            // This change is also reflected in the UCD files. For example, Unicode 11.0's UCD file
            // https://www.unicode.org/Public/11.0.0/ucd/auxiliary/GraphemeBreakProperty.txt
            // has the line "D800..DFFF    ; Control # Cs [2048] <surrogate-D800>..<surrogate-DFFF>",
            // but starting with Unicode 12.0 that line has been removed.
            //
            // If a later version of the Unicode Standard further modifies this guidance we should reflect
            // that here.

            _currentCodeUnitOffset += _codeUnitLengthOfCurrentCodepoint;

            _currentCodepoint = Codepoint.ReadAt(_text, _currentCodeUnitOffset,
                out _codeUnitLengthOfCurrentCodepoint);

            _currentType = _currentCodepoint.GraphemeBreakClass;
        }
    }
}
