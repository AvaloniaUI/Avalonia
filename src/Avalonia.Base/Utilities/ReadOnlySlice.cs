using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Avalonia.Utilities
{
    /// <summary>
    ///     ReadOnlySlice enables the ability to work with a sequence within a region of memory and retains the position in within that region.
    /// </summary>
    /// <typeparam name="T">The type of elements in the slice.</typeparam>
    [DebuggerTypeProxy(typeof(ReadOnlySlice<>.ReadOnlySliceDebugView))]
    public readonly record struct ReadOnlySlice<T> : IReadOnlyList<T> where T : struct
    {
        private readonly int _bufferOffset;
        
        /// <summary>
        /// Gets an empty <see cref="ReadOnlySlice{T}"/>
        /// </summary>
        public static ReadOnlySlice<T> Empty => new ReadOnlySlice<T>(Array.Empty<T>());
        
        private readonly ReadOnlyMemory<T> _buffer;

        public ReadOnlySlice(ReadOnlyMemory<T> buffer) : this(buffer, 0, buffer.Length) { }

        public ReadOnlySlice(ReadOnlyMemory<T> buffer, int start, int length, int bufferOffset = 0)
        {
#if DEBUG
            if (start.CompareTo(0) < 0)
            {
                throw new ArgumentOutOfRangeException(nameof (start));
            }

            if (length.CompareTo(buffer.Length) > 0)
            {
                throw new ArgumentOutOfRangeException(nameof (length));
            }
#endif
            
            _buffer = buffer;
            Start = start;
            Length = length;
            _bufferOffset = bufferOffset;
        }

        /// <summary>
        ///     Gets the start.
        /// </summary>
        /// <value>
        ///     The start.
        /// </value>
        public int Start { get; }

        /// <summary>
        ///     Gets the end.
        /// </summary>
        /// <value>
        ///     The end.
        /// </value>
        public int End => Start + Length - 1;

        /// <summary>
        ///     Gets the length.
        /// </summary>
        /// <value>
        ///     The length.
        /// </value>
        public int Length { get; }

        /// <summary>
        ///     Gets a value that indicates whether this instance of <see cref="ReadOnlySlice{T}"/> is Empty.
        /// </summary>
        public bool IsEmpty => Length == 0;

        /// <summary>
        ///     Get the underlying span.
        /// </summary>
        public ReadOnlySpan<T> Span => _buffer.Span.Slice(_bufferOffset, Length);

        /// <summary>
        ///     Get the buffer offset.
        /// </summary>
        public int BufferOffset => _bufferOffset;
        
        /// <summary>
        ///     Get the underlying buffer.
        /// </summary>
        public ReadOnlyMemory<T> Buffer => _buffer;

        /// <summary>
        /// Returns a value to specified element of the slice.
        /// </summary>
        /// <param name="index">The index of the element to return.</param>
        /// <returns>The <typeparamref name="T"/>.</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown when index less than 0 or index greater than or equal to <see cref="Length"/>.
        /// </exception>
        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if DEBUG
                if (index.CompareTo(0) < 0 || index.CompareTo(Length) > 0)
                {
                    throw new ArgumentOutOfRangeException(nameof (index));
                }
#endif
                return Span[index];
            }
        }
        
        /// <summary>
        ///     Returns a sub slice of elements that start at the specified index and has the specified number of elements.
        /// </summary>
        /// <param name="start">The start of the sub slice.</param>
        /// <param name="length">The length of the sub slice.</param>
        /// <returns>A <see cref="ReadOnlySlice{T}"/> that contains the specified number of elements from the specified start.</returns>
        public ReadOnlySlice<T> AsSlice(int start, int length)
        {
            if (IsEmpty)
            {
                return this;
            }

            if (length == 0)
            {
                return Empty;
            }

            if (start < 0 || _bufferOffset + start > _buffer.Length - 1)
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }

            if (_bufferOffset + start + length > _buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            return new ReadOnlySlice<T>(_buffer, start, length, _bufferOffset);
        }

        /// <summary>
        ///     Returns a specified number of contiguous elements from the start of the slice.
        /// </summary>
        /// <param name="length">The number of elements to return.</param>
        /// <returns>A <see cref="ReadOnlySlice{T}"/> that contains the specified number of elements from the start of this slice.</returns>
        public ReadOnlySlice<T> Take(int length)
        {
            if (IsEmpty)
            {
                return this;
            }

            if (length > Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            return new ReadOnlySlice<T>(_buffer, Start, length, _bufferOffset);
        }

        /// <summary>
        ///     Bypasses a specified number of elements in the slice and then returns the remaining elements.
        /// </summary>
        /// <param name="length">The number of elements to skip before returning the remaining elements.</param>
        /// <returns>A <see cref="ReadOnlySlice{T}"/> that contains the elements that occur after the specified index in this slice.</returns>
        public ReadOnlySlice<T> Skip(int length)
        {
            if (IsEmpty)
            {
                return this;
            }

            if (length > Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            return new ReadOnlySlice<T>(_buffer, Start + length, Length - length, _bufferOffset + length);
        }

        /// <summary>
        /// Returns an enumerator for the slice.
        /// </summary>
        public ImmutableReadOnlyListStructEnumerator<T> GetEnumerator()
        {
            return new ImmutableReadOnlyListStructEnumerator<T>(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        int IReadOnlyCollection<T>.Count => Length;

        T IReadOnlyList<T>.this[int index] => this[index];

        public static implicit operator ReadOnlySlice<T>(T[] array)
        {
            return new ReadOnlySlice<T>(array);
        }

        public static implicit operator ReadOnlySlice<T>(ReadOnlyMemory<T> memory)
        {
            return new ReadOnlySlice<T>(memory);
        }

        public static implicit operator ReadOnlySpan<T>(ReadOnlySlice<T> slice) => slice.Span;

        internal class ReadOnlySliceDebugView
        {
            private readonly ReadOnlySlice<T> _readOnlySlice;

            public ReadOnlySliceDebugView(ReadOnlySlice<T> readOnlySlice)
            {
                _readOnlySlice = readOnlySlice;
            }

            public int Start => _readOnlySlice.Start;

            public int End => _readOnlySlice.End;

            public int Length => _readOnlySlice.Length;

            public bool IsEmpty => _readOnlySlice.IsEmpty;

            public ReadOnlySpan<T> Items => _readOnlySlice.Span;
        }
    }
}
