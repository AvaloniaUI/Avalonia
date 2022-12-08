using System;
using System.Buffers;
using System.Runtime.InteropServices;

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
        /// Construct character buffer reference from unsafe character string
        /// </summary>
        /// <param name="unsafeCharacterString">pointer to character string</param>
        /// <param name="characterLength">character length of unsafe string</param>
        public unsafe CharacterBufferReference(char* unsafeCharacterString, int characterLength)
            : this(new UnmanagedMemoryManager<char>(unsafeCharacterString, characterLength).Memory, 0)
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

        public ReadOnlyMemory<char> CharacterBuffer { get; }

        public int OffsetToFirstChar { get; }

        /// <summary>
        /// A MemoryManager over a raw pointer
        /// </summary>
        /// <remarks>The pointer is assumed to be fully unmanaged, or externally pinned - no attempt will be made to pin this data</remarks>
        public sealed unsafe class UnmanagedMemoryManager<T> : MemoryManager<T>
            where T : unmanaged
        {
            private readonly T* _pointer;
            private readonly int _length;

            /// <summary>
            /// Create a new UnmanagedMemoryManager instance at the given pointer and size
            /// </summary>
            /// <remarks>It is assumed that the span provided is already unmanaged or externally pinned</remarks>
            public UnmanagedMemoryManager(Span<T> span)
            {
                fixed (T* ptr = &MemoryMarshal.GetReference(span))
                {
                    _pointer = ptr;
                    _length = span.Length;
                }
            }
            /// <summary>
            /// Create a new UnmanagedMemoryManager instance at the given pointer and size
            /// </summary>
            public UnmanagedMemoryManager(T* pointer, int length)
            {
                if (length < 0)
                    throw new ArgumentOutOfRangeException(nameof(length));
                _pointer = pointer;
                _length = length;
            }
            /// <summary>
            /// Obtains a span that represents the region
            /// </summary>
            public override Span<T> GetSpan() => new Span<T>(_pointer, _length);

            /// <summary>
            /// Provides access to a pointer that represents the data (note: no actual pin occurs)
            /// </summary>
            public override MemoryHandle Pin(int elementIndex = 0)
            {
                if (elementIndex < 0 || elementIndex >= _length)
                    throw new ArgumentOutOfRangeException(nameof(elementIndex));
                return new MemoryHandle(_pointer + elementIndex);
            }
            /// <summary>
            /// Has no effect
            /// </summary>
            public override void Unpin() { }

            /// <summary>
            /// Releases all resources associated with this object
            /// </summary>
            protected override void Dispose(bool disposing) { }
        }     
    }
}

