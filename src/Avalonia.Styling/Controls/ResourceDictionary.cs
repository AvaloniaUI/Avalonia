using System;
using System.Collections;
using System.Collections.Generic;

namespace Avalonia.Controls
{
    /// <summary>
    /// An indexed dictionary of resources.
    /// </summary>
    public class ResourceDictionary : IDictionary<string, object>, IDictionary
    {
        private Dictionary<string, object> _inner = new Dictionary<string, object>();

        public object this[string key]
        {
            get { return _inner[key]; }
            set { _inner[key] = value; }
        }

        public int Count => _inner.Count;

        ICollection<string> IDictionary<string, object>.Keys => _inner.Keys;

        ICollection<object> IDictionary<string, object>.Values => _inner.Values;

        bool ICollection<KeyValuePair<string, object>>.IsReadOnly => false;

        object IDictionary.this[object key]
        {
            get { return ((IDictionary)_inner)[key]; }
            set { ((IDictionary)_inner)[key] = value; }
        }

        ICollection IDictionary.Keys => _inner.Keys;

        ICollection IDictionary.Values => _inner.Values;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => ((IDictionary)_inner).SyncRoot;

        bool IDictionary.IsFixedSize => false;

        bool IDictionary.IsReadOnly => false;

        public void Add(string key, object value) => _inner.Add(key, value);

        public void Clear() => _inner.Clear();

        public bool ContainsKey(string key) => _inner.ContainsKey(key);

        public bool Remove(string key) => _inner.Remove(key);

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _inner.GetEnumerator();

        public bool TryGetValue(string key, out object value) => _inner.TryGetValue(key, out value);

        bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item)
        {
            return ((IDictionary<string, object>)_inner).Contains(item);
        }

        void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item)
        {
            ((IDictionary<string, object>)_inner).Add(item);
        }

        void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            ((IDictionary<string, object>)_inner).CopyTo(array, arrayIndex);
        }

        bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item)
        {
            return ((IDictionary<string, object>)_inner).Remove(item);
        }

        void ICollection.CopyTo(Array array, int index) => ((IDictionary)_inner).CopyTo(array, index);

        IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();

        IDictionaryEnumerator IDictionary.GetEnumerator() => ((IDictionary)_inner).GetEnumerator();

        void IDictionary.Add(object key, object value) => ((IDictionary)_inner).Add(key, value);

        bool IDictionary.Contains(object key) => ((IDictionary)_inner).Contains(key);

        void IDictionary.Remove(object key) => ((IDictionary)_inner).Remove(key);
    }
}
