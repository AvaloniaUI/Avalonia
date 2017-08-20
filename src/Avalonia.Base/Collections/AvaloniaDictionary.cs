// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace Avalonia.Collections
{
    /// <summary>
    /// A notifying dictionary.
    /// </summary>
    /// <typeparam name="TKey">The type of the dictionary key.</typeparam>
    /// <typeparam name="TValue">The type of the dictionary value.</typeparam>
    public class AvaloniaDictionary<TKey, TValue> : IDictionary<TKey, TValue>,
        INotifyCollectionChanged,
        INotifyPropertyChanged
    {
        private Dictionary<TKey, TValue> _inner;

        /// <summary>
        /// Initializes a new instance of the <see cref="AvaloniaDictionary{TKey, TValue}"/> class.
        /// </summary>
        public AvaloniaDictionary()
        {
            _inner = new Dictionary<TKey, TValue>();
        }

        /// <summary>
        /// Occurs when the collection changes.
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// Raised when a property on the collection changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <inheritdoc/>
        public int Count => _inner.Count;

        /// <inheritdoc/>
        public bool IsReadOnly => false;

        /// <inheritdoc/>
        public ICollection<TKey> Keys => _inner.Keys;

        /// <inheritdoc/>
        public ICollection<TValue> Values => _inner.Values;

        /// <summary>
        /// Gets or sets the named resource.
        /// </summary>
        /// <param name="key">The resource key.</param>
        /// <returns>The resource, or null if not found.</returns>
        public TValue this[TKey key]
        {
            get
            {
                return _inner[key];
            }

            set
            {
                TValue old;
                bool replace = _inner.TryGetValue(key, out old);
                _inner[key] = value;

                if (replace)
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs($"Item[{key}]"));

                    if (CollectionChanged != null)
                    {
                        var e = new NotifyCollectionChangedEventArgs(
                            NotifyCollectionChangedAction.Replace,
                            new KeyValuePair<TKey, TValue>(key, value),
                            new KeyValuePair<TKey, TValue>(key, old));
                        CollectionChanged(this, e);
                    }
                }
                else
                {
                    NotifyAdd(key, value);
                }
            }
        }

        /// <inheritdoc/>
        public void Add(TKey key, TValue value)
        {
            _inner.Add(key, value);
            NotifyAdd(key, value);
        }

        /// <inheritdoc/>
        public void Clear()
        {
            var old = _inner;

            _inner = new Dictionary<TKey, TValue>();

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Count"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs($"Item[]"));
            

            if (CollectionChanged != null)
            {
                var e = new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Remove,
                    old.ToList(),
                    -1);
                CollectionChanged(this, e);
            }
        }

        /// <inheritdoc/>
        public bool ContainsKey(TKey key)
        {
            return _inner.ContainsKey(key);
        }

        /// <inheritdoc/>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((IDictionary<TKey, TValue>)_inner).CopyTo(array, arrayIndex);
        }

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _inner.GetEnumerator();
        }

        /// <inheritdoc/>
        public bool Remove(TKey key)
        {
            TValue value;

            if (_inner.TryGetValue(key, out value))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Count"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs($"Item[{key}]"));
                
                if (CollectionChanged != null)
                {
                    var e = new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Remove,
                        new[] { new KeyValuePair<TKey, TValue>(key, value) },
                        -1);
                    CollectionChanged(this, e);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public bool TryGetValue(TKey key, out TValue value)
        {
            return _inner.TryGetValue(key, out value);
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _inner.GetEnumerator();
        }

        /// <inheritdoc/>
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        /// <inheritdoc/>
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            return _inner.Contains(item);
        }

        /// <inheritdoc/>
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key);
        }

        private void NotifyAdd(TKey key, TValue value)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Count"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs($"Item[{key}]"));
            

            if (CollectionChanged != null)
            {
                var e = new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Add,
                    new[] { new KeyValuePair<TKey, TValue>(key, value) },
                    -1);
                CollectionChanged(this, e);
            }
        }
    }
}