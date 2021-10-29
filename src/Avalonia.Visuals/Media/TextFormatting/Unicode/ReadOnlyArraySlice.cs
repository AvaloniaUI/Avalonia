// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.
// Ported from: https://github.com/SixLabors/Fonts/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting.Unicode
{
    /// <summary>
    /// ReadOnlyArraySlice represents a contiguous region of arbitrary memory similar
    /// to <see cref="ReadOnlyMemory{T}"/> and <see cref="ReadOnlySpan{T}"/> though constrained
    /// to arrays.
    /// Unlike <see cref="ReadOnlySpan{T}"/>, it is not a byref-like type.
    /// </summary>
    /// <typeparam name="T">The type of item contained in the slice.</typeparam>
    internal readonly struct ReadOnlyArraySlice<T> : IReadOnlyList<T> where T : struct
    {
        private readonly T[] _data;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlyArraySlice{T}"/> struct.
        /// </summary>
        /// <param name="data">The underlying data buffer.</param>
        public ReadOnlyArraySlice(T[] data)
            : this(data, 0, data.Length)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlyArraySlice{T}"/> struct.
        /// </summary>
        /// <param name="data">The underlying data buffer.</param>
        /// <param name="start">The offset position in the underlying buffer this slice was created from.</param>
        /// <param name="length">The number of items in the slice.</param>
        public ReadOnlyArraySlice(T[] data, int start, int length)
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
        /// Gets an empty <see cref="ReadOnlyArraySlice{T}"/>
        /// </summary>
        public static ReadOnlyArraySlice<T> Empty => new ReadOnlyArraySlice<T>(Array.Empty<T>());

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
        public ReadOnlySpan<T> Span => new ReadOnlySpan<T>(_data, Start, Length);

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
                    throw new ArgumentOutOfRangeException(nameof (index));
                }
#endif
                var i = index + Start;
                
                return ref _data[i];
            }
        }

        /// <summary>
        /// Defines an implicit conversion of an array to a <see cref="ReadOnlyArraySlice{T}"/>
        /// </summary>
        public static implicit operator ReadOnlyArraySlice<T>(T[] array)
            => new ReadOnlyArraySlice<T>(array, 0, array.Length);

        /// <summary>
        /// Copies the contents of this slice into destination span. If the source
        /// and destinations overlap, this method behaves as if the original values in
        /// a temporary location before the destination is overwritten.
        /// </summary>
        /// <param name="destination">The slice to copy items into.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the destination slice is shorter than the source Span.
        /// </exception>
        public void CopyTo(ArraySlice<T> destination) => Span.CopyTo(destination.Span);

        /// <summary>
        /// Forms a slice out of the given slice, beginning at 'start', of given length
        /// </summary>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <param name="length">The desired length for the slice (exclusive).</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> or end index is not in range (&lt;0 or &gt;Length).
        /// </exception>
        public ReadOnlyArraySlice<T> Slice(int start, int length)
            => new ReadOnlyArraySlice<T>(_data, start, length);

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
