using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Avalonia.FreeDesktop.Atspi;
using Tmds.DBus;

#nullable enable

namespace Avalonia.FreeDesktop
{
    internal class AtspiCache : ICache
    {
        private readonly AtspiRoot _root;
        private readonly ObservableCollection<CacheItem> _items = new ObservableCollection<CacheItem>();

        public AtspiCache(AtspiRoot root)
        {
            _root = root;
        }
        
        public ObjectPath ObjectPath => "/org/a11y/atspi/cache";

        public void Add(AtspiContext item) => _items.Add(item.ToCacheItem());
        public Task<CacheItem[]> GetItemsAsync() => Task.FromResult(_items.ToArray());
        public Task<IDisposable> WatchAddAccessibleAsync(Action<CacheItem> handler, Action<Exception>? onError = null)
        {
            void Listener(object s, NotifyCollectionChangedEventArgs e)
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                    foreach (CacheItem i in e.NewItems) handler(i);
            }

            _items.CollectionChanged += Listener;
            return Task.FromResult(Disposable.Create(() => _items.CollectionChanged -= Listener));
        }

        public Task<IDisposable> WatchRemoveAccessibleAsync(Action<CacheItem> handler, Action<Exception>? onError = null)
        {
            void Listener(object s, NotifyCollectionChangedEventArgs e)
            {
                if (e.Action == NotifyCollectionChangedAction.Remove)
                    foreach (CacheItem i in e.NewItems) handler(i);
            }

            _items.CollectionChanged += Listener;
            return Task.FromResult(Disposable.Create(() => _items.CollectionChanged -= Listener));
        }
    }
}
