using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Avalonia.Controls.Repeaters
{
    public class ItemsSourceView : INotifyCollectionChanged, IDisposable
    {
        private readonly IList _inner;
        private INotifyCollectionChanged _notifyCollectionChanged;
        private int _cachedSize = -1;

        public ItemsSourceView(IEnumerable source)
        {
            Contract.Requires<ArgumentNullException>(source != null);

            _inner = source as IList;

            if (_inner == null && source is IEnumerable<object> objectEnumerable)
            {
                _inner = new List<object>(objectEnumerable);
            }
            else
            {
                _inner = new List<object>(source.Cast<object>());
            }

            ListenToCollectionChanges();
        }

        public int Count
        {
            get
            {
                if (_cachedSize == -1)
                {
                    _cachedSize = _inner.Count;
                }

                return _cachedSize;
            }
        }

        public bool HasKeyIndexMapping => false;


        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public void Dispose()
        {
            if (_notifyCollectionChanged != null)
            {
                _notifyCollectionChanged.CollectionChanged -= OnCollectionChanged;
            }
        }

        public object GetAt(int index) => _inner[index];

        public string KeyFromIndex(int index)
        {
            throw new NotImplementedException();
        }

        public int IndexFromKey(string key)
        {
            throw new NotImplementedException();
        }

        protected void OnItemsSourceChanged(NotifyCollectionChangedEventArgs args)
        {
            _cachedSize = _inner.Count;
            CollectionChanged?.Invoke(this, args);
        }

        private void ListenToCollectionChanges()
        {
            if (_inner is INotifyCollectionChanged incc)
            {
                incc.CollectionChanged += OnCollectionChanged;
                _notifyCollectionChanged = incc;
            }
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnItemsSourceChanged(e);
        }
    }
}
