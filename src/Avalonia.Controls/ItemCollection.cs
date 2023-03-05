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
    public class ItemCollection : IList<object?>,
        IReadOnlyList<object?>,
        IList,
        INotifyCollectionChanged,
        ICollectionChangedListener
    {
        private IList? _inner;
        private Mode _mode;

        internal ItemCollection()
        {
        }

        public object? this[int index]
        {
            get
            {
                if (Inner is null)
                    ThrowIndexOutOfRange();
                return Inner[index];
            }

            set => InnerWritable[index] = value;
        }

        public bool IsReadOnly => _mode == Mode.ItemsSource;
        public int Count => Inner?.Count ?? 0;
        internal IList? Source => _mode == Mode.Items ? this : _inner;

        private IList? Inner
        {
            get => _inner;
            set
            {
                if (_inner != value)
                {
                    if (_inner is not null)
                    {
                        RaiseCollectionChanged(new(NotifyCollectionChangedAction.Remove, _inner, 0));
                        if (_inner is INotifyCollectionChanged inccOld)
                            CollectionChangedEventManager.Instance.RemoveListener(inccOld, this);
                    }
                    
                    _inner = value;

                    if (_inner is not null)
                    {
                        if (_inner is INotifyCollectionChanged inccNew)
                            CollectionChangedEventManager.Instance.AddListener(inccNew, this);
                        RaiseCollectionChanged(new(NotifyCollectionChangedAction.Add, _inner, 0));
                    }
                }
            }
        }
       

        private IList InnerWritable
        {
            get
            {
                if (IsReadOnly)
                    ThrowIsItemsSource();
                return _inner ??= CreateDefaultCollection();
            }
        }

        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => this;
        bool IList.IsFixedSize => false;

        public event NotifyCollectionChangedEventHandler? CollectionChanged;
        internal event NotifyCollectionChangedEventHandler? PostCollectionChanged;

        public void Add(object? value) => InnerWritable.Add(value);
        public void Clear() => InnerWritable.Clear();
        public bool Contains(object? value) => Inner?.Contains(value) ?? false;
        public void CopyTo(Array array, int index) => Inner?.CopyTo(array, index);
        public int IndexOf(object? value) => Inner?.IndexOf(value) ?? -1;
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
            static IEnumerator<object?> EnumerateItems(IList inner)
            {
                foreach (var i in inner)
                    yield return i;
            }

            return Inner switch
            {
                null => Enumerable.Empty<object?>().GetEnumerator(),
                IEnumerable<object?> e => e.GetEnumerator(),
                _ => EnumerateItems(Inner)
            };
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Inner?.GetEnumerator() ?? Enumerable.Empty<object?>().GetEnumerator();
        }

        int IList.Add(object? value) => InnerWritable.Add(value);
        void IList.Remove(object? value) => InnerWritable.Remove(value);

        void ICollection<object?>.CopyTo(object?[] array, int arrayIndex)
        {
            if (Inner is ICollection<object?> inner)
                inner.CopyTo(array, arrayIndex);
            else
                throw new NotImplementedException();
        }

        void ICollectionChangedListener.PreChanged(INotifyCollectionChanged sender, NotifyCollectionChangedEventArgs e)
        {
        }

        void ICollectionChangedListener.Changed(INotifyCollectionChanged sender, NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }

        void ICollectionChangedListener.PostChanged(INotifyCollectionChanged sender, NotifyCollectionChangedEventArgs e)
        {
            PostCollectionChanged?.Invoke(this, e);
        }

        internal IList? GetItemsPropertyValue()
        {
            return _mode == Mode.ObsoleteItemsSetter ? Inner : this;
        }

        internal void SetItems(IList? items)
        {
            _mode = Mode.ObsoleteItemsSetter;
            Inner = items;
        }

        internal void SetItemsSource(IEnumerable? value)
        {
            _mode = value is not null ? Mode.ItemsSource : Mode.Items;
            Inner = value switch
            {
                IList list => list,
                IEnumerable<object> iObj => new List<object>(iObj),
                null => new List<object>(),
                _ => new List<object>(value.Cast<object>())
            };
        }

        private AvaloniaList<object?> CreateDefaultCollection()
        {
            var result = new AvaloniaList<object?>();
            result.ResetBehavior = ResetBehavior.Remove;
            CollectionChangedEventManager.Instance.AddListener(result, this);
            return result;
        }

        private void RaiseCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
            PostCollectionChanged?.Invoke(this, e);
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
