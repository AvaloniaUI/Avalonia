using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Avalonia.Controls
{
    public class ItemCollection : IList<object?>, IList, IReadOnlyList<object?>
    {
        private IList? _inner;
        private bool _isItemsSource;

        internal ItemCollection()
        {
        }

        public object? this[int index]
        {
            get
            {
                if (_inner is null)
                    ThrowIndexOutOfRange();
                return _inner[index];
            }

            set => InnerWritable[index] = value;
        }

        public bool IsReadOnly => _isItemsSource;
        public int Count => _inner?.Count ?? 0;

        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => this;
        bool IList.IsFixedSize => false;

        private IList InnerWritable
        {
            get
            {
                if (_isItemsSource)
                    ThrowIsItemsSource();
                return _inner ??= new List<object>();
            }
        }

        public void Add(object? value) => InnerWritable.Add(value);
        public void Clear() => InnerWritable.Clear();
        public bool Contains(object? value) => _inner?.Contains(value) ?? false;
        public void CopyTo(Array array, int index) => _inner?.CopyTo(array, index);
        public int IndexOf(object? value) => _inner?.IndexOf(value) ?? -1;
        public void Insert(int index, object? value) => InnerWritable.Insert(index, value);
        public void RemoveAt(int index) => InnerWritable.RemoveAt(index);

        public bool Remove(object? value)
        {
            var c = Count;
            InnerWritable.Remove(value);
            return Count < c;
        }

        public IEnumerator<object?> GetEnumerator()
        {
            IEnumerator<object?> EnumerateItems()
            {
                foreach (var i in _inner)
                    yield return i;
            }

            if (_inner is null)
                return Enumerable.Empty<object?>().GetEnumerator();
            else if (_inner is IEnumerable<object?> e)
                return e.GetEnumerator();
            else
                return EnumerateItems();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _inner?.GetEnumerator() ?? Enumerable.Empty<object?>().GetEnumerator();
        }

        int IList.Add(object? value) => InnerWritable.Add(value);
        void IList.Remove(object? value) => InnerWritable.Remove(value);

        internal void SetItemsSource(ItemsSourceView? value)
        {
            _inner = value;
            _isItemsSource = value is not null;
        }

        [DoesNotReturn]
        private static void ThrowIndexOutOfRange() => throw new IndexOutOfRangeException();

        [DoesNotReturn]
        private static void ThrowIsItemsSource()
        {
            throw new InvalidOperationException(
                "Operation is not valid while ItemsSource is in use." +
                "Access and modify elements with ItemsControl.ItemsSource instead.");
        }

        void ICollection<object?>.CopyTo(object?[] array, int arrayIndex)
        {
            if (_inner is ICollection<object?> inner)
                inner.CopyTo(array, arrayIndex);
            else
                throw new NotImplementedException();
        }
    }
}
