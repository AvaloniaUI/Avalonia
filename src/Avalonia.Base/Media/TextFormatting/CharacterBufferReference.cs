using System;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Text character buffer reference
    /// </summary>
    public readonly struct CharacterBufferReference : IEquatable<CharacterBufferReference>
    {
        /// <summary>
        /// Construct character buffer reference from character array
        /// </summary>
        /// <param name="characterArray">character array</param>
        /// <param name="offsetToFirstChar">character buffer offset to the first character</param>
        public CharacterBufferReference(char[] characterArray, int offsetToFirstChar = 0)
            : this(characterArray.AsMemory(), offsetToFirstChar)
        { }

        /// <summary>
        /// Construct character buffer reference from string
        /// </summary>
        /// <param name="characterString">character string</param>
        /// <param name="offsetToFirstChar">character buffer offset to the first character</param>
        public CharacterBufferReference(string characterString, int offsetToFirstChar = 0)
            : this(characterString.AsMemory(), offsetToFirstChar)
        { }
      
        /// <summary>
        /// Construct character buffer reference from memory buffer
        /// </summary>
        internal CharacterBufferReference(ReadOnlyMemory<char> characterBuffer, int offsetToFirstChar = 0)
        {
            if (offsetToFirstChar < 0)
            {
                throw new ArgumentOutOfRangeException("offsetToFirstChar", "ParameterCannotBeNegative");
            }

            // maximum offset is one less than CharacterBuffer.Count, except that zero is always a valid offset
            // even in the case of an empty or null character buffer
            var maxOffset = characterBuffer.Length == 0 ? 0 : Math.Max(0, characterBuffer.Length - 1);
            if (offsetToFirstChar > maxOffset)
            {
                throw new ArgumentOutOfRangeException("offsetToFirstChar", $"ParameterCannotBeGreaterThan, {maxOffset}");
            }

            CharacterBuffer = characterBuffer;
            OffsetToFirstChar = offsetToFirstChar;
        }

        /// <summary>
        /// Gets the character memory buffer
        /// </summary>
        public ReadOnlyMemory<char> CharacterBuffer { get; }

        /// <summary>
        /// Gets the character offset relative to the beginning of buffer to 
        /// the first character of the run
        /// </summary>
        public int OffsetToFirstChar { get; }

        /// <summary>
        /// Compute hash code
        /// </summary>
        public override int GetHashCode()
        {
            return CharacterBuffer.IsEmpty ? 0 : CharacterBuffer.GetHashCode();
        }

        /// <summary>
        /// Test equality with the input object 
        /// </summary>
        /// <param name="obj"> The object to test. </param>
        public override bool Equals(object? obj)
        {
            if (obj is CharacterBufferReference reference)
            {
                return Equals(reference);
            }

            return false;
        }

        /// <summary>
        /// Test equality with the input CharacterBufferReference
        /// </summary>
        /// <param name="value"> The characterBufferReference value to test </param>
        public bool Equals(CharacterBufferReference value)
        {
            return CharacterBuffer.Equals(value.CharacterBuffer);
        }

        /// <summary>
        /// Compare two CharacterBufferReference for equality
        /// </summary>
        /// <param name="left">left operand</param>
        /// <param name="right">right operand</param>
        /// <returns>whether or not two operands are equal</returns>
        public static bool operator ==(CharacterBufferReference left, CharacterBufferReference right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compare two CharacterBufferReference for inequality
        /// </summary>
        /// <param name="left">left operand</param>
        /// <param name="right">right operand</param>
        /// <returns>whether or not two operands are equal</returns>
        public static bool operator !=(CharacterBufferReference left, CharacterBufferReference right)
        {
            return !(left == right);
        }
    }
}

