// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;

namespace Perspex.Styling
{
    public class Classes : ICollection<string>, INotifyCollectionChanged
    {
        private readonly List<string> _inner;

        private readonly Subject<NotifyCollectionChangedEventArgs> _beforeChanged
            = new Subject<NotifyCollectionChangedEventArgs>();

        private readonly Subject<NotifyCollectionChangedEventArgs> _changed
            = new Subject<NotifyCollectionChangedEventArgs>();

        private readonly Subject<NotifyCollectionChangedEventArgs> _afterChanged
            = new Subject<NotifyCollectionChangedEventArgs>();

        public Classes()
        {
            _inner = new List<string>();
        }

        public Classes(params string[] classes)
        {
            _inner = new List<string>(classes);
        }

        public Classes(IEnumerable<string> classes)
        {
            _inner = new List<string>(classes);
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public int Count => _inner.Count;

        public bool IsReadOnly => false;

        public IObservable<NotifyCollectionChangedEventArgs> BeforeChanged => _beforeChanged;

        public IObservable<NotifyCollectionChangedEventArgs> Changed => _changed;

        public IObservable<NotifyCollectionChangedEventArgs> AfterChanged => _afterChanged;

        public void Add(string item)
        {
            Add(Enumerable.Repeat(item, 1));
        }

        public void Add(params string[] items)
        {
            Add((IEnumerable<string>)items);
        }

        public void Add(IEnumerable<string> items)
        {
            items = items.Except(_inner);

            NotifyCollectionChangedEventArgs e = new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Add,
                items);

            _beforeChanged.OnNext(e);
            _inner.AddRange(items);
            RaiseChanged(e);
        }

        public void Clear()
        {
            NotifyCollectionChangedEventArgs e = new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Reset);

            _beforeChanged.OnNext(e);
            _inner.Clear();
            RaiseChanged(e);
        }

        public bool Contains(string item)
        {
            return _inner.Contains(item);
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            _inner.CopyTo(array, arrayIndex);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _inner.GetEnumerator();
        }

        public override string ToString()
        {
            return string.Join(" ", this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _inner.GetEnumerator();
        }

        public bool Remove(string item)
        {
            return Remove(Enumerable.Repeat(item, 1));
        }

        public bool Remove(params string[] items)
        {
            return Remove((IEnumerable<string>)items);
        }

        public bool Remove(IEnumerable<string> items)
        {
            items = items.Intersect(_inner);

            if (items.Any())
            {
                NotifyCollectionChangedEventArgs e = new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Remove,
                    items);

                _beforeChanged.OnNext(e);

                foreach (string item in items)
                {
                    _inner.Remove(item);
                }

                RaiseChanged(e);
                return true;
            }
            else
            {
                return false;
            }
        }

        private void RaiseChanged(NotifyCollectionChangedEventArgs e)
        {
            if (CollectionChanged != null)
            {
                CollectionChanged(this, e);
            }

            _changed.OnNext(e);
            _afterChanged.OnNext(e);
        }
    }
}
