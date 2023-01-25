// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.
// Ported from: https://github.com/SixLabors/Fonts/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Avalonia.Utilities
{
    /// <summary>
    /// ArraySlice represents a contiguous region of arbitrary memory similar
    /// to <see cref="Memory{T}"/> and <see cref="Span{T}"/> though constrained
    /// to arrays.
    /// Unlike <see cref="Span{T}"/>, it is not a byref-like type.
    /// </summary>
    /// <typeparam name="T">The type of item contained in the slice.</typeparam>
    internal readonly struct ArraySlice<T> : IReadOnlyList<T>
    {
        /// <summary>
        /// Gets an empty <see cref="ArraySlice{T}"/>
        /// </summary>
        public static ArraySlice<T> Empty => new ArraySlice<T>(Array.Empty<T>());

        private readonly T[] _data;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArraySlice{T}"/> struct.
        /// </summary>
        /// <param name="data">The underlying data buffer.</param>
        public ArraySlice(T[] data)
            : this(data, 0, data.Length)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArraySlice{T}"/> struct.
        /// </summary>
        /// <param name="data">The underlying data buffer.</param>
        /// <param name="start">The offset position in the underlying buffer this slice was created from.</param>
        /// <param name="length">The number of items in the slice.</param>
        public ArraySlice(T[] data, int start, int length)
        {
#if DEBUG
            if (start.CompareTo(0) < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }

            if (length.CompareTo(data.Length) > 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            if ((start + length).CompareTo(data.Length) > 0)
            {
                throw new ArgumentOutOfRangeException(nameof(data));
            }
#endif

            _data = data;
            Start = start;
            Length = length;
        }


        /// <summary>
        ///     Gets a value that indicates whether this instance of <see cref="ArraySlice{T}"/> is Empty.
        /// </summary>
        public bool IsEmpty => Length == 0;

        /// <summary>
        /// Gets the offset position in the underlying buffer this slice was created from.
        /// </summary>
        public int Start { get; }

        /// <summary>
        /// Gets the number of items in the slice.
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// Gets a <see cref="Span{T}"/> representing this slice.
        /// </summary>
        public Span<T> Span => new Span<T>(_data, Start, Length);

        /// <summary>
        /// Returns a reference to specified element of the slice.
        /// </summary>
        /// <param name="index">The index of the element to return.</param>
        /// <returns>The <typeparamref name="T"/>.</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown when index less than 0 or index greater than or equal to <see cref="Length"/>.
        /// </exception>
        public ref T this[int index]
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
                var i = index + Start;

                return ref _data[i];
            }
        }

        /// <summary>
        /// Defines an implicit conversion of an array to a <see cref="ArraySlice{T}"/>
        /// </summary>
        public static implicit operator ArraySlice<T>(T[] array) => new ArraySlice<T>(array, 0, array.Length);

        /// <summary>
        /// Fills the contents of this slice with the given value.
        /// </summary>
        public void Fill(T value) => Span.Fill(value);

        /// <summary>
        /// Forms a slice out of the given slice, beginning at 'start', of given length
        /// </summary>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <param name="length">The desired length for the slice (exclusive).</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> or end index is not in range (&lt;0 or &gt;Length).
        /// </exception>
        public ArraySlice<T> Slice(int start, int length) => new ArraySlice<T>(_data, start, length);

        /// <summary>
        ///     Returns a specified number of contiguous elements from the start of the slice.
        /// </summary>
        /// <param name="length">The number of elements to return.</param>
        /// <returns>A <see cref="ArraySlice{T}"/> that contains the specified number of elements from the start of this slice.</returns>
        public ArraySlice<T> Take(int length)
        {
            if (IsEmpty)
            {
                return this;
            }

            if (length > Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            return new ArraySlice<T>(_data, Start, length);
        }

        /// <summary>
        ///     Bypasses a specified number of elements in the slice and then returns the remaining elements.
        /// </summary>
        /// <param name="length">The number of elements to skip before returning the remaining elements.</param>
        /// <returns>A <see cref="ArraySlice{T}"/> that contains the elements that occur after the specified index in this slice.</returns>
        public ArraySlice<T> Skip(int length)
        {
            if (IsEmpty)
            {
                return this;
            }

            if (length > Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            return new ArraySlice<T>(_data, Start + length, Length - length);
        }

        public ImmutableReadOnlyListStructEnumerator<T> GetEnumerator() =>
            new ImmutableReadOnlyListStructEnumerator<T>(this);

        /// <inheritdoc/>
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc/>
        T IReadOnlyList<T>.this[int index] => this[index];

        /// <inheritdoc/>
        int IReadOnlyCollection<T>.Count => Length;
    }
}

