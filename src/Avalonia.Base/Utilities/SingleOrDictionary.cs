using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Utilities
{
    /// <summary>
    /// Stores either a single key value pair or constructs a dictionary when more than one value is stored.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    public class SingleOrDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private KeyValuePair<TKey, TValue>? singleValue;
        private Dictionary<TKey, TValue> dictionary;

        private bool useDictionary = false;
        
        public void Add(TKey key, TValue value)
        {
            if (singleValue != null)
            {
                dictionary = new Dictionary<TKey, TValue>();
                ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).Add(singleValue.Value);
                useDictionary = true;
                singleValue = null;
            }

            if (useDictionary)
            {
                dictionary.Add(key, value);
            }
            else
            {
                singleValue = new KeyValuePair<TKey, TValue>(key, value);
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (!useDictionary)
            {
                if (!singleValue.HasValue || !singleValue.Value.Key.Equals(key))
                {
                    value = default(TValue);
                    return false;
                }
                else
                {
                    value = singleValue.Value.Value;
                    return true;
                }
            }
            else
            {
                return dictionary.TryGetValue(key, out value);
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            if (!useDictionary)
            {
                if (singleValue.HasValue)
                {
                    return new SingleEnumerator<KeyValuePair<TKey, TValue>>(singleValue.Value);
                }
            }
            else
            {
                return dictionary.GetEnumerator();
            }
            return Enumerable.Empty<KeyValuePair<TKey, TValue>>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerable<TValue> Values
        {
            get
            {
                if(!useDictionary)
                {
                    if (singleValue.HasValue)
                    {
                        return new[] { singleValue.Value.Value };
                    }
                }
                else
                {
                    return dictionary.Values;
                }
                return Enumerable.Empty<TValue>();
            }
        }

        private class SingleEnumerator<T> : IEnumerator<T>
        {
            private T value;
            private int index = -1;

            public SingleEnumerator(T value)
            {
                this.value = value;
            }

            public T Current
            {
                get
                {
                    if (index == 0)
                    {
                        return value;
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }
            }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                index++;
                return index < 1;
            }

            public void Reset()
            {
                index = -1;
            }
        }

    }
}
