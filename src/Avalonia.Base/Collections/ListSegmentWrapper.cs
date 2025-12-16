using System;
using System.Collections;
using System.Collections.Generic;

namespace Avalonia.Collections
{
    /// <summary>
    /// A reusable IList wrapper over a pooled array of items.
    /// Used to avoid allocations in NotifyCollectionChangedEventArgs when removing ranges.
    /// This class is not thread-safe - use ThreadStatic instances.
    /// </summary>
    internal sealed class ListSegmentWrapper<T> : IList
    {
        private T[]? _items;
        private int _count;

        /// <summary>
        /// Sets the wrapper to contain the specified items from the source list.
        /// Items are copied to an internal pooled array.
        /// </summary>
        public void Set(List<T> source, int offset, int count)
        {
            if (_items == null || _items.Length < count)
            {
                // Grow the pooled array if needed (powers of 2 to reduce reallocation)
                int newSize = Math.Max(8, count);
                newSize = (int)Math.Pow(2, Math.Ceiling(Math.Log(newSize, 2)));
                _items = new T[newSize];
            }
            
            // Copy items from source to pooled array
            for (int i = 0; i < count; i++)
            {
                _items[i] = source[offset + i];
            }
            _count = count;
        }

        public void Clear()
        {
            // Clear references to allow GC
            if (_items != null)
            {
                Array.Clear(_items, 0, _count);
            }
            _count = 0;
        }

        public int Count => _count;

        public bool IsFixedSize => true;

        public bool IsReadOnly => true;

        public bool IsSynchronized => false;

        public object SyncRoot => this;

        public object? this[int index]
        {
            get
            {
                if ((uint)index >= (uint)_count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return _items![index];
            }
            set => throw new NotSupportedException();
        }

        public int Add(object? value) => throw new NotSupportedException();

        public void Clear(int _) => throw new NotSupportedException();

        void IList.Clear() => throw new NotSupportedException();

        public bool Contains(object? value)
        {
            for (int i = 0; i < _count; i++)
            {
                if (Equals(_items![i], value))
                    return true;
            }
            return false;
        }

        public int IndexOf(object? value)
        {
            for (int i = 0; i < _count; i++)
            {
                if (Equals(_items![i], value))
                    return i;
            }
            return -1;
        }

        public void Insert(int index, object? value) => throw new NotSupportedException();

        public void Remove(object? value) => throw new NotSupportedException();

        public void RemoveAt(int index) => throw new NotSupportedException();

        public void CopyTo(Array array, int index)
        {
            for (int i = 0; i < _count; i++)
            {
                array.SetValue(_items![i], index + i);
            }
        }

        public IEnumerator GetEnumerator()
        {
            for (int i = 0; i < _count; i++)
            {
                yield return _items![i];
            }
        }
    }
}
