using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Collections;

namespace Avalonia.Utilities
{
    internal class SafeEnumerableList<T> : IAvaloniaList<T>, IList, INotifyCollectionChanged
    {
        private AvaloniaList<T> _list = new();
        private int _generation;
        private int _enumCount = 0;

        public event NotifyCollectionChangedEventHandler? CollectionChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        //for test purposes
        internal AvaloniaList<T> Inner => _list;
        public SafeEnumerableList()
        {
            _list.CollectionChanged += List_CollectionChanged;
            _list.PropertyChanged += List_PropertyChanged;
        }

        private void List_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        private void List_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }

        public SafeListEnumerator GetEnumerator() => new SafeListEnumerator(this, _list);

        public int IndexOf(T item)
        {
            return _list.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            GetList().Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            GetList().RemoveAt(index);
        }

        public void Add(T item)
        {
            GetList().Add(item);
        }

        public void Clear()
        {
            GetList().Clear();
        }

        public bool Contains(T item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return GetList().Remove(item);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private AvaloniaList<T> GetList()
        {
            if (_enumCount > 0)
            {
                _list.CollectionChanged -= List_CollectionChanged;
                _list = new(_list)
                {
                    Validator = Validator,
                    ResetBehavior = ResetBehavior,
                };
                _list.CollectionChanged += List_CollectionChanged;
                ++_generation;
                _enumCount = 0;
            }
            return _list;
        }

        public void AddRange(IEnumerable<T> items)
        {
            GetList().AddRange(items);
        }

        public void InsertRange(int index, IEnumerable<T> items)
        {
            GetList().InsertRange(index, items);
        }

        public void Move(int oldIndex, int newIndex)
        {
            GetList().Move(oldIndex, newIndex);
        }

        public void MoveRange(int oldIndex, int count, int newIndex)
        {
            GetList().MoveRange(oldIndex, count, newIndex);
        }

        public void RemoveAll(IEnumerable<T> items)
        {
            GetList().RemoveAll(items);
        }

        public void RemoveRange(int index, int count)
        {
            GetList().RemoveRange(index, count);
        }

        public int Add(object? value)
        {
            return ((IList)GetList()).Add(value);
        }

        public bool Contains(object? value)
        {
            return ((IList)_list).Contains(value);
        }

        public int IndexOf(object? value)
        {
            return ((IList)_list).IndexOf(value);
        }

        public void Insert(int index, object? value)
        {
            ((IList)GetList()).Insert(index, value);
        }

        public void Remove(object? value)
        {
            ((IList)GetList()).Remove(value);
        }

        public void CopyTo(Array array, int index)
        {
            ((ICollection)_list).CopyTo(array, index);
        }

        public int Count => _list.Count;

        public bool IsReadOnly => ((ICollection<T>)_list).IsReadOnly;

        public ResetBehavior ResetBehavior { get => _list.ResetBehavior; set => _list.ResetBehavior = value; }
        public IAvaloniaListItemValidator<T>? Validator { get => _list.Validator; set => _list.Validator = value; }

        public bool IsFixedSize => ((IList)_list).IsFixedSize;

        public bool IsSynchronized => ((ICollection)_list).IsSynchronized;

        public object SyncRoot => ((ICollection)_list).SyncRoot;

        object? IList.this[int index] { get => ((IList)_list)[index]; set => ((IList)GetList())[index] = value; }
        public T this[int index] { get => _list[index]; set => GetList()[index] = value; }

        public struct SafeListEnumerator : IEnumerator<T>
        {
            private readonly SafeEnumerableList<T> _owner;
            private readonly int _generation;
            private readonly IEnumerator<T> _enumerator;

            public SafeListEnumerator(SafeEnumerableList<T> owner, AvaloniaList<T> list)
            {
                _owner = owner;
                _generation = owner._generation;
                ++owner._enumCount;
                _enumerator = list.GetEnumerator();
            }

            public bool MoveNext()
                => _enumerator.MoveNext();

            public T Current => _enumerator.Current;

            object IEnumerator.Current => _enumerator.Current!;

            public void Reset() => throw new InvalidOperationException();

            public void Dispose()
            {
                if (_generation == _owner._generation)
                {
                    --_owner._enumCount;
                }
            }
        }
    }
}
