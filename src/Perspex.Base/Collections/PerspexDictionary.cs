





namespace Perspex.Collections
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;

    /// <summary>
    /// A notifying dictionary.
    /// </summary>
    /// <typeparam name="TKey">The type of the dictionary key.</typeparam>
    /// <typeparam name="TValue">The type of the dictionary value.</typeparam>
    public class PerspexDictionary<TKey, TValue> : IDictionary<TKey, TValue>,
        INotifyCollectionChanged,
        INotifyPropertyChanged
    {
        private Dictionary<TKey, TValue> inner;

        /// <summary>
        /// Initializes a new instance of the <see cref="PerspexDictionary{TKey, TValue}"/> class.
        /// </summary>
        public PerspexDictionary()
        {
            this.inner = new Dictionary<TKey, TValue>();
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
        public int Count
        {
            get { return this.inner.Count; }
        }

        /// <inheritdoc/>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <inheritdoc/>
        public ICollection<TKey> Keys
        {
            get { return this.inner.Keys; }
        }

        /// <inheritdoc/>
        public ICollection<TValue> Values
        {
            get { return this.inner.Values; }
        }

        /// <summary>
        /// Gets or sets the named resource.
        /// </summary>
        /// <param name="key">The resource key.</param>
        /// <returns>The resource, or null if not found.</returns>
        public TValue this[TKey key]
        {
            get
            {
                return this.inner[key];
            }

            set
            {
                TValue old;
                bool replace = this.inner.TryGetValue(key, out old);
                this.inner[key] = value;

                if (replace)
                {
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs($"Item[{key}]"));
                    }

                    if (this.CollectionChanged != null)
                    {
                        var e = new NotifyCollectionChangedEventArgs(
                            NotifyCollectionChangedAction.Replace,
                            new KeyValuePair<TKey, TValue>(key, value),
                            new KeyValuePair<TKey, TValue>(key, old));
                        this.CollectionChanged(this, e);
                    }
                }
                else
                {
                    this.NotifyAdd(key, value);
                }
            }
        }

        /// <inheritdoc/>
        public void Add(TKey key, TValue value)
        {
            this.inner.Add(key, value);
            this.NotifyAdd(key, value);
        }

        /// <inheritdoc/>
        public void Clear()
        {
            var old = this.inner;

            this.inner = new Dictionary<TKey, TValue>();

            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs("Count"));
                this.PropertyChanged(this, new PropertyChangedEventArgs($"Item[]"));
            }

            if (this.CollectionChanged != null)
            {
                var e = new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Remove,
                    old.ToList(),
                    -1);
                this.CollectionChanged(this, e);
            }
        }

        /// <inheritdoc/>
        public bool ContainsKey(TKey key)
        {
            return this.inner.ContainsKey(key);
        }

        /// <inheritdoc/>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((IDictionary<TKey, TValue>)this.inner).CopyTo(array, arrayIndex);
        }

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return this.inner.GetEnumerator();
        }

        /// <inheritdoc/>
        public bool Remove(TKey key)
        {
            TValue value;

            if (this.inner.TryGetValue(key, out value))
            {
                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("Count"));
                    this.PropertyChanged(this, new PropertyChangedEventArgs($"Item[{key}]"));
                }

                if (this.CollectionChanged != null)
                {
                    var e = new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Remove,
                        new[] { new KeyValuePair<TKey, TValue>(key, value) },
                        -1);
                    this.CollectionChanged(this, e);
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
            return this.inner.TryGetValue(key, out value);
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.inner.GetEnumerator();
        }

        /// <inheritdoc/>
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            this.Add(item.Key, item.Value);
        }

        /// <inheritdoc/>
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            return this.inner.Contains(item);
        }

        /// <inheritdoc/>
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            return this.Remove(item.Key);
        }

        private void NotifyAdd(TKey key, TValue value)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs("Count"));
                this.PropertyChanged(this, new PropertyChangedEventArgs($"Item[{key}]"));
            }

            if (this.CollectionChanged != null)
            {
                var val = new KeyValuePair<TKey, TValue>(key, value);
                var e = new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Add,
                    new[] { new KeyValuePair<TKey, TValue>(key, value) },
                    -1);
                this.CollectionChanged(this, e);
            }
        }
    }
}