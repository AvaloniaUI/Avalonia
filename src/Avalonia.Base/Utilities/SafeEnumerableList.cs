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
    internal class SafeEnumerableList<T> : IAvaloniaList<T>, INotifyCollectionChanged
    {
        private AvaloniaList<T> _list = new();
        private int _generation;
        private int _enumCount = 0;

        public event NotifyCollectionChangedEventHandler? CollectionChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

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
            ((IAvaloniaList<T>)GetList()).AddRange(items);
        }

        public void InsertRange(int index, IEnumerable<T> items)
        {
            ((IAvaloniaList<T>)GetList()).InsertRange(index, items);
        }

        public void Move(int oldIndex, int newIndex)
        {
            ((IAvaloniaList<T>)GetList()).Move(oldIndex, newIndex);
        }

        public void MoveRange(int oldIndex, int count, int newIndex)
        {
            ((IAvaloniaList<T>)GetList()).MoveRange(oldIndex, count, newIndex);
        }

        public void RemoveAll(IEnumerable<T> items)
        {
            ((IAvaloniaList<T>)GetList()).RemoveAll(items);
        }

        public void RemoveRange(int index, int count)
        {
            ((IAvaloniaList<T>)GetList()).RemoveRange(index, count);
        }

        public int Count => _list.Count;

        public bool IsReadOnly => ((ICollection<T>)_list).IsReadOnly;

        public ResetBehavior ResetBehavior { get => _list.ResetBehavior; set => _list.ResetBehavior = value; }
        public IAvaloniaListItemValidator<T>? Validator { get => _list.Validator; set => _list.Validator = value; }

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
