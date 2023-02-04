using System;
using System.Runtime.CompilerServices;

namespace Avalonia.Media.TextFormatting.Unicode
{
    public readonly record struct Codepoint
    {
        private readonly uint _value;

        /// <summary>
        /// The replacement codepoint that is used for non supported values.
        /// </summary>
        public static Codepoint ReplacementCodepoint
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new('\uFFFD');
        }

        /// <summary>
        /// Creates a new instance of <see cref="Codepoint"/> with the specified value.
        /// </summary>
        /// <param name="value">The codepoint value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Codepoint(uint value) => _value = value;

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
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                const ulong whiteSpaceMask =
                    (1UL << (int)GeneralCategory.Control) |
                    (1UL << (int)GeneralCategory.NonspacingMark) |
                    (1UL << (int)GeneralCategory.Format) |
                    (1UL << (int)GeneralCategory.SpaceSeparator) |
                    (1UL << (int)GeneralCategory.SpacingMark);

                return ((1UL << (int)GeneralCategory) & whiteSpaceMask) != 0UL;
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
#if NET6_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static Codepoint ReadAt(ReadOnlySpan<char> text, int index, out int count)
        {
            // Perf note: this method is performance critical for text layout, modify with care!

            count = 1;

            // Perf note: uint check allows the JIT to ellide the next bound check
            if ((uint)index >= (uint)text.Length)
            {
                return ReplacementCodepoint;
            }

            uint code = text[index];

            //# Surrogate
            if (IsInRangeInclusive(code, 0xD800U, 0xDFFFU))
            {
                uint hi, low;

                //# High surrogate
                if (code <= 0xDBFF)
                {
                    if ((uint)(index + 1) < (uint)text.Length)
                    {
                        hi = code;
                        low = text[index + 1];

                        if (IsInRangeInclusive(low, 0xDC00U, 0xDFFFU))
                        {
                            count = 2;
                            // Perf note: the code is written as below to become just two instructions: shl, lea.
                            // See https://github.com/dotnet/runtime/blob/7ec3634ee579d89b6024f72b595bfd7118093fc5/src/libraries/System.Private.CoreLib/src/System/Text/UnicodeUtility.cs#L38
                            return new Codepoint((hi << 10) + low - ((0xD800U << 10) + 0xDC00U - (1 << 16)));
                        }
                    }
                }

                //# Low surrogate
                else
                {
                    if (index > 0)
                    {
                        low = code;
                        hi = text[index - 1];

                        if (IsInRangeInclusive(hi, 0xD800U, 0xDBFFU))
                        {
                            count = 2;
                            return new Codepoint((hi << 10) + low - ((0xD800U << 10) + 0xDC00U - (1 << 16)));
                        }
                    }
                }

                return ReplacementCodepoint;
            }

            return new Codepoint(code);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsInRangeInclusive(uint value, uint lowerBound, uint upperBound)
            => value - lowerBound <= upperBound - lowerBound;

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="cp"/> is between
        /// <paramref name="lowerBound"/> and <paramref name="upperBound"/>, inclusive.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInRangeInclusive(Codepoint cp, uint lowerBound, uint upperBound)
            => IsInRangeInclusive(cp._value, lowerBound, upperBound);
    }
}
