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
    /// Slice represents a contiguous region of arbitrary memory similar
    /// to <see cref="Memory{T}"/> and <see cref="Span{T}"/> though constrained
    /// to arrays.
    /// Unlike <see cref="Span{T}"/>, it is not a byref-like type.
    /// </summary>
    /// <typeparam name="T">The type of item contained in the slice.</typeparam>
    internal readonly struct Slice<T> : IReadOnlyList<T>
        where T : struct
    {
        private readonly T[] _data;

        /// <summary>
        /// Initializes a new instance of the <see cref="Slice{T}"/> struct.
        /// </summary>
        /// <param name="data">The underlying data buffer.</param>
        public Slice(T[] data): this(data, 0, data.Length)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Slice{T}"/> struct.
        /// </summary>
        /// <param name="data">The underlying data buffer.</param>
        /// <param name="start">The offset position in the underlying buffer this slice was created from.</param>
        /// <param name="length">The number of items in the slice.</param>
        public Slice(T[] data, int start, int length)
        {
            if (start < 0)
            {
                throw new ArgumentOutOfRangeException(nameof (start));
            }

            if (length > data.Length)
            {
                throw new ArgumentOutOfRangeException(nameof (length));
            }
            
            if (start + length > data.Length)
            {
                throw new ArgumentOutOfRangeException(nameof (length));
            }

            _data = data;
            Start = start;
            Length = length;
        }

        /// <summary>
        /// Gets an empty <see cref="Slice{T}"/>
        /// </summary>
        public static Slice<T> Empty => new Slice<T>(Array.Empty<T>());

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
                var i =  Start + index;
                
                return ref _data[i];
            }
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="Slice{T}"/> to a <see cref="ReadOnlySlice{T}"/>
        /// </summary>
        public static implicit operator ReadOnlySlice<T>(Slice<T> slice)
            => new ReadOnlySlice<T>(slice._data, slice.Start, slice.Length);

        /// <summary>
        /// Defines an implicit conversion of an array to a <see cref="Slice{T}"/>
        /// </summary>
        public static implicit operator Slice<T>(T[] array) => new Slice<T>(array, 0, array.Length);

        /// <summary>
        /// Copies the contents of this slice into destination span. If the source
        /// and destinations overlap, this method behaves as if the original values in
        /// a temporary location before the destination is overwritten.
        /// </summary>
        /// <param name="destination">The slice to copy items into.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the destination slice is shorter than the source Span.
        /// </exception>
        public void CopyTo(Span<T> destination) => Span.CopyTo(destination);

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
        public Slice<T> AsSlice(int start, int length) => new Slice<T>(_data, start, length);

        /// <summary>
        ///     Returns a specified number of contiguous elements from the start of the slice.
        /// </summary>
        /// <param name="length">The number of elements to return.</param>
        /// <returns>A <see cref="Slice{T}"/> that contains the specified number of elements from the start of this slice.</returns>
        public Slice<T> Take(int length) => AsSlice(0, length);

        /// <summary>
        ///     Bypasses a specified number of elements in the slice and then returns the remaining elements.
        /// </summary>
        /// <param name="length">The number of elements to skip before returning the remaining elements.</param>
        /// <returns>A <see cref="Slice{T}"/> that contains the elements that occur after the specified index in this slice.</returns>
        public Slice<T> Skip(int length) => AsSlice(Start + length, Length - length);
        
        public IEnumerator<T> GetEnumerator()
        {
            return new ImmutableReadOnlyListStructEnumerator<T>(this);
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc/>
        int IReadOnlyCollection<T>.Count => Length;

        /// <inheritdoc/>
        T IReadOnlyList<T>.this[int index] => this[index];
    }
}
