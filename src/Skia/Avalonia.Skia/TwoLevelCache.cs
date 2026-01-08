using System;
using System.Collections.Generic;

namespace Avalonia.Skia
{
    /// <summary>
    /// Provides a lightweight two-level cache for storing key-value pairs, supporting fast retrieval and optional
    /// eviction handling.
    /// </summary>
    /// <remarks>The cache maintains a primary entry for the most recently added item and a secondary array
    /// for additional items, with a configurable capacity. When the cache exceeds its capacity, evicted values can be
    /// processed using an optional eviction action. This class is intended for internal use and is not
    /// thread-safe.</remarks>
    /// <typeparam name="TKey">The type of keys used to identify cached values. Must be non-nullable.</typeparam>
    /// <typeparam name="TValue">The type of values to be stored in the cache. Must be a reference type.</typeparam>
    internal class TwoLevelCache<TKey, TValue>
        where TKey : notnull
        where TValue : class
    {
        private readonly int _secondarySize;
        private TKey? _primaryKey;
        private TValue? _primaryValue;
        private KeyValuePair<TKey, TValue>[]? _secondary;
        private readonly Action<TValue?>? _evictionAction;
        private readonly IEqualityComparer<TKey> _comparer;

        public TwoLevelCache(int secondarySize = 3, Action<TValue?>? evictionAction = null, IEqualityComparer<TKey>? comparer = null)
        {
            if (secondarySize < 0)
            { 
                throw new ArgumentOutOfRangeException(nameof(secondarySize));
            }

            _secondarySize = secondarySize;
            _evictionAction = evictionAction;
            _comparer = comparer ?? EqualityComparer<TKey>.Default;
        }

        public bool TryGet(TKey key, out TValue? value)
        {
            if (_primaryValue != null && _comparer.Equals(_primaryKey!, key))
            {
                value = _primaryValue;

                return true;
            }

            var sec = _secondary;

            if (sec != null)
            {
                for (int i = 0; i < sec.Length; i++)
                {
                    if (_comparer.Equals(sec[i].Key, key))
                    {
                        value = sec[i].Value;

                        return true;
                    }
                }
            }

            value = null;

            return false;
        }

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory)
        {
            if (TryGet(key, out var existing) && existing != null)
            {
                return existing;
            }   

            var value = factory(key);

            // Install: if primary empty -> set primary
            if (_primaryValue == null)
            {
                _primaryKey = key;
                _primaryValue = value;

                return value;
            }

            // If primary matches after factory (unlikely) return primary and dispose created
            if (_comparer.Equals(_primaryKey!, key))
            {
                // factory might have created a redundant value
                _evictionAction?.Invoke(value);

                return _primaryValue;
            }

            // Ensure secondary exists
            var sec = _secondary;

            if (sec == null)
            {
                if (_secondarySize == 0)
                {
                    // No secondary - evict primary to eviction action and replace primary
                    _evictionAction?.Invoke(_primaryValue);
                    _primaryKey = key;
                    _primaryValue = value;

                    return value;
                }

                _secondary = new KeyValuePair<TKey, TValue>[_secondarySize];
                _secondary[0] = new KeyValuePair<TKey, TValue>(key, value);

                return value;
            }

            for (int i = 0; i < sec.Length; i++)
            {
                if (_comparer.Equals(sec[i].Key, key))
                {
                    // factory value not needed
                    _evictionAction?.Invoke(value);

                    return sec[i].Value;
                }
            }

            // Rotate into secondary (evict last)
            var newSec = new KeyValuePair<TKey, TValue>[sec.Length];

            newSec[0] = new KeyValuePair<TKey, TValue>(key, value);

            for (int i = 1; i < sec.Length; i++)
            {
                newSec[i] = sec[i - 1];
            }

            // Evict last if present
            var last = sec[sec.Length - 1].Value;

            if (last != null)
            {
                _evictionAction?.Invoke(last);
            }

            _secondary = newSec;

            return value;
        }

        public void ClearAndDispose()
        {
            if (_primaryValue != null)
            {
                _evictionAction?.Invoke(_primaryValue);
                _primaryValue = null;
                _primaryKey = default;
            }

            if (_secondary != null)
            {
                for (int i = 0; i < _secondary.Length; i++)
                {
                    _evictionAction?.Invoke(_secondary[i].Value);
                }

                _secondary = null;
            }
        }
    }
}
