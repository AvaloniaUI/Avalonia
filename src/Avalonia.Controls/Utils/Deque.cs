using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Avalonia.Controls.Utils
{
    /// <summary>
    /// A double-ended queue (deque) that supports O(1) amortized insertions and removals
    /// at both the front and back, as well as O(1) random access by index.
    /// </summary>
    /// <typeparam name="T">The type of elements in the deque.</typeparam>
    internal class Deque<T> : IReadOnlyList<T>
    {
        private T[] _buffer;
        private int _head;
        private int _count;

        /// <summary>
        /// Initializes a new instance of the <see cref="Deque{T}"/> class.
        /// </summary>
        /// <param name="capacity">The initial capacity of the deque.</param>
        public Deque(int capacity = 4)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            _buffer = capacity > 0 ? new T[capacity] : Array.Empty<T>();
            _head = 0;
            _count = 0;
        }

        /// <summary>
        /// Gets the number of elements in the deque.
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The element at the specified index.</returns>
        public T this[int index]
        {
            get
            {
                if ((uint)index >= (uint)_count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return _buffer[GetBufferIndex(index)];
            }
            set
            {
                if ((uint)index >= (uint)_count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                _buffer[GetBufferIndex(index)] = value;
            }
        }

        /// <summary>
        /// Adds an element to the front of the deque. O(1) amortized.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void PushFront(T item)
        {
            EnsureCapacity(_count + 1);
            _head = DecrementIndex(_head);
            _buffer[_head] = item;
            _count++;
        }

        /// <summary>
        /// Adds an element to the back of the deque. O(1) amortized.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void PushBack(T item)
        {
            EnsureCapacity(_count + 1);
            _buffer[GetBufferIndex(_count)] = item;
            _count++;
        }

        /// <summary>
        /// Removes and returns the element at the front of the deque. O(1).
        /// </summary>
        /// <returns>The element that was removed.</returns>
        public T PopFront()
        {
            if (_count == 0)
                throw new InvalidOperationException("Deque is empty.");

            var item = _buffer[_head];
            _buffer[_head] = default!;
            _head = IncrementIndex(_head);
            _count--;
            return item;
        }

        /// <summary>
        /// Removes and returns the element at the back of the deque. O(1).
        /// </summary>
        /// <returns>The element that was removed.</returns>
        public T PopBack()
        {
            if (_count == 0)
                throw new InvalidOperationException("Deque is empty.");

            _count--;
            var index = GetBufferIndex(_count);
            var item = _buffer[index];
            _buffer[index] = default!;
            return item;
        }

        /// <summary>
        /// Removes a range of elements from the front of the deque. O(count).
        /// </summary>
        /// <param name="count">The number of elements to remove.</param>
        public void RemoveFromFront(int count)
        {
            if (count < 0 || count > _count)
                throw new ArgumentOutOfRangeException(nameof(count));

            // Clear the removed elements to allow GC
            for (var i = 0; i < count; i++)
            {
                _buffer[GetBufferIndex(i)] = default!;
            }

            _head = GetBufferIndex(count);
            _count -= count;
        }

        /// <summary>
        /// Removes a range of elements from the back of the deque. O(count).
        /// </summary>
        /// <param name="count">The number of elements to remove.</param>
        public void RemoveFromBack(int count)
        {
            if (count < 0 || count > _count)
                throw new ArgumentOutOfRangeException(nameof(count));

            // Clear the removed elements to allow GC
            for (var i = _count - count; i < _count; i++)
            {
                _buffer[GetBufferIndex(i)] = default!;
            }

            _count -= count;
        }

        /// <summary>
        /// Removes all elements from the deque.
        /// </summary>
        public void Clear()
        {
            if (_count > 0)
            {
                // Clear the buffer to allow GC
                if (_head + _count <= _buffer.Length)
                {
                    Array.Clear(_buffer, _head, _count);
                }
                else
                {
                    Array.Clear(_buffer, _head, _buffer.Length - _head);
                    Array.Clear(_buffer, 0, (_head + _count) % _buffer.Length);
                }
            }

            _head = 0;
            _count = 0;
        }

        /// <summary>
        /// Returns the index of the specified item, or -1 if not found.
        /// </summary>
        /// <param name="item">The item to find.</param>
        /// <returns>The index of the item, or -1 if not found.</returns>
        public int IndexOf(T item)
        {
            var comparer = EqualityComparer<T>.Default;
            for (var i = 0; i < _count; i++)
            {
                if (comparer.Equals(_buffer[GetBufferIndex(i)], item))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Inserts multiple copies of an item at the specified index.
        /// This is an O(n) operation where n is the number of elements that need to be shifted.
        /// </summary>
        /// <param name="index">The index at which to insert.</param>
        /// <param name="item">The item to insert.</param>
        /// <param name="count">The number of copies to insert.</param>
        public void InsertMany(int index, T item, int count)
        {
            if (index < 0 || index > _count)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0)
                return;

            EnsureCapacity(_count + count);

            // Decide whether to shift front or back based on which is smaller
            if (index <= _count / 2)
            {
                // Shift front elements backward
                for (var i = 0; i < index; i++)
                {
                    var oldIdx = GetBufferIndex(i);
                    var newIdx = GetBufferIndex(i - count);
                    _buffer[newIdx] = _buffer[oldIdx];
                }
                _head = DecrementIndexBy(_head, count);
            }
            else
            {
                // Shift back elements forward
                for (var i = _count - 1; i >= index; i--)
                {
                    var oldIdx = GetBufferIndex(i);
                    var newIdx = GetBufferIndex(i + count);
                    _buffer[newIdx] = _buffer[oldIdx];
                }
            }

            // Insert the new items
            for (var i = 0; i < count; i++)
            {
                _buffer[GetBufferIndex(index + i)] = item;
            }

            _count += count;
        }

        /// <summary>
        /// Removes a range of elements starting at the specified index.
        /// </summary>
        /// <param name="index">The starting index.</param>
        /// <param name="count">The number of elements to remove.</param>
        public void RemoveRange(int index, int count)
        {
            if (index < 0 || index > _count)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0 || index + count > _count)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 0)
                return;

            var elementsAfter = _count - index - count;
            var elementsBefore = index;

            // Decide whether to shift front or back based on which is smaller
            if (elementsBefore <= elementsAfter)
            {
                // Shift front elements forward
                for (var i = index - 1; i >= 0; i--)
                {
                    var oldIdx = GetBufferIndex(i);
                    var newIdx = GetBufferIndex(i + count);
                    _buffer[newIdx] = _buffer[oldIdx];
                    _buffer[oldIdx] = default!;
                }
                _head = IncrementIndexBy(_head, count);
            }
            else
            {
                // Shift back elements backward
                for (var i = index; i < _count - count; i++)
                {
                    var oldIdx = GetBufferIndex(i + count);
                    var newIdx = GetBufferIndex(i);
                    _buffer[newIdx] = _buffer[oldIdx];
                    _buffer[oldIdx] = default!;
                }
            }

            _count -= count;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the deque.
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < _count; i++)
            {
                yield return _buffer[GetBufferIndex(i)];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetBufferIndex(int index)
        {
            var i = _head + index;
            if (i >= _buffer.Length)
                i -= _buffer.Length;
            else if (i < 0)
                i += _buffer.Length;
            return i;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int IncrementIndex(int index)
        {
            return (index + 1) % _buffer.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int DecrementIndex(int index)
        {
            return (index - 1 + _buffer.Length) % _buffer.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int IncrementIndexBy(int index, int count)
        {
            return (index + count) % _buffer.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int DecrementIndexBy(int index, int count)
        {
            return (index - count % _buffer.Length + _buffer.Length) % _buffer.Length;
        }

        private void EnsureCapacity(int min)
        {
            if (_buffer.Length < min)
            {
                var newCapacity = _buffer.Length == 0 ? 4 : _buffer.Length * 2;
                if (newCapacity < min)
                    newCapacity = min;
                Resize(newCapacity);
            }
        }

        private void Resize(int newCapacity)
        {
            var newBuffer = new T[newCapacity];

            // Copy elements in logical order to the new buffer
            if (_count > 0)
            {
                if (_head + _count <= _buffer.Length)
                {
                    // Elements are contiguous
                    Array.Copy(_buffer, _head, newBuffer, 0, _count);
                }
                else
                {
                    // Elements wrap around
                    var firstPart = _buffer.Length - _head;
                    Array.Copy(_buffer, _head, newBuffer, 0, firstPart);
                    Array.Copy(_buffer, 0, newBuffer, firstPart, _count - firstPart);
                }
            }

            _buffer = newBuffer;
            _head = 0;
        }
    }
}
