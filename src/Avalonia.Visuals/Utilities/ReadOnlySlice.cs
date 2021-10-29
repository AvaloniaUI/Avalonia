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
    public readonly struct ReadOnlySlice<T> : IReadOnlyList<T> where T : struct
    {
        private readonly ReadOnlyMemory<T> _data;

        public ReadOnlySlice(ReadOnlyMemory<T> data) : this(data, 0, data.Length) { }

        public ReadOnlySlice(ReadOnlyMemory<T> data, int start, int length)
        {
#if DEBUG
            if (start.CompareTo(0) < 0)
            {
                throw new ArgumentOutOfRangeException(nameof (start));
            }

            if (length.CompareTo(data.Length) > 0)
            {
                throw new ArgumentOutOfRangeException(nameof (length));
            }
            
            if ((start + length).CompareTo(data.Length) > 0)
            {
                throw new ArgumentOutOfRangeException(nameof (data));
            }
#endif
            
            _data = data;
            Start = start;
            Length = length;
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
        ///     The underlying span.
        /// </summary>
        public ReadOnlySpan<T> Span => _data.Span.Slice(Start, Length);

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

            return new ReadOnlySlice<T>(_data, Start, length);
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

            return new ReadOnlySlice<T>(_data, Start + length, Length - length);
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
