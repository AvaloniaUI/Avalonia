using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Utils;

namespace Avalonia.Controls
{
    public class ItemCollection : IList<object?>, IList, IReadOnlyList<object?>
    {
        private readonly ItemsControl _owner;
        private IList? _inner;
        private Mode _mode;

        internal ItemCollection(ItemsControl owner)
        {
            _owner = owner;
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

        public bool IsReadOnly => _mode == Mode.ItemsSource;
        public int Count => _inner?.Count ?? 0;

        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => this;
        bool IList.IsFixedSize => false;

        private IList InnerWritable
        {
            get
            {
                if (IsReadOnly)
                    ThrowIsItemsSource();
                return _inner ??= CreateDefaultCollection();
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

        void ICollection<object?>.CopyTo(object?[] array, int arrayIndex)
        {
            if (_inner is ICollection<object?> inner)
                inner.CopyTo(array, arrayIndex);
            else
                throw new NotImplementedException();
        }

        internal IList? GetItemsPropertyValue()
        {
            return _mode == Mode.ObsoleteItemsSetter ? _inner : this;
        }

        internal void SetItems(IList? items)
        {
            if (_inner is not null)
            {
                _owner.RemoveControlItemsFromLogicalChildren(_inner);
                if (_inner is INotifyCollectionChanged inccOld)
                    inccOld.CollectionChanged -= _owner.OnItemsCollectionChanged;
            }

            _inner = items;
            _mode = Mode.ObsoleteItemsSetter;

            if (_inner is not null)
            {
                _owner.AddControlItemsToLogicalChildren(_inner);
                if (_inner is INotifyCollectionChanged inccNew)
                    inccNew.CollectionChanged += _owner.OnItemsCollectionChanged; 
            }
        }

        internal void SetItemsSource(IEnumerable? value)
        {
            _inner = value switch
            {
                IList list => list,
                IEnumerable<object> iObj => new List<object>(iObj),
                null => new List<object>(),
                _ => new List<object>(value.Cast<object>())
            };

            _mode = value is not null ? Mode.ItemsSource : Mode.Items;
        }

        private AvaloniaList<object?> CreateDefaultCollection()
        {
            var result = new AvaloniaList<object?>();
            result.ResetBehavior = ResetBehavior.Remove;
            result.CollectionChanged += _owner.OnItemsCollectionChanged;
            return result;
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

        private enum Mode
        {
            Items,
            ItemsSource,
            ObsoleteItemsSetter,
        }
    }
}
