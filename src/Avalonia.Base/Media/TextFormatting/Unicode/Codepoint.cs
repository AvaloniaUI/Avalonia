using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Avalonia.Media.TextFormatting.Unicode
{
    public readonly record struct Codepoint
    {
        private readonly uint _value;

        /// <summary>
        /// The replacement codepoint that is used for non supported values.
        /// </summary>
        public static readonly Codepoint ReplacementCodepoint = new Codepoint('\uFFFD');

        public Codepoint(uint value)
        {
            _value = value;
        }

        /// <summary>
        /// Get the codepoint's value.
        /// </summary>
        public uint Value => _value;

        /// <summary>
        /// Gets the <see cref="Unicode.GeneralCategory"/>.
        /// </summary>
        public GeneralCategory GeneralCategory => UnicodeData.GetGeneralCategory(_value);

        /// <summary>
        /// Gets the <see cref="Unicode.Script"/>.
        /// </summary>
        public Script Script => UnicodeData.GetScript(_value);

        /// <summary>
        /// Gets the <see cref="Unicode.BidiClass"/>.
        /// </summary>
        public BidiClass BiDiClass => UnicodeData.GetBiDiClass(_value);

        /// <summary>
        /// Gets the <see cref="Unicode.BidiPairedBracketType"/>.
        /// </summary>
        public BidiPairedBracketType PairedBracketType => UnicodeData.GetBiDiPairedBracketType(_value);
        
        /// <summary>
        /// Gets the <see cref="Unicode.LineBreakClass"/>.
        /// </summary>
        public LineBreakClass LineBreakClass => UnicodeData.GetLineBreakClass(_value);

        /// <summary>
        /// Gets the <see cref="GraphemeBreakClass"/>.
        /// </summary>
        public GraphemeBreakClass GraphemeBreakClass => UnicodeData.GetGraphemeClusterBreak(_value);

        /// <summary>
        /// Determines whether this <see cref="Codepoint"/> is a break char.
        /// </summary>
        /// <returns>
        /// <c>true</c> if [is break character]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsBreakChar
        {
            get
            {
                switch (_value)
                {
                    case '\u000A':
                    case '\u000B':
                    case '\u000C':
                    case '\u000D':
                    case '\u0085':
                    case '\u2028':
                    case '\u2029':
                        return true;
                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// Determines whether this <see cref="Codepoint"/> is white space.
        /// </summary>
        /// <returns>
        /// <c>true</c> if [is whitespace]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsWhiteSpace
        {
            get
            {
                switch (GeneralCategory)
                {
                    case GeneralCategory.Control:
                    case GeneralCategory.NonspacingMark:
                    case GeneralCategory.Format:
                    case GeneralCategory.SpaceSeparator:
                    case GeneralCategory.SpacingMark:
                        return true;
                }

                return false;
            }
        }
        
        /// <summary>
        /// Gets the canonical representation of a given codepoint.
        /// <see href="https://www.unicode.org/L2/L2013/13123-norm-and-bpa.pdf"/>
        /// </summary>
        /// <param name="codePoint">The code point to be mapped.</param>
        /// <returns>The mapped canonical code point, or the passed <paramref name="codePoint"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Codepoint GetCanonicalType(Codepoint codePoint)
        {
            if (codePoint._value == 0x3008)
            {
                return new Codepoint(0x2329);
            }

            if (codePoint._value == 0x3009)
            {
                return new Codepoint(0x232A);
            }

            return codePoint;
        }
        
        /// <summary>
        /// Gets the codepoint representing the bracket pairing for this instance.
        /// </summary>
        /// <param name="codepoint">
        /// When this method returns, contains the codepoint representing the bracket pairing for this instance;
        /// otherwise, the default value for the type of the <paramref name="codepoint"/> parameter.
        /// This parameter is passed uninitialized.
        /// .</param>
        /// <returns><see langword="true"/> if this instance has a bracket pairing; otherwise, <see langword="false"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetPairedBracket(out Codepoint codepoint)
        {
            if (PairedBracketType == BidiPairedBracketType.None)
            {
                codepoint = default;
                
                return false;
            }

            codepoint = UnicodeData.GetBiDiPairedBracket(_value);

            return true;
        }

        public static implicit operator int(Codepoint codepoint)
        {
            return (int)codepoint._value;
        }

        public static implicit operator uint(Codepoint codepoint)
        {
            return codepoint._value;
        }

        /// <summary>
        /// Reads the <see cref="Codepoint"/> at specified position.
        /// </summary>
        /// <param name="text">The buffer to read from.</param>
        /// <param name="index">The index to read at.</param>
        /// <param name="count">The count of character that were read.</param>
        /// <returns></returns>
        public static Codepoint ReadAt(IReadOnlyList<char> text, int index, out int count)
        {
            count = 1;

            if (index >= text.Count)
            {
                return ReplacementCodepoint;
            }

            var code = text[index];

            ushort hi, low;

            //# High surrogate
            if (0xD800 <= code && code <= 0xDBFF)
            {
                hi = code;

                if (index + 1 == text.Count)
                {
                    return ReplacementCodepoint;
                }

                low = text[index + 1];

                if (0xDC00 <= low && low <= 0xDFFF)
                {
                    count = 2;
                    return new Codepoint((uint)((hi - 0xD800) * 0x400 + (low - 0xDC00) + 0x10000));
                }

                return ReplacementCodepoint;
            }

            //# Low surrogate
            if (0xDC00 <= code && code <= 0xDFFF)
            {
                if (index == 0)
                {
                    return ReplacementCodepoint;
                }

                hi = text[index - 1];

                low = code;

                if (0xD800 <= hi && hi <= 0xDBFF)
                {
                    count = 2;
                    return new Codepoint((uint)((hi - 0xD800) * 0x400 + (low - 0xDC00) + 0x10000));
                }

                return ReplacementCodepoint;
            }

            return new Codepoint(code);
        }
        
        /// <summary>
        /// Reads the <see cref="Codepoint"/> at specified position.
        /// </summary>
        /// <param name="text">The buffer to read from.</param>
        /// <param name="index">The index to read at.</param>
        /// <param name="count">The count of character that were read.</param>
        /// <returns></returns>
        public static Codepoint ReadAt(ReadOnlySpan<char> text, int index, out int count)
        {
            count = 1;

            if (index >= text.Length)
            {
                return ReplacementCodepoint;
            }

            var code = text[index];

            ushort hi, low;

            //# High surrogate
            if (0xD800 <= code && code <= 0xDBFF)
            {
                hi = code;

                if (index + 1 == text.Length)
                {
                    return ReplacementCodepoint;
                }

                low = text[index + 1];

                if (0xDC00 <= low && low <= 0xDFFF)
                {
                    count = 2;
                    return new Codepoint((uint)((hi - 0xD800) * 0x400 + (low - 0xDC00) + 0x10000));
                }

                return ReplacementCodepoint;
            }

            //# Low surrogate
            if (0xDC00 <= code && code <= 0xDFFF)
            {
                if (index == 0)
                {
                    return ReplacementCodepoint;
                }

                hi = text[index - 1];

                low = code;

                if (0xD800 <= hi && hi <= 0xDBFF)
                {
                    count = 2;
                    return new Codepoint((uint)((hi - 0xD800) * 0x400 + (low - 0xDC00) + 0x10000));
                }

                return ReplacementCodepoint;
            }

            return new Codepoint(code);
        }

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="cp"/> is between
        /// <paramref name="lowerBound"/> and <paramref name="upperBound"/>, inclusive.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInRangeInclusive(Codepoint cp, uint lowerBound, uint upperBound)
            => (cp._value - lowerBound) <= (upperBound - lowerBound);
    }
}
