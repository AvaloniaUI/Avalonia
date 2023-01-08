using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting
{
    public readonly struct CharacterBufferRange : IReadOnlyList<char>
    {
        /// <summary>
        /// Getting an empty character string
        /// </summary>
        public static CharacterBufferRange Empty => new CharacterBufferRange();

        /// <summary>
        /// Construct <see cref="CharacterBufferRange"/> from character array
        /// </summary>
        /// <param name="characterArray">character array</param>
        /// <param name="offsetToFirstChar">character buffer offset to the first character</param>
        /// <param name="characterLength">character length</param>
        public CharacterBufferRange(
            char[] characterArray,
            int offsetToFirstChar,
            int characterLength
            )
            : this(
                new CharacterBufferReference(characterArray, offsetToFirstChar),
                characterLength
                )
        { }

        /// <summary>
        /// Construct <see cref="CharacterBufferRange"/> from string
        /// </summary>
        /// <param name="characterString">character string</param>
        /// <param name="offsetToFirstChar">character buffer offset to the first character</param>
        /// <param name="characterLength">character length</param>
        public CharacterBufferRange(
            string characterString,
            int offsetToFirstChar,
            int characterLength
            )
            : this(
                new CharacterBufferReference(characterString, offsetToFirstChar),
                characterLength
                )
        { }

        /// <summary>
        /// Construct a <see cref="CharacterBufferRange"/> from <see cref="CharacterBufferReference"/>
        /// </summary>
        /// <param name="characterBufferReference">character buffer reference</param>
        /// <param name="characterLength">number of characters</param>
        public CharacterBufferRange(
            CharacterBufferReference characterBufferReference,
            int characterLength
            )
        {
            if (characterLength < 0)
            {
                throw new ArgumentOutOfRangeException("characterLength", "ParameterCannotBeNegative");
            }

            int maxLength = characterBufferReference.CharacterBuffer.Length > 0 ?
                characterBufferReference.CharacterBuffer.Length - characterBufferReference.OffsetToFirstChar :
                0;

            if (characterLength > maxLength)
            {
                throw new ArgumentOutOfRangeException("characterLength", $"ParameterCannotBeGreaterThan {maxLength}");
            }

            CharacterBufferReference = characterBufferReference;
            Length = characterLength;
        }

        /// <summary>
        /// Construct a <see cref="CharacterBufferRange"/> from part of another <see cref="CharacterBufferRange"/>
        /// </summary>
        internal CharacterBufferRange(
            CharacterBufferRange characterBufferRange,
            int offsetToFirstChar,
            int characterLength
            ) :
            this(
                characterBufferRange.CharacterBuffer,
                characterBufferRange.OffsetToFirstChar + offsetToFirstChar,
                characterLength
                )
        { }


        /// <summary>
        /// Construct a <see cref="CharacterBufferRange"/> from string
        /// </summary>
        internal CharacterBufferRange(
            string charString
            ) :
            this(
                charString,
                0,
                charString.Length
                )
        { }


        /// <summary>
        /// Construct <see cref="CharacterBufferRange"/> from memory buffer
        /// </summary>
        internal CharacterBufferRange(
            ReadOnlyMemory<char> charBuffer,
            int offsetToFirstChar,
            int characterLength
            ) :
            this(
                new CharacterBufferReference(charBuffer, offsetToFirstChar),
                characterLength
                )
        { }


        /// <summary>
        /// Construct a <see cref="CharacterBufferRange"/> by extracting text info from a text run
        /// </summary>
        internal CharacterBufferRange(TextRun textRun)
        {
            CharacterBufferReference = textRun.CharacterBufferReference;
            Length = textRun.Length;
        }

        public char this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if DEBUG
                if (index.CompareTo(0) < 0 || index.CompareTo(Length) > 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
#endif
                return CharacterBufferReference.CharacterBuffer.Span[CharacterBufferReference.OffsetToFirstChar + index];
            }
        }

        /// <summary>
        /// Gets a reference to the character buffer
        /// </summary>
        public CharacterBufferReference CharacterBufferReference { get; }

        /// <summary>
        /// Gets the number of characters in text source character store
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// Gets a span from the character buffer range
        /// </summary>
        public ReadOnlySpan<char> Span =>
            CharacterBufferReference.CharacterBuffer.Span.Slice(CharacterBufferReference.OffsetToFirstChar, Length);

        /// <summary>
        /// Gets the character memory buffer
        /// </summary>
        internal ReadOnlyMemory<char> CharacterBuffer => CharacterBufferReference.CharacterBuffer;

        /// <summary>
        /// Gets the character offset relative to the beginning of buffer to 
        /// the first character of the run
        /// </summary>
        internal int OffsetToFirstChar => CharacterBufferReference.OffsetToFirstChar;

        /// <summary>
        /// Indicate whether the character buffer range is empty
        /// </summary>
        internal bool IsEmpty => CharacterBufferReference.CharacterBuffer.Length == 0 || Length <= 0;

        internal CharacterBufferRange Take(int length)
        {
            if (IsEmpty)
            {
                return this;
            }

            if (length > Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            return new CharacterBufferRange(CharacterBufferReference, length);
        }

        internal CharacterBufferRange Skip(int length)
        {
            if (IsEmpty)
            {
                return this;
            }

            if (length > Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            if (length == Length)
            {
                return new CharacterBufferRange(new CharacterBufferReference(), 0);
            }

            var characterBufferReference = new CharacterBufferReference(
                CharacterBufferReference.CharacterBuffer,
                CharacterBufferReference.OffsetToFirstChar + length);

            return new CharacterBufferRange(characterBufferReference, Length - length);
        }

        /// <summary>
        /// Compute hash code
        /// </summary>
        public override int GetHashCode()
        {
            return CharacterBufferReference.GetHashCode() ^ Length;
        }

        /// <summary>
        /// Test equality with the input object
        /// </summary>
        /// <param name="obj"> The object to test </param>
        public override bool Equals(object? obj)
        {
            if (obj is CharacterBufferRange range)
            {
                return Equals(range);
            }

            return false;
        }

        /// <summary>
        /// Test equality with the input CharacterBufferRange
        /// </summary>
        /// <param name="value"> The CharacterBufferRange value to test </param>
        public bool Equals(CharacterBufferRange value)
        {
            return CharacterBufferReference.Equals(value.CharacterBufferReference)
                && Length == value.Length;
        }

        /// <summary>
        /// Compare two CharacterBufferRange for equality
        /// </summary>
        /// <param name="left">left operand</param>
        /// <param name="right">right operand</param>
        /// <returns>whether or not two operands are equal</returns>
        public static bool operator ==(CharacterBufferRange left, CharacterBufferRange right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compare two CharacterBufferRange for inequality
        /// </summary>
        /// <param name="left">left operand</param>
        /// <param name="right">right operand</param>
        /// <returns>whether or not two operands are equal</returns>
        public static bool operator !=(CharacterBufferRange left, CharacterBufferRange right)
        {
            return !(left == right);
        }

        int IReadOnlyCollection<char>.Count => Length;

        public IEnumerator<char> GetEnumerator() => new ImmutableReadOnlyListStructEnumerator<char>(this);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
