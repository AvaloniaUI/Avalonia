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
        where T : struct
    {
        private const int DefaultCapacity = 4;
        private const int MaxCoreClrArrayLength = 0x7FeFFFFF;

        // Starts out null, initialized on first Add.
        private T[] _data;
        private int _size;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArrayBuilder{T}"/> struct.
        /// </summary>
        /// <param name="capacity">The initial capacity of the array.</param>
        public ArrayBuilder(int capacity)
            : this()
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof (capacity));
            }

            _data = new T[capacity];
            _size = capacity;
        }

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
                return ref _data![index];
            }
        }

        /// <summary>
        /// Adds the given item to the array.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void Add(T item)
        {
            var position = _size;

            // Expand the array.
            Length++;
            
            _data![position] = item;
        }

        /// <summary>
        /// Appends a given number of empty items to the array returning
        /// the items as a slice.
        /// </summary>
        /// <param name="length">The number of items in the slice.</param>
        /// <param name="clear">Whether to clear the new slice, Defaults to <see langword="true"/>.</param>
        /// <returns>The <see cref="Slice{T}"/>.</returns>
        public Slice<T> Add(int length, bool clear = true)
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
        /// <returns>The <see cref="Slice{T}"/>.</returns>
        public Slice<T> Add(in ReadOnlySlice<T> value)
        {
            var position = _size;

            // Expand the array.
            Length += value.Length;

            var slice = AsSlice(position, Length - position);
            
            value.CopyTo(slice.Span);

            return slice;
        }

        /// <summary>
        /// Clears the array.
        /// Allocated memory is left intact for future usage.
        /// </summary>
        public void Clear()
        {
            // No need to actually clear since we're not allowing reference types.
            _size = 0;
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
        /// <returns>The <see cref="Slice{T}"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Slice<T> AsSlice() => AsSlice(Length);

        /// <summary>
        /// Returns the current state of the array as a slice.
        /// </summary>
        /// <param name="length">The number of items in the slice.</param>
        /// <returns>The <see cref="Slice{T}"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Slice<T> AsSlice(int length) => new Slice<T>(_data!, 0, length);

        /// <summary>
        /// Returns the current state of the array as a slice.
        /// </summary>
        /// <param name="start">The index at which to begin the slice.</param>
        /// <param name="length">The number of items in the slice.</param>
        /// <returns>The <see cref="Slice{T}"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Slice<T> AsSlice(int start, int length) => new Slice<T>(_data!, start, length);
    }
}
