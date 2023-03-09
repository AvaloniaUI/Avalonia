// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.
// Ported from: https://github.com/SixLabors/Fonts/

using System;
using System.Runtime.CompilerServices;

namespace Avalonia.Utilities
{
    /// <summary>
    /// A helper type for avoiding allocations while building arrays.
    /// </summary>
    /// <typeparam name="T">The type of item contained in the array.</typeparam>
    internal struct ArrayBuilder<T>
    {
        private const int DefaultCapacity = 4;
        private const int MaxCoreClrArrayLength = 0x7FeFFFFF;

        // Starts out null, initialized on first Add.
        private T[]? _data;
        private int _size;

        /// <summary>
        /// Gets or sets the number of items in the array.
        /// </summary>
        public int Length
        {
            get => _size;

            set
            {
                if (value == _size)
                {
                    return;
                }

                if (value > 0)
                {
                    EnsureCapacity(value);

                    _size = value;
                }
                else
                {
                    _size = 0;
                }
            }
        }

        /// <summary>
        /// Gets the current capacity of the array.
        /// </summary>
        public int Capacity
            => _data?.Length ?? 0;

        /// <summary>
        /// Returns a reference to specified element of the array.
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
                if (index.CompareTo(0) < 0 || index.CompareTo(_size) > 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
#endif

                return ref _data![index];
            }
        }

        /// <summary>
        /// Appends a given number of empty items to the array returning
        /// the items as a slice.
        /// </summary>
        /// <param name="length">The number of items in the slice.</param>
        /// <param name="clear">Whether to clear the new slice, Defaults to <see langword="true"/>.</param>
        /// <returns>The <see cref="ArraySlice{T}"/>.</returns>
        public ArraySlice<T> Add(int length, bool clear = true)
        {
            var position = _size;

            // Expand the array.
            Length += length;

            var slice = AsSlice(position, Length - position);

            if (clear)
            {
                slice.Span.Clear();
            }

            return slice;
        }

        /// <summary>
        /// Appends the slice to the array copying the data across.
        /// </summary>
        /// <param name="value">The array slice.</param>
        /// <returns>The <see cref="ArraySlice{T}"/>.</returns>
        public ArraySlice<T> Add(in ArraySlice<T> value)
        {
            var position = _size;

            // Expand the array.
            Length += value.Length;

            var slice = AsSlice(position, Length - position);

            value.Span.CopyTo(slice.Span);

            return slice;
        }

        /// <summary>
        /// Appends an item.
        /// </summary>
        /// <param name="value">The item to append.</param>
        public void AddItem(T value)
        {
            var index = Length++;
            _data![index] = value;
        }

        /// <summary>
        /// Clears the array.
        /// Allocated memory is left intact for future usage.
        /// </summary>
        public void Clear()
        {
#if NET6_0_OR_GREATER
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                ClearArray();
            }
            else
            {
                _size = 0;
            }
#else
            ClearArray();
#endif
        }

        private void ClearArray()
        {
            var size = _size;
            _size = 0;
            if (size > 0)
            {
                Array.Clear(_data!, 0, size);
            }
        }

        private void EnsureCapacity(int min)
        {
            var length = _data?.Length ?? 0;

            if (length >= min)
            {
                return;
            }

            // Same expansion algorithm as List<T>.
            var newCapacity = length == 0 ? DefaultCapacity : (uint)length * 2u;

            if (newCapacity > MaxCoreClrArrayLength)
            {
                newCapacity = MaxCoreClrArrayLength;
            }

            if (newCapacity < min)
            {
                newCapacity = (uint)min;
            }
            
            var array = new T[newCapacity];

            if (_size > 0)
            {
                Array.Copy(_data!, array, _size);
            }

            _data = array;
        }

        /// <summary>
        /// Returns the current state of the array as a slice.
        /// </summary>
        /// <returns>The <see cref="ArraySlice{T}"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArraySlice<T> AsSlice() => AsSlice(Length);

        /// <summary>
        /// Returns the current state of the array as a slice.
        /// </summary>
        /// <param name="length">The number of items in the slice.</param>
        /// <returns>The <see cref="ArraySlice{T}"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArraySlice<T> AsSlice(int length) => new ArraySlice<T>(_data!, 0, length);

        /// <summary>
        /// Returns the current state of the array as a slice.
        /// </summary>
        /// <param name="start">The index at which to begin the slice.</param>
        /// <param name="length">The number of items in the slice.</param>
        /// <returns>The <see cref="ArraySlice{T}"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArraySlice<T> AsSlice(int start, int length) => new ArraySlice<T>(_data!, start, length);

        /// <summary>
        /// Returns the current state of the array as a span.
        /// </summary>
        /// <returns>The <see cref="Span{T}"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan() => _data.AsSpan(0, _size);
    }
}
