using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Avalonia.Utilities
{
    /// <summary>
    /// Stores either a single key value pair or constructs a dictionary when more than one value is stored.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    internal class SingleOrDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
        where TKey : notnull
    {
        private KeyValuePair<TKey, TValue>? _singleValue;
        private Dictionary<TKey, TValue>? dictionary;

        public void Add(TKey key, TValue value)
        {
            if (_singleValue != null)
            {
                dictionary = new Dictionary<TKey, TValue>();
                ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).Add(_singleValue.Value);
                _singleValue = null;
            }

            if (dictionary != null)
            {
                dictionary.Add(key, value);
            }
            else
            {
                _singleValue = new KeyValuePair<TKey, TValue>(key, value);
            }
        }

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            if (dictionary == null)
            {
                if (!_singleValue.HasValue || !EqualityComparer<TKey>.Default.Equals(_singleValue.Value.Key, key))
                {
                    value = default;
                    return false;
                }
                else
                {
                    value = _singleValue.Value.Value;
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
            if (dictionary == null)
            {
                if (_singleValue.HasValue)
                {
                    return new SingleEnumerator<KeyValuePair<TKey, TValue>>(_singleValue.Value);
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
                if(dictionary == null)
                {
                    if (_singleValue.HasValue)
                    {
                        return new[] { _singleValue.Value.Value };
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
            private readonly T value;
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

            object? IEnumerator.Current => Current;

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
